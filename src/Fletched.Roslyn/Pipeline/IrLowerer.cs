using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

/// <summary>
/// Lowers a <see cref="PredicateModel"/> (semantic tree) into a <see cref="PlanProgram"/>
/// (block-based execution plan) by assigning slots, generating labels, and emitting blocks.
/// </summary>
public sealed class IrLowerer
{
    private readonly DiagnosticReporter _reporter;

    public IrLowerer(DiagnosticReporter reporter) => _reporter = reporter;

    public PlanProgram? Lower(PredicateModel model) => Lower(model, callGraph: null);

    public PlanProgram? Lower(PredicateModel model, PredicateCallGraph? callGraph)
    {
        var ctx = new LoweringContext();

        // Assign slots to parameters
        foreach (VariableSymbol param in model.Parameters)
            ctx.AllocateSlot(param);

        PlanBlock? entry = LowerExpr(model.Body, ctx, out string? entryLabel);
        if (entry is null) return null;

        // Finalize: the entry block and all accumulated blocks
        var allBlocks = ctx.FinalizeBlocks();

        PlanBlock? resolvedEntry = allBlocks.FirstOrDefault(b => b.Label == (entryLabel ?? allBlocks[0].Label));
        if (resolvedEntry is null) return null;

        var rest = allBlocks.Where(b => b.Label != resolvedEntry.Label).ToList();
        PlanProgram plan = new(resolvedEntry, rest, ctx.SlotMap);
        return RecursivePlanningAnnotator.Annotate(model, plan, callGraph, _reporter);
    }

    private PlanBlock? LowerExpr(SemanticExpr expr, LoweringContext ctx, out string? startLabel)
    {
        startLabel = null;

        switch (expr)
        {
            case WithExpr withExpr:
                return LowerWith(withExpr, ctx, out startLabel);

            case ConjExpr conjExpr:
                return LowerConj(conjExpr, ctx, out startLabel);

            case DisjExpr disjExpr:
                return LowerDisj(disjExpr, ctx, out startLabel, continuationLabel: null);

            case UnifyExpr unifyExpr:
                {
                    PlanValue left = LowerValue(unifyExpr.Left, ctx);
                    PlanValue right = LowerValue(unifyExpr.Right, ctx);
                    string label = ctx.NextLabel("main");
                    startLabel = label;
                    var block = new PlanBlock(label,
                        new[] { new UnifyInstr(left, right) },
                        new SucceedTerm());
                    ctx.AddBlock(block);
                    return block;
                }

            case ConstraintExpr constraintExpr:
                {
                    var args = constraintExpr.Arguments.Select(a => LowerValue(a, ctx)).ToList();
                    string label = ctx.NextLabel("main");
                    startLabel = label;
                    var block = new PlanBlock(label,
                        new[] { new ConstraintInstr(constraintExpr.Method, args) },
                        new SucceedTerm());
                    ctx.AddBlock(block);
                    return block;
                }

            case CompExpr compExpr:
                {
                    string label = ctx.NextLabel("main");
                    startLabel = label;
                    var block = new PlanBlock(label,
                        new PlanInstruction[] { new CompInstr(compExpr.Op, LowerValue(compExpr.Left, ctx), LowerValue(compExpr.Right, ctx)) },
                        new SucceedTerm());
                    ctx.AddBlock(block);
                    return block;
                }

            case CallExpr callExpr:
                {
                    // Map argument expressions to slots and copy non-slot values into anonymous slots.
                    var argSlots = new List<int>(callExpr.Arguments.Count);
                    var callInstructions = new List<PlanInstruction>();
                    foreach (SemanticExpr arg in callExpr.Arguments)
                    {
                        if (arg is VarExpr varExpr)
                        {
                            argSlots.Add(ctx.GetSlot(varExpr.Variable));
                            continue;
                        }

                        int tmpSlot = ctx.AllocateAnonymousSlot();
                        argSlots.Add(tmpSlot);
                        callInstructions.Add(new AssignInstr(tmpSlot, LowerValue(arg, ctx)));
                    }

                    callInstructions.Add(new CallInstr(
                        callExpr.PredicateType,
                        argSlots,
                        callExpr.Arity,
                        IsTabledCall: PredicateAttributeHelpers.IsTabledPredicate(callExpr.PredicateType)));

                    string label = ctx.NextLabel("call");
                    startLabel = label;
                    var block = new PlanBlock(label,
                        callInstructions,
                        new SucceedTerm());
                    ctx.AddBlock(block);
                    return block;
                }

            case NotExpr notExpr:
                {
                    var subGoalInstructions = new List<PlanInstruction>();
                    AppendInstructions(notExpr.Goal, ctx, subGoalInstructions);
                    string label = ctx.NextLabel("not");
                    startLabel = label;
                    var block = new PlanBlock(label,
                        new[] { new NotInstr(subGoalInstructions) },
                        new SucceedTerm());
                    ctx.AddBlock(block);
                    return block;
                }

            default:
                _reporter.Error(DiagnosticsCatalog.UnsupportedExpression,
                    null, expr.GetType().Name);
                return null;
        }
    }

