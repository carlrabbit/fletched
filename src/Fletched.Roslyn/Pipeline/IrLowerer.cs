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
                return LowerDisj(disjExpr, ctx, out startLabel);

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

        string label = ctx.NextLabel("conj");
        startLabel = label;

        var instructions = new List<PlanInstruction>();

        foreach (SemanticExpr part in conj.Parts)
        {
            switch (part)
            {
                case UnifyExpr u:
                    instructions.Add(new UnifyInstr(LowerValue(u.Left, ctx), LowerValue(u.Right, ctx)));
                    break;
                case ConstraintExpr c:
                    instructions.Add(new ConstraintInstr(c.Method,
                        c.Arguments.Select(a => LowerValue(a, ctx)).ToList()));
                    break;
                case WithExpr w:
                {
                    // Close out current instructions block, then chain into the with loop
                    if (instructions.Count > 0)
                    {
                        string prevLabel = startLabel ?? ctx.NextLabel("conj_pre");
                        if (startLabel is null) startLabel = prevLabel;
                        ctx.AddBlock(new PlanBlock(prevLabel, instructions.ToList(), new GotoTerm(ctx.PeekNextLabel("with"))));
                        instructions.Clear();
                    }
                    var withBlock = LowerWith(w, ctx, out string? wStart);
                    // The with starts a new chain; re-anchor
                    if (wStart is not null) startLabel ??= wStart;
                    break;
                }
                case DisjExpr d:
                {
                    if (instructions.Count > 0)
                    {
                        string prevLabel = label;
                        ctx.AddBlock(new PlanBlock(prevLabel, instructions.ToList(), new GotoTerm(ctx.PeekNextLabel("disj"))));
                        instructions.Clear();
                    }
                    LowerDisj(d, ctx, out string? dStart);
                    if (dStart is not null) startLabel ??= dStart;
                    break;
                }
                default:
                    _reporter.Error(DiagnosticsCatalog.UnsupportedExpression, null, part.GetType().Name);
                    return null;
            }
        }

        if (instructions.Count > 0)
        {
            ctx.AddBlock(new PlanBlock(label, instructions, new SucceedTerm()));
        }

        return ctx.FindBlock(startLabel ?? label);
    }

    private PlanBlock? LowerDisj(DisjExpr disj, LoweringContext ctx, out string? startLabel)
    {
        string entryLabel = ctx.NextLabel("disj");
        string leftLabel = ctx.NextLabel("disj_l");
        string rightLabel = ctx.NextLabel("disj_r");
        startLabel = entryLabel;

        // Entry block: push choice point for right branch, goto left
        ctx.AddBlock(new PlanBlock(entryLabel, Array.Empty<PlanInstruction>(),
            new ChoiceTerm(leftLabel, rightLabel, -1)));

        // Left branch
        var leftInstructions = new List<PlanInstruction>();
        AppendInstructions(disj.Left, ctx, leftInstructions);
        ctx.AddBlock(new PlanBlock(leftLabel, leftInstructions, new SucceedTerm()));

        // Right branch
        var rightInstructions = new List<PlanInstruction>();
        AppendInstructions(disj.Right, ctx, rightInstructions);
        ctx.AddBlock(new PlanBlock(rightLabel, rightInstructions, new SucceedTerm()));

        return ctx.FindBlock(entryLabel);
    }

    private PlanBlock? LowerWith(WithExpr with, LoweringContext ctx, out string? startLabel)
    {
        // Each variable in the With gets its own slot + loop
        // For multiple variables, we nest loops
        startLabel = null;
        string? outerStart = null;

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

            // L_init: index = 0, goto L_check
            ctx.AddBlock(new PlanBlock(initLabel,
                new PlanInstruction[] { new IndexInitInstr(idxVar) },
                new GotoTerm(checkLabel)));

            // L_check: if idx >= Data.Length → Fail else → L_bind
            ctx.AddBlock(new PlanBlock(checkLabel, Array.Empty<PlanInstruction>(),
                new LoopCheckTerm(bindLabel, "Fail", idxVar, variable.Type)));

            // L_bind: Assign(slot, Data[idx]), Choice(L_body, L_next)
            ctx.AddBlock(new PlanBlock(bindLabel,
                new PlanInstruction[] { new LoopBindInstr(slot, idxVar, variable.Type) },
                new ChoiceTerm(bodyLabel, nextLabel, slot)));

            // L_body: lower body, then Succeed
            // The body block is inserted after the loop preamble
            string bodyBlockLabel = bodyLabel;
            ctx.PushLoopContext(bodyBlockLabel, nextLabel, idxVar);

            // L_next: idx++, goto L_check
            ctx.AddBlock(new PlanBlock(nextLabel,
                new PlanInstruction[] { new IndexIncrInstr(idxVar) },
                new GotoTerm(checkLabel)));
        }

        // Now lower the body inside the loop context
        var bodyInstructions = new List<PlanInstruction>();
        AppendInstructions(with.Body, ctx, bodyInstructions);

        string bodyLabel2 = ctx.PopLoopContext() ?? ctx.NextLabel("body");
        ctx.AddBlock(new PlanBlock(bodyLabel2, bodyInstructions, new SucceedTerm()));

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
            default:
                return new ConstValue(null, "object");
        }
    }

    private static string SymbolDisplayString(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
