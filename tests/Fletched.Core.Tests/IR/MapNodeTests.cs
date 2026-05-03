using System;
using System.Reflection;
using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests.IR;

public class MapNodeTests
{
    // ── MapNode ────────────────────────────────────────────────────────────────

    [Test]
    public async Task MapNode_StoresAllFields()
    {
        var collection = new VarNode("board", typeof(int[][]));
        var elementVar = new VarNode("elem", typeof(int[]));
        var selectorBody = new VarNode("result", typeof(int));

        var node = new MapNode(collection, elementVar, selectorBody, typeof(int[]), typeof(int));

        await Assert.That(node.Collection).IsEqualTo(collection);
        await Assert.That(node.ElementVar).IsEqualTo(elementVar);
        await Assert.That(node.SelectorBody).IsEqualTo(selectorBody);
        await Assert.That(node.SourceType).IsEqualTo(typeof(int[]));
        await Assert.That(node.ResultType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task MapNode_IsExprNode()
    {
        var collection = new ConstNode(null, typeof(string[]));
        var elementVar = new VarNode("e", typeof(string));
        var selectorBody = new ConstNode("x", typeof(string));

        ExprNode node = new MapNode(collection, elementVar, selectorBody, typeof(string), typeof(string));

        await Assert.That(node).IsTypeOf<MapNode>();
    }

    [Test]
    public async Task MapNode_Equality_SameArguments()
    {
        var collection = new VarNode("col", typeof(int[]));
        var elementVar = new VarNode("e", typeof(int));
        var selectorBody = new ConstNode(42, typeof(int));

        var a = new MapNode(collection, elementVar, selectorBody, typeof(int), typeof(int));
        var b = new MapNode(collection, elementVar, selectorBody, typeof(int), typeof(int));

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task MapNode_Inequality_DifferentResultType()
    {
        var collection = new VarNode("col", typeof(object[]));
        var elementVar = new VarNode("e", typeof(object));
        var selectorBody = new ConstNode(null, typeof(object));

        var a = new MapNode(collection, elementVar, selectorBody, typeof(object), typeof(int));
        var b = new MapNode(collection, elementVar, selectorBody, typeof(object), typeof(string));

        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task MapNode_Inequality_DifferentCollection()
    {
        var col1 = new VarNode("board1", typeof(int[]));
        var col2 = new VarNode("board2", typeof(int[]));
        var elementVar = new VarNode("e", typeof(int));
        var selectorBody = new ConstNode(0, typeof(int));

        var a = new MapNode(col1, elementVar, selectorBody, typeof(int), typeof(int));
        var b = new MapNode(col2, elementVar, selectorBody, typeof(int), typeof(int));

        await Assert.That(a).IsNotEqualTo(b);
    }

    // ── MapInstr ───────────────────────────────────────────────────────────────

    private static MemberInfo GetStudentMember() =>
        typeof(FakeRecord).GetProperty(nameof(FakeRecord.Value))!;

    private record FakeRecord(int Value);

    [Test]
    public async Task MapInstr_StoresAllFields()
    {
        MemberInfo member = GetStudentMember();
        var instr = new MapInstr(0, member, 1, typeof(FakeRecord), typeof(int));

        await Assert.That(instr.CollectionSlot).IsEqualTo(0);
        await Assert.That(instr.Member).IsEqualTo(member);
        await Assert.That(instr.ResultSlot).IsEqualTo(1);
        await Assert.That(instr.SourceType).IsEqualTo(typeof(FakeRecord));
        await Assert.That(instr.ResultType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task MapInstr_Equality_SameArguments()
    {
        MemberInfo member = GetStudentMember();
        var a = new MapInstr(2, member, 3, typeof(FakeRecord), typeof(int));
        var b = new MapInstr(2, member, 3, typeof(FakeRecord), typeof(int));

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task MapInstr_Inequality_DifferentSlots()
    {
        MemberInfo member = GetStudentMember();
        var a = new MapInstr(0, member, 1, typeof(FakeRecord), typeof(int));
        var b = new MapInstr(0, member, 2, typeof(FakeRecord), typeof(int));

        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task MapInstr_IsPlanInstruction()
    {
        MemberInfo member = GetStudentMember();
        PlanInstruction instr = new MapInstr(0, member, 1, typeof(FakeRecord), typeof(int));

        await Assert.That(instr).IsTypeOf<MapInstr>();
    }
}