    private PlanBlock? LowerConj(ConjExpr conj, LoweringContext ctx, out string? startLabel)
    {
        startLabel = null;
        if (conj.Parts.Count == 0) return null;
        return LowerConjParts(conj.Parts, 0, ctx, out startLabel);
    }

    /// <summary>
    /// Lowers a slice of conjunction parts starting at <paramref name="fromIndex"/>.
    /// When a <see cref="DisjExpr"/> is encountered, the remaining parts are lowered
    /// recursively first so their entry label can be used as the disjunction's continuation.
    /// </summary>
    private PlanBlock? LowerConjParts(
        IReadOnlyList<SemanticExpr> parts, int fromIndex,
        LoweringContext ctx, out string? startLabel)
    {
        startLabel = null;
        if (fromIndex >= parts.Count) return null;

        string blockLabel = ctx.NextLabel("conj");
        startLabel = blockLabel;
        var instructions = new List<PlanInstruction>();

        for (int i = fromIndex; i < parts.Count; i++)
        {
            SemanticExpr part = parts[i];

            switch (part)
            {
                case UnifyExpr u:
                    instructions.Add(new UnifyInstr(LowerValue(u.Left, ctx), LowerValue(u.Right, ctx)));
                    break;

                case ConstraintExpr c:
                    instructions.Add(new ConstraintInstr(c.Method,
                        c.Arguments.Select(a => LowerValue(a, ctx)).ToList()));
                    break;

                case CompExpr comp:
                    instructions.Add(new CompInstr(comp.Op,
                        LowerValue(comp.Left, ctx), LowerValue(comp.Right, ctx)));
                    break;

                case CallExpr call:
                    {
                        var argSlots = new List<int>(call.Arguments.Count);
                        foreach (SemanticExpr arg in call.Arguments)
                        {
                            if (arg is VarExpr varExpr)
                            {
                                argSlots.Add(ctx.GetSlot(varExpr.Variable));
                                continue;
                            }

                            int tmpSlot = ctx.AllocateAnonymousSlot();
                            argSlots.Add(tmpSlot);
                            instructions.Add(new AssignInstr(tmpSlot, LowerValue(arg, ctx)));
                        }

                        instructions.Add(new CallInstr(
                            call.PredicateType,
                            argSlots,
                            call.Arity,
                            IsTabledCall: PredicateAttributeHelpers.IsTabledPredicate(call.PredicateType)));
                        break;
                    }

                case WithExpr w:
                    {
                        if (instructions.Count > 0)
                        {
                            // PeekNextLabel("init") matches the first label that LowerWith allocates.
                            ctx.AddBlock(new PlanBlock(blockLabel, instructions.ToList(),
                                new GotoTerm(ctx.PeekNextLabel("init"))));
                            instructions.Clear();
                        }
                        LowerWith(w, ctx, out string? wStart);
                        if (wStart is not null) startLabel ??= wStart;
                        break;
                    }

                case DisjExpr d:
                    {
                        // Pre-allocate the disjunction entry label BEFORE lowering the remaining
                        // parts, so the GotoTerm from the current block is stable.
                        string disjEntry = ctx.NextLabel("disj");

                        if (instructions.Count > 0)
                        {
                            ctx.AddBlock(new PlanBlock(blockLabel, instructions.ToList(),
                                new GotoTerm(disjEntry)));
                            instructions.Clear();
                        }
                        else
                        {
                            // No prior instructions: disjunction itself is the entry point.
                            startLabel = disjEntry;
                        }

                        // Lower the remaining parts to obtain the continuation label.
                        string? contLabel = null;
                        if (i + 1 < parts.Count)
                            LowerConjParts(parts, i + 1, ctx, out contLabel);

                        // Emit the disjunction blocks with the continuation.
                        LowerDisj(d, ctx, out _, contLabel, disjEntry);

                        return ctx.FindBlock(startLabel!);
                    }

                case NotExpr not:
                    {
                        var subGoalInstructions = new List<PlanInstruction>();
                        AppendInstructions(not.Goal, ctx, subGoalInstructions);
                        instructions.Add(new NotInstr(subGoalInstructions));
                        break;
                    }

                default:
                    _reporter.Error(DiagnosticsCatalog.UnsupportedExpression, null, part.GetType().Name);
                    return null;
            }
        }

        if (instructions.Count > 0)
            ctx.AddBlock(new PlanBlock(blockLabel, instructions, new SucceedTerm()));

        return ctx.FindBlock(startLabel ?? blockLabel);
    }

