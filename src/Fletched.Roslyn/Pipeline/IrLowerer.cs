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

    public PlanProgram? Lower(PredicateModel model)
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
        return new PlanProgram(resolvedEntry, rest, ctx.SlotMap);
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
                // Map argument expressions to slots
                var argSlots = callExpr.Arguments
                    .Select(a => a is VarExpr ve ? ctx.GetSlot(ve.Variable) : ctx.AllocateAnonymousSlot())
                    .ToList();

                string label = ctx.NextLabel("call");
                startLabel = label;
                var block = new PlanBlock(label,
                    new[] { new CallInstr(callExpr.PredicateType, argSlots) },
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

        // Left branch
        var leftInstructions = new List<PlanInstruction>();
        AppendInstructions(disj.Left, ctx, leftInstructions);
        ctx.AddBlock(new PlanBlock(leftLabel, leftInstructions, leftTerm));

        // Right branch
        var rightInstructions = new List<PlanInstruction>();
        AppendInstructions(disj.Right, ctx, rightInstructions);
        ctx.AddBlock(new PlanBlock(rightLabel, rightInstructions, rightTerm));

        return ctx.FindBlock(entryLabel);
    }

    private PlanBlock? LowerWith(WithExpr with, LoweringContext ctx, out string? startLabel)
    {
        // Each variable in the With gets its own slot + loop.
        // For multiple variables we nest loops: the outer loop's "body" block
        // simply redirects to the next inner loop's init, and only the innermost
        // body block contains the actual predicate body instructions.
        startLabel = null;
        string? outerStart = null;

        var bodyLabels = new List<string>();
        var initLabels = new List<string>();

        foreach (VariableSymbol variable in with.Variables)
        {
            int slot = ctx.AllocateSlot(variable);
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
                new PlanInstruction[] { new IndexInitInstr(idxVar, variable.Type) },
                new GotoTerm(checkLabel)));

            // L_check: if idx >= Data.Length → Fail else → L_bind
            ctx.AddBlock(new PlanBlock(checkLabel, Array.Empty<PlanInstruction>(),
                new LoopCheckTerm(bindLabel, "Fail", idxVar, variable.Type)));

            // L_bind: Assign(slot, Data[idx]), Choice(L_body, L_next)
            ctx.AddBlock(new PlanBlock(bindLabel,
                new PlanInstruction[] { new LoopBindInstr(slot, idxVar, variable.Type) },
                new ChoiceTerm(bodyLabel, nextLabel, slot)));

            // L_next: idx++, goto L_check
            ctx.AddBlock(new PlanBlock(nextLabel,
                new PlanInstruction[] { new IndexIncrInstr(idxVar) },
                new GotoTerm(checkLabel)));
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
        LowerExpr(with.Body, ctx, out string? bodyStart);

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
                var argSlots = call.Arguments
                    .Select(a => a is VarExpr ve ? ctx.GetSlot(ve.Variable) : ctx.AllocateAnonymousSlot())
                    .ToList();
                instructions.Add(new CallInstr(call.PredicateType, argSlots));
                break;
            }
            case ConjExpr conj:
                foreach (SemanticExpr part in conj.Parts)
                    AppendInstructions(part, ctx, instructions);
                break;
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
