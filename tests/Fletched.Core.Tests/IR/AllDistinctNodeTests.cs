using System;
using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests.IR;

public class AllDistinctNodeTests
{
    [Test]
    public async Task AllDistinctNode_StoresCollectionAndElementType()
    {
        var collection = new VarNode("col", typeof(int[]));
        var node = new AllDistinctNode(collection, typeof(int));

        await Assert.That(node.Collection).IsEqualTo(collection);
        await Assert.That(node.ElementType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task AllDistinctNode_Equality_SameArguments()
    {
        var collection = new VarNode("col", typeof(int[]));
        var a = new AllDistinctNode(collection, typeof(int));
        var b = new AllDistinctNode(collection, typeof(int));

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task AllDistinctNode_Inequality_DifferentElementType()
    {
        var collection = new VarNode("col", typeof(string[]));
        var a = new AllDistinctNode(collection, typeof(string));
        var b = new AllDistinctNode(collection, typeof(int));

        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task AllDistinctNode_IsExprNode()
    {
        var collection = new ConstNode(null, typeof(int[]));
        ExprNode node = new AllDistinctNode(collection, typeof(int));

        await Assert.That(node).IsTypeOf<AllDistinctNode>();
    }

    [Test]
    public async Task AllDistinctInstr_StoresSlotsAndElementType()
    {
        int[] slots = [0, 1, 2];
        var instr = new AllDistinctInstr(slots, typeof(int));

        await Assert.That(instr.Slots.Count).IsEqualTo(3);
        await Assert.That(instr.ElementType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task AllDistinctInstr_Equality_SameArguments()
    {
        int[] slots = [0, 1];
        var a = new AllDistinctInstr(slots, typeof(string));
        var b = new AllDistinctInstr(slots, typeof(string));

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task AllDistinctInstr_IsPlanInstruction()
    {
        PlanInstruction instr = new AllDistinctInstr(Array.Empty<int>(), typeof(int));

        await Assert.That(instr).IsTypeOf<AllDistinctInstr>();
    }
}