    private PlanBlock? LowerDisj(DisjExpr disj, LoweringContext ctx, out string? startLabel,
        string? continuationLabel = null, string? preallocatedEntry = null)
    {
        string entryLabel = preallocatedEntry ?? ctx.NextLabel("disj");
        string leftLabel = ctx.NextLabel("disj_l");
        string rightLabel = ctx.NextLabel("disj_r");
        startLabel = entryLabel;

        // Entry block: push choice point for right branch, goto left
        ctx.AddBlock(new PlanBlock(entryLabel, Array.Empty<PlanInstruction>(),
            new ChoiceTerm(leftLabel, rightLabel, -1)));

        PlanTerminator leftTerm = continuationLabel is not null
            ? new GotoTerm(continuationLabel) : (PlanTerminator)new SucceedTerm();
        PlanTerminator rightTerm = continuationLabel is not null
            ? new GotoTerm(continuationLabel) : (PlanTerminator)new SucceedTerm();

        // Left branch — nested DisjExpr must be lowered recursively so that its
        // choice points and blocks are emitted correctly.  Simple expressions are
        // collected as a flat instruction list in a single block.
        EmitDisjBranch(disj.Left, leftLabel, leftTerm, ctx, continuationLabel);

        // Right branch
        EmitDisjBranch(disj.Right, rightLabel, rightTerm, ctx, continuationLabel);

        return ctx.FindBlock(entryLabel);
    }

    /// <summary>
    /// Emits a single branch block for a disjunction.  When <paramref name="branch"/>
    /// is itself a <see cref="DisjExpr"/> the nested disjunction is lowered
    /// recursively (reusing <paramref name="branchLabel"/> as its entry) so that
    /// all of its sub-branches are reachable.  For all other expression kinds
    /// <see cref="AppendInstructions"/> is used to produce a flat instruction list.
    /// </summary>
    private void EmitDisjBranch(SemanticExpr branch, string branchLabel,
        PlanTerminator branchTerm, LoweringContext ctx, string? continuationLabel)
    {
        if (branch is DisjExpr nestedDisj)
        {
            // Recursively lower with the same continuation so that every leaf
            // terminates correctly (SucceedTerm or GotoTerm to the continuation).
            LowerDisj(nestedDisj, ctx, out _, continuationLabel, branchLabel);
        }
        else
        {
            var instructions = new List<PlanInstruction>();
            AppendInstructions(branch, ctx, instructions);
            ctx.AddBlock(new PlanBlock(branchLabel, instructions, branchTerm));
        }
    }

