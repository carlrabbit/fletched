using Fletched.Roslyn.Pipeline;
using TUnit;

namespace Fletched.Performance.Tests;

public class OptimizationPipelineTests
{
    [Test]
    public async Task RemoveRedundantUnify_ConstantMismatch_RewritesBlockToFail()
    {
        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new UnifyInstr(new ConstValue(1, "int"), new ConstValue(2, "int")),
                    new AssignInstr(0, new ConstValue(3, "int"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new RemoveRedundantUnify().Apply(program);

        await Assert.That(optimized.Entry.Instructions.Count).IsEqualTo(0);
        await Assert.That(optimized.Entry.Terminator).IsTypeOf<FailTerm>();
    }

    [Test]
    public async Task ReorderConjunction_IndependentComparison_MovesAheadOfUnify()
    {
        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new UnifyInstr(new SlotValue(0, "int"), new ConstValue(1, "int")),
                    new CompInstr(CompOp.NotEqual, new SlotValue(1, "int"), new ConstValue(0, "int"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new ReorderConjunction().Apply(program);

        await Assert.That(optimized.Entry.Instructions[0]).IsTypeOf<CompInstr>();
        await Assert.That(optimized.Entry.Instructions[1]).IsTypeOf<UnifyInstr>();
    }

    [Test]
    public async Task ConstraintHoisting_DependentComparison_DoesNotCrossProducer()
    {
        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new UnifyInstr(new SlotValue(0, "int"), new ConstValue(1, "int")),
                    new CompInstr(CompOp.NotEqual, new SlotValue(0, "int"), new ConstValue(0, "int"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new ConstraintHoisting().Apply(program);

        await Assert.That(optimized.Entry.Instructions[0]).IsTypeOf<UnifyInstr>();
        await Assert.That(optimized.Entry.Instructions[1]).IsTypeOf<CompInstr>();
    }

    [Test]
    public async Task TempHoisting_RepeatedFieldRead_IntroducesTemporarySlot()
    {
        var userSlot = new SlotValue(0, "global::Example.User");
        var nameField = new FieldValue(userSlot, "Name", "string");

        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new CompInstr(CompOp.NotEqual, nameField, new ConstValue("root", "string")),
                    new UnifyInstr(nameField, new ConstValue("admin", "string"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new TempHoisting().Apply(program);

        await Assert.That(optimized.Entry.Instructions.Count).IsEqualTo(3);
        await Assert.That(optimized.Entry.Instructions[0]).IsTypeOf<AssignInstr>();

        var assign = (AssignInstr)optimized.Entry.Instructions[0];
        var comparison = (CompInstr)optimized.Entry.Instructions[1];
        var unify = (UnifyInstr)optimized.Entry.Instructions[2];

        await Assert.That(assign.Value).IsEqualTo(nameField);
        await Assert.That(comparison.Left).IsEqualTo(new SlotValue(assign.Slot, "string"));
        await Assert.That(unify.Left).IsEqualTo(new SlotValue(assign.Slot, "string"));
    }

    private static PlanProgram CreateProgram(params PlanBlock[] blocks)
    {
        return new PlanProgram(
            blocks[0],
            blocks.Skip(1).ToList(),
            new Dictionary<VariableSymbol, int>());
    }
}
