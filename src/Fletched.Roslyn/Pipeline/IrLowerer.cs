using System;
using System.Collections.Generic;
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
        if (body is null)
            return null;

        IReadOnlyList<SemanticExpr> parts = body is ConjExpr conj ? conj.Parts : new[] { body };
        int candidateIndex = FindLookupCandidateIndex(parts, variable, preferConstants: true);
        if (candidateIndex < 0)
            candidateIndex = FindLookupCandidateIndex(parts, variable, preferConstants: false);

        if (candidateIndex < 0)
            return null;

        UnifyExpr candidate = (UnifyExpr)parts[candidateIndex];
        (FieldExpr field, SemanticExpr key) = candidate.Left is FieldExpr leftField
            ? (leftField, candidate.Right)
            : ((FieldExpr)candidate.Right, candidate.Left);

        if (key is ConstExpr)
            body = RemovePart(body, parts, candidateIndex);

        return new IndexedLookupSpec(field.Member.Name, LowerValue(key, ctx));
    }

    private static int FindLookupCandidateIndex(
        IReadOnlyList<SemanticExpr> parts,
        VariableSymbol variable,
        bool preferConstants)
    {
        for (int index = 0; index < parts.Count; index++)
        {
            if (parts[index] is not UnifyExpr unify)
                continue;

            if (!TryMatchLookup(unify.Left, unify.Right, variable, preferConstants)
                && !TryMatchLookup(unify.Right, unify.Left, variable, preferConstants))
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static bool TryMatchLookup(
        SemanticExpr fieldExpr,
        SemanticExpr keyExpr,
        VariableSymbol variable,
        bool preferConstants)
    {
        if (fieldExpr is not FieldExpr { Target: VarExpr varExpr }
            || !Equals(varExpr.Variable, variable))
        {
            return false;
        }

        bool isConstant = keyExpr is ConstExpr;
        bool isSlot = keyExpr is VarExpr;
        if (!isConstant && !isSlot)
            return false;

        if (preferConstants != isConstant)
            return false;

        return true;
    }

    private static SemanticExpr? RemovePart(
        SemanticExpr originalBody,
        IReadOnlyList<SemanticExpr> parts,
        int removeIndex)
    {
        if (parts.Count == 1)
            return null;

        var remaining = new List<SemanticExpr>(parts.Count - 1);
        for (int index = 0; index < parts.Count; index++)
        {
            if (index == removeIndex)
                continue;

            remaining.Add(parts[index]);
        }

        return remaining.Count == 1
            ? remaining[0]
            : new ConjExpr(remaining, originalBody.Type);
    }

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