    private PlanBlock? LowerWith(WithExpr with, LoweringContext ctx, out string? startLabel)
    {
        // Source variables enumerate fact tables. Fresh variables only allocate
        // slots and start unbound.
        startLabel = null;
        SemanticExpr? remainingBody = with.Body;
        string? outerStart = null;

        var bodyLabels = new List<string>();
        var initLabels = new List<string>();

        foreach (VariableSymbol variable in with.Variables)
        {
            int slot = ctx.AllocateSlot(variable);
            if (variable.Kind != VariableKind.Source)
                continue;

            IndexedLookupSpec? indexedLookup = TryExtractIndexedLookup(variable, ref remainingBody, ctx);
            string initLabel = ctx.NextLabel("init");
            string checkLabel = ctx.NextLabel("chk");
            string bindLabel = ctx.NextLabel("bind");
            string bodyLabel = ctx.NextLabel("body");
            string nextLabel = ctx.NextLabel("next");
            string idxVar = ctx.NextIndexVar();

            if (outerStart is null) outerStart = initLabel;

            bodyLabels.Add(bodyLabel);
            initLabels.Add(initLabel);

            // L_init: index = 0, goto L_check
            ctx.AddBlock(new PlanBlock(initLabel,
                new PlanInstruction[] { new IndexInitInstr(idxVar, variable.Type, indexedLookup) },
                new GotoTerm(checkLabel)));

            // L_check: if idx >= Data.Length → Fail else → L_bind
            ctx.AddBlock(new PlanBlock(checkLabel, Array.Empty<PlanInstruction>(),
                new LoopCheckTerm(bindLabel, "Fail", idxVar, variable.Type, indexedLookup)));

            // L_bind: Assign(slot, Data[idx]), Choice(L_body, L_next)
            ctx.AddBlock(new PlanBlock(bindLabel,
                new PlanInstruction[] { new LoopBindInstr(slot, idxVar, variable.Type, indexedLookup) },
                new ChoiceTerm(bodyLabel, nextLabel, slot)));

            // L_next: idx++, goto L_check
            ctx.AddBlock(new PlanBlock(nextLabel,
                new PlanInstruction[] { new IndexIncrInstr(idxVar) },
                new GotoTerm(checkLabel)));
        }

        if (bodyLabels.Count == 0)
        {
            if (remainingBody is null)
            {
                string label = ctx.NextLabel("with");
                ctx.AddBlock(new PlanBlock(label, Array.Empty<PlanInstruction>(), new SucceedTerm()));
                startLabel = label;
                return ctx.FindBlock(label);
            }

            return LowerExpr(remainingBody, ctx, out startLabel);
        }

        // Chain nested loops: each outer body block redirects to the next inner loop's init.
        for (int i = 0; i < bodyLabels.Count - 1; i++)
        {
            ctx.AddBlock(new PlanBlock(bodyLabels[i],
                Array.Empty<PlanInstruction>(),
                new GotoTerm(initLabels[i + 1])));
        }

        // The innermost body block holds the actual predicate body instructions.
        // Use full LowerExpr to support DisjExpr and nested WithExpr inside the body.
        string innermostBody = bodyLabels[bodyLabels.Count - 1];
        string? bodyStart;
        if (remainingBody is not null)
            LowerExpr(remainingBody, ctx, out bodyStart);
        else
            bodyStart = null;

        if (bodyStart is not null && bodyStart != innermostBody)
        {
            // Redirect the innermost body block to the lowered body entry.
            ctx.AddBlock(new PlanBlock(innermostBody, Array.Empty<PlanInstruction>(),
                new GotoTerm(bodyStart)));
        }
        else if (bodyStart is null)
        {
            ctx.AddBlock(new PlanBlock(innermostBody, Array.Empty<PlanInstruction>(), new SucceedTerm()));
        }

        startLabel = outerStart;
        return ctx.FindBlock(outerStart!);
    }

