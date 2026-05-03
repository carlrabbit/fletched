using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests;

public class MapDslTests
{
    [Test]
    public async Task Map_CreatesMapNode_WithCorrectTypes()
    {
        var boardVar = new VarNode("board", typeof(int[]));
        LogicExpr<int[]> board = new(boardVar);

        LogicExpr<int[]> result = Logic.Map<int, int>(board, proxy =>
            new LogicExpr<int>(new VarNode(proxy.VariableName, typeof(int))));

        var node = (MapNode)result.Node!;
        await Assert.That(node.Collection).IsEqualTo(boardVar);
        await Assert.That(node.SourceType).IsEqualTo(typeof(int));
        await Assert.That(node.ResultType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task Map_ElementVar_HasCorrectType()
    {
        var colVar = new VarNode("col", typeof(string[]));
        LogicExpr<string[]> col = new(colVar);

        LogicExpr<string[]> result = Logic.Map<string, string>(col, proxy =>
            new LogicExpr<string>(new VarNode(proxy.VariableName, typeof(string))));

        var node = (MapNode)result.Node!;
        await Assert.That(node.ElementVar.Type).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task Map_ElementVar_NameMatchesSelectorProxyVariableName()
    {
        var colVar = new VarNode("col", typeof(int[]));
        LogicExpr<int[]> col = new(colVar);

        string? capturedProxyName = null;
        LogicExpr<int[]> result = Logic.Map<int, int>(col, proxy =>
        {
            capturedProxyName = proxy.VariableName;
            return new LogicExpr<int>(new VarNode(proxy.VariableName, typeof(int)));
        });

        var node = (MapNode)result.Node!;
        await Assert.That(node.ElementVar.Name).IsEqualTo(capturedProxyName);
    }

    [Test]
    public async Task Map_SelectorBody_IsStoredOnNode()
    {
        var colVar = new VarNode("col", typeof(int[]));
        LogicExpr<int[]> col = new(colVar);
        var selectorResult = new ConstNode(99, typeof(int));

        LogicExpr<int[]> result = Logic.Map<int, int>(col, _ =>
            new LogicExpr<int>(selectorResult));

        var node = (MapNode)result.Node!;
        await Assert.That(node.SelectorBody).IsEqualTo(selectorResult);
    }

    [Test]
    public async Task Map_ResultIsArrayType()
    {
        var colVar = new VarNode("names", typeof(string[]));
        LogicExpr<string[]> col = new(colVar);

        LogicExpr<int[]> result = Logic.Map<string, int>(col, proxy =>
            new LogicExpr<int>(new ConstNode(0, typeof(int))));

        var node = (MapNode)result.Node!;
        await Assert.That(node.SourceType).IsEqualTo(typeof(string));
        await Assert.That(node.ResultType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task Map_CanBePassedToAllDistinct()
    {
        var colVar = new VarNode("col", typeof(int[]));
        LogicExpr<int[]> col = new(colVar);

        LogicExpr<int[]> mapped = Logic.Map<int, int>(col, proxy =>
            new LogicExpr<int>(new VarNode(proxy.VariableName, typeof(int))));

        LogicExpr<bool> constraint = Logic.AllDistinct(mapped);

        var allDistinct = (AllDistinctNode)constraint.Node!;
        await Assert.That(allDistinct.Collection).IsTypeOf<MapNode>();
        await Assert.That(allDistinct.ElementType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task Map_EachCallUsesDistinctElementVarName()
    {
        var colVar = new VarNode("col", typeof(int[]));
        LogicExpr<int[]> col = new(colVar);

        LogicExpr<int[]> r1 = Logic.Map<int, int>(col, proxy =>
            new LogicExpr<int>(new VarNode(proxy.VariableName, typeof(int))));
        LogicExpr<int[]> r2 = Logic.Map<int, int>(col, proxy =>
            new LogicExpr<int>(new VarNode(proxy.VariableName, typeof(int))));

        var n1 = (MapNode)r1.Node!;
        var n2 = (MapNode)r2.Node!;
        await Assert.That(n1.ElementVar.Name).IsNotEqualTo(n2.ElementVar.Name);
    }
}
