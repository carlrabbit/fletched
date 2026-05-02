using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests.IR;

public class PlanNodeTests
{
    [Test]
    public async Task SlotValue_Equality()
    {
        var a = new SlotValue(0, typeof(string));
        var b = new SlotValue(0, typeof(string));
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task SlotValue_DifferentSlot_NotEqual()
    {
        var a = new SlotValue(0, typeof(string));
        var b = new SlotValue(1, typeof(string));
        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task ConstValue_NullValue()
    {
        var c = new ConstValue(null, typeof(string));
        await Assert.That(c.Value).IsNull();
    }

    [Test]
    public async Task PlanBlock_HasLabel()
    {
        var block = new PlanBlock("L0", new PlanInstruction[0], new SucceedTerm());
        await Assert.That(block.Label).IsEqualTo("L0");
    }

    [Test]
    public async Task PlanBlock_InstructionCount()
    {
        var instr = new UnifyInstr(new SlotValue(0, typeof(string)), new ConstValue("x", typeof(string)));
        var block = new PlanBlock("L1", new PlanInstruction[] { instr }, new SucceedTerm());
        await Assert.That(block.Instructions.Count).IsEqualTo(1);
    }

    [Test]
    public async Task FullScanSource_HasFactType()
    {
        var source = new FullScanSource(typeof(string));
        await Assert.That(source.FactType).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task GotoTerm_HasTargetLabel()
    {
        var term = new GotoTerm("L5");
        await Assert.That(term.TargetLabel).IsEqualTo("L5");
    }

    [Test]
    public async Task ChoiceTerm_HasBothLabels()
    {
        var term = new ChoiceTerm("L_body", "L_alt", 0);
        await Assert.That(term.NextLabel).IsEqualTo("L_body");
        await Assert.That(term.AlternativeLabel).IsEqualTo("L_alt");
    }
}