    private IndexedLookupSpec? TryExtractIndexedLookup(
        VariableSymbol variable,
        ref SemanticExpr? body,
        LoweringContext ctx)
    {
        if (body is null || variable.Type is not INamedTypeSymbol factType)
            return null;

        IReadOnlyList<SemanticExpr> parts = body is ConjExpr conj ? conj.Parts : new[] { body };
        ImmutableArray<FactIndexDeclaration> indexes = FactIndexModel.GetIndexes(factType);
        if (indexes.Length == 0)
            return null;

        EqualityConstraint[] equalities = parts
            .Select((part, index) => TryExtractEqualityConstraint(part, index, variable, ctx))
            .Where(part => part is not null)
            .Cast<EqualityConstraint>()
            .ToArray();

        RangeConstraint[] ranges = parts
            .Select((part, index) => TryExtractRangeConstraint(part, index, variable, ctx))
            .Where(part => part is not null)
            .Cast<RangeConstraint>()
            .ToArray();

        var validCandidates = new List<IndexedLookupCandidate>();
        var skippedCandidates = new List<SkippedFactIndexCandidate>();

        foreach (FactIndexDeclaration index in indexes)
        {
            IndexedLookupCandidate? candidate = index.Kind switch
            {
                FactIndexKindModel.Equality => BuildEqualityCandidate(factType, index, equalities),
                FactIndexKindModel.Range => BuildRangeCandidate(factType, index, ranges),
                _ => null
            };

            if (candidate is null)
            {
                skippedCandidates.Add(new SkippedFactIndexCandidate(
                    new FactIndexCandidate(
                        factType.Name,
                        index.Name,
                        index.Kind,
                        index.Members,
                        ImmutableArray<SlotId>.Empty,
                        ImmutableArray<PlanInstructionId>.Empty,
                        0,
                        "index requirements are not satisfied by the current fact constraints"),
                    "required members are not constrained at the lookup point"));
                continue;
            }

            validCandidates.Add(candidate);
        }

        IndexedLookupCandidate? selected = validCandidates
            .OrderByDescending(candidate => candidate.Candidate.Score)
            .ThenByDescending(candidate => candidate.Candidate.SatisfiedConstraints.Length)
            .ThenBy(candidate => candidate.Declaration.DeclarationOrder)
            .ThenBy(candidate => candidate.Candidate.IndexName, StringComparer.Ordinal)
            .FirstOrDefault();

        if (selected is null)
            return null;

        foreach (IndexedLookupCandidate candidate in validCandidates.Where(candidate => !ReferenceEquals(candidate, selected)))
        {
            skippedCandidates.Add(new SkippedFactIndexCandidate(
                candidate.Candidate,
                "a higher-ranked candidate was selected deterministically"));
        }

        ImmutableArray<string> residuals = parts
            .Select(DescribeConstraint)
            .Where(text => !selected.SatisfiedConstraintTexts.Contains(text, StringComparer.Ordinal))
            .ToImmutableArray();

        return new IndexedLookupSpec(
            selected.Declaration.Name,
            selected.Declaration.FieldName,
            selected.Declaration.IsImplicit,
            selected.AccessPathKind,
            selected.Declaration.Members,
            selected.EqualityParts,
            selected.Range,
            selected.BoundInputNames,
            selected.SatisfiedConstraintTexts,
            residuals,
            skippedCandidates.ToImmutableArray(),
            selected.Candidate.Reason);
    }

    private IndexedLookupCandidate? BuildEqualityCandidate(
        INamedTypeSymbol factType,
        FactIndexDeclaration declaration,
        EqualityConstraint[] equalities)
    {
        var parts = new List<EqualityLookupPart>(declaration.Members.Length);
        var boundInputs = ImmutableArray.CreateBuilder<SlotId>();
        var boundInputNames = ImmutableArray.CreateBuilder<string>();
        var satisfiedIds = ImmutableArray.CreateBuilder<PlanInstructionId>();
        var satisfiedTexts = ImmutableArray.CreateBuilder<string>();
        int constantMatches = 0;

        foreach (string member in declaration.Members)
        {
            EqualityConstraint? match = equalities
                .Where(equality => string.Equals(equality.MemberName, member, StringComparison.Ordinal))
                .OrderBy(equality => equality.IsConstant ? 0 : 1)
                .ThenBy(equality => equality.Order)
                .FirstOrDefault();

            if (match is null)
                return null;

            parts.Add(new EqualityLookupPart(member, match.Key));
            satisfiedIds.Add(match.Id);
            satisfiedTexts.Add(match.Text);
            if (match.IsConstant)
                constantMatches++;

            if (match.Slot is not null)
            {
                boundInputs.Add(new SlotId(match.Slot.Value));
                boundInputNames.Add(match.BoundInputName);
            }
        }

        int score = declaration.Unique ? 1_000 : 0;
        score += declaration.IsCompositeEquality ? 800 + declaration.Members.Length * 10 : 700;
        score += constantMatches * 5;
        string reason = declaration.IsCompositeEquality
            ? "all composite index members are constrained"
            : constantMatches > 0
                ? "single equality member is constrained by a constant"
                : "single equality member is constrained";

        return new IndexedLookupCandidate(
            declaration,
            declaration.IsCompositeEquality ? FactAccessPathKind.CompositeEqualityIndex : FactAccessPathKind.EqualityIndex,
            parts.ToImmutableArray(),
            Range: null,
            boundInputNames.ToImmutable(),
            satisfiedTexts.ToImmutable(),
            new FactIndexCandidate(
                factType.Name,
                declaration.Name,
                declaration.Kind,
                declaration.Members,
                boundInputs.ToImmutable(),
                satisfiedIds.ToImmutable(),
                score,
                reason));
    }

    private IndexedLookupCandidate? BuildRangeCandidate(
        INamedTypeSymbol factType,
        FactIndexDeclaration declaration,
        RangeConstraint[] ranges)
    {
        RangeConstraint? lower = ranges
            .Where(range => string.Equals(range.MemberName, declaration.Members[0], StringComparison.Ordinal) && range.IsLowerBound)
            .OrderBy(range => range.Order)
            .FirstOrDefault();
        RangeConstraint? upper = ranges
            .Where(range => string.Equals(range.MemberName, declaration.Members[0], StringComparison.Ordinal) && !range.IsLowerBound)
            .OrderBy(range => range.Order)
            .FirstOrDefault();

        if (lower is null && upper is null)
            return null;

        var boundInputs = ImmutableArray.CreateBuilder<SlotId>();
        var boundInputNames = ImmutableArray.CreateBuilder<string>();
        var satisfiedIds = ImmutableArray.CreateBuilder<PlanInstructionId>();
        var satisfiedTexts = ImmutableArray.CreateBuilder<string>();

        foreach (RangeConstraint range in new[] { lower, upper }.Where(range => range is not null).Cast<RangeConstraint>())
        {
            satisfiedIds.Add(range.Id);
            satisfiedTexts.Add(range.Text);
            if (range.Slot is not null)
            {
                boundInputs.Add(new SlotId(range.Slot.Value));
                boundInputNames.Add(range.BoundInputName);
            }
        }

        bool twoSided = lower is not null && upper is not null;
        return new IndexedLookupCandidate(
            declaration,
            FactAccessPathKind.RangeIndex,
            ImmutableArray<EqualityLookupPart>.Empty,
            new RangeLookupSpec(
                declaration.Members[0],
                lower?.Key,
                lower?.Inclusive ?? true,
                upper?.Key,
                upper?.Inclusive ?? true),
            boundInputNames.ToImmutable(),
            satisfiedTexts.ToImmutable(),
            new FactIndexCandidate(
                factType.Name,
                declaration.Name,
                declaration.Kind,
                declaration.Members,
                boundInputs.ToImmutable(),
                satisfiedIds.ToImmutable(),
                twoSided ? 600 : 500,
                twoSided
                    ? "both range bounds are constrained"
                    : "at least one range bound is constrained"));
    }

    private EqualityConstraint? TryExtractEqualityConstraint(SemanticExpr expr, int order, VariableSymbol variable, LoweringContext ctx)
    {
        if (expr is not UnifyExpr unify)
            return null;

        return TryCreateEqualityConstraint(unify.Left, unify.Right, order, variable, ctx)
            ?? TryCreateEqualityConstraint(unify.Right, unify.Left, order, variable, ctx);
    }

    private EqualityConstraint? TryCreateEqualityConstraint(
        SemanticExpr fieldExpr,
        SemanticExpr keyExpr,
        int order,
        VariableSymbol variable,
        LoweringContext ctx)
    {
        if (fieldExpr is not FieldExpr { Target: VarExpr varExpr } field
            || !Equals(varExpr.Variable, variable))
        {
            return null;
        }

        if (keyExpr is not ConstExpr && keyExpr is not VarExpr)
            return null;

        return new EqualityConstraint(
            field.Member.Name,
            LowerValue(keyExpr, ctx),
            order,
            keyExpr is ConstExpr,
            keyExpr is VarExpr keyVar ? ctx.GetSlot(keyVar.Variable) : null,
            keyExpr is VarExpr keyVarExpr ? keyVarExpr.Variable.Name : DescribeConstraint(keyExpr),
            DescribeConstraint(new UnifyExpr(fieldExpr, keyExpr)),
            new PlanInstructionId($"eq_{order}_{field.Member.Name}"));
    }

    private RangeConstraint? TryExtractRangeConstraint(SemanticExpr expr, int order, VariableSymbol variable, LoweringContext ctx)
    {
        if (expr is not CompExpr comparison)
            return null;

        return TryCreateRangeConstraint(comparison.Op, comparison.Left, comparison.Right, order, variable, ctx)
            ?? TryCreateRangeConstraint(Reverse(comparison.Op), comparison.Right, comparison.Left, order, variable, ctx);
    }

    private RangeConstraint? TryCreateRangeConstraint(
        CompOp op,
        SemanticExpr fieldExpr,
        SemanticExpr keyExpr,
        int order,
        VariableSymbol variable,
        LoweringContext ctx)
    {
        if (fieldExpr is not FieldExpr { Target: VarExpr varExpr } field
            || !Equals(varExpr.Variable, variable))
        {
            return null;
        }

        if (keyExpr is not ConstExpr && keyExpr is not VarExpr)
            return null;

        bool isLower = op is CompOp.GreaterThan or CompOp.GreaterThanOrEqual;
        bool inclusive = op is CompOp.GreaterThanOrEqual or CompOp.LessThanOrEqual;
        string text = $"{DescribeConstraint(fieldExpr)} {DescribeOperator(op)} {DescribeConstraint(keyExpr)}";
        return new RangeConstraint(
            field.Member.Name,
            LowerValue(keyExpr, ctx),
            isLower,
            inclusive,
            order,
            keyExpr is VarExpr keyVar ? ctx.GetSlot(keyVar.Variable) : null,
            keyExpr is VarExpr keyVarExpr ? keyVarExpr.Variable.Name : DescribeConstraint(keyExpr),
            text,
            new PlanInstructionId($"range_{order}_{field.Member.Name}_{op}"));
    }

    private static CompOp Reverse(CompOp op) =>
        op switch
        {
            CompOp.GreaterThan => CompOp.LessThan,
            CompOp.GreaterThanOrEqual => CompOp.LessThanOrEqual,
            CompOp.LessThan => CompOp.GreaterThan,
            CompOp.LessThanOrEqual => CompOp.GreaterThanOrEqual,
            _ => op
        };

    private static string DescribeConstraint(SemanticExpr expr) =>
        expr switch
        {
            VarExpr variable => variable.Variable.Name,
            ConstExpr constant when constant.Value is string text => $"\"{text}\"",
            ConstExpr constant when constant.Value is null => "null",
            ConstExpr constant => constant.Value?.ToString() ?? "null",
            FieldExpr { Target: VarExpr varExpr } field => $"{varExpr.Variable.Name}.{field.Member.Name}",
            UnifyExpr unify => $"{DescribeConstraint(unify.Left)} == {DescribeConstraint(unify.Right)}",
            CompExpr comparison => $"{DescribeConstraint(comparison.Left)} {DescribeOperator(comparison.Op)} {DescribeConstraint(comparison.Right)}",
            _ => expr.ToString() ?? string.Empty
        };

    private static string DescribeOperator(CompOp op) =>
        op switch
        {
            CompOp.NotEqual => "!=",
            CompOp.LessThan => "<",
            CompOp.GreaterThan => ">",
            CompOp.LessThanOrEqual => "<=",
            CompOp.GreaterThanOrEqual => ">=",
            _ => "?"
        };

    private sealed record EqualityConstraint(
        string MemberName,
        PlanValue Key,
        int Order,
        bool IsConstant,
        int? Slot,
        string BoundInputName,
        string Text,
        PlanInstructionId Id);

    private sealed record RangeConstraint(
        string MemberName,
        PlanValue Key,
        bool IsLowerBound,
        bool Inclusive,
        int Order,
        int? Slot,
        string BoundInputName,
        string Text,
        PlanInstructionId Id);

    private sealed record IndexedLookupCandidate(
        FactIndexDeclaration Declaration,
        FactAccessPathKind AccessPathKind,
        ImmutableArray<EqualityLookupPart> EqualityParts,
        RangeLookupSpec? Range,
        ImmutableArray<string> BoundInputNames,
        ImmutableArray<string> SatisfiedConstraintTexts,
        FactIndexCandidate Candidate);

    private void AppendInstructions(SemanticExpr expr, LoweringContext ctx, List<PlanInstruction> instructions)
    {
        switch (expr)
        {
            case UnifyExpr u:
                instructions.Add(new UnifyInstr(LowerValue(u.Left, ctx), LowerValue(u.Right, ctx)));
                break;
            case ConstraintExpr c:
                instructions.Add(new ConstraintInstr(c.Method,
                    c.Arguments.Select(a => LowerValue(a, ctx)).ToList()));
                break;
            case CompExpr comp:
                instructions.Add(new CompInstr(comp.Op,
                    LowerValue(comp.Left, ctx), LowerValue(comp.Right, ctx)));
                break;
            case CallExpr call:
                {
                    var argSlots = new List<int>(call.Arguments.Count);
                    foreach (SemanticExpr arg in call.Arguments)
                    {
                        if (arg is VarExpr varExpr)
                        {
                            argSlots.Add(ctx.GetSlot(varExpr.Variable));
                            continue;
                        }

                        int tmpSlot = ctx.AllocateAnonymousSlot();
                        argSlots.Add(tmpSlot);
                        instructions.Add(new AssignInstr(tmpSlot, LowerValue(arg, ctx)));
                    }

                    instructions.Add(new CallInstr(
                        call.PredicateType,
                        argSlots,
                        call.Arity,
                        IsTabledCall: PredicateAttributeHelpers.IsTabledPredicate(call.PredicateType)));
                    break;
                }
            case ConjExpr conj:
                foreach (SemanticExpr part in conj.Parts)
                    AppendInstructions(part, ctx, instructions);
                break;
            case NotExpr not:
                {
                    var subGoalInstructions = new List<PlanInstruction>();
                    AppendInstructions(not.Goal, ctx, subGoalInstructions);
                    instructions.Add(new NotInstr(subGoalInstructions));
                    break;
                }
            // WithExpr and DisjExpr inside a body are complex — handled by full LowerExpr
            default:
                break;
        }
    }

    private PlanValue LowerValue(SemanticExpr expr, LoweringContext ctx)
    {
        switch (expr)
        {
            case VarExpr v:
                return new SlotValue(ctx.GetSlot(v.Variable), SymbolDisplayString(v.Variable.Type));
            case ConstExpr c:
                return new ConstValue(c.Value, SymbolDisplayString(c.Type));
            case FieldExpr f:
                return new FieldValue(LowerValue(f.Target, ctx), f.Member.Name,
                    SymbolDisplayString(f.FieldType));
            case ArithExpr a:
                return new ArithValue(a.Op, LowerValue(a.Left, ctx), LowerValue(a.Right, ctx));
            case ListEmptyExpr e:
                return new ListEmptyValue(SymbolDisplayString(e.ElementType));
            case ListConsExpr c:
                return new ListConsValue(
                    LowerValue(c.Head, ctx),
                    LowerValue(c.Tail, ctx),
                    SymbolDisplayString(c.ElementType));
            default:
                return new ConstValue(null, "object");
        }
    }

    private static string SymbolDisplayString(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
