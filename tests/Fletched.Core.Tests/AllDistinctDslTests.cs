using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests;

public class AllDistinctDslTests
{
    [Test]
    public async Task AllDistinct_CreatesAllDistinctNode()
    {
        var valuesVar = new VarNode("col", typeof(int[]));
        LogicExpr<int[]> values = new(valuesVar);

        LogicExpr<bool> expr = Logic.AllDistinct(values);

        var node = (AllDistinctNode)expr.Node!;
        await Assert.That(node.Collection).IsEqualTo(valuesVar);
        await Assert.That(node.ElementType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task AllDistinct_WithStringType_SetsCorrectElementType()
    {
        var valuesVar = new VarNode("names", typeof(string[]));
        LogicExpr<string[]> values = new(valuesVar);

        LogicExpr<bool> expr = Logic.AllDistinct(values);

        var node = (AllDistinctNode)expr.Node!;
        await Assert.That(node.ElementType).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task AllDistinct_CanBeUsedInConjunction()
    {
        var namesVar = new VarNode("names", typeof(string[]));
        LogicExpr<string[]> names = new(namesVar);
        var xVar = new VarNode("x", typeof(bool));
        LogicExpr<bool> otherExpr = new(xVar);

        // AllDistinct returns a LogicExpr<bool>, so it can be combined with &&
        LogicExpr<bool> combined = Logic.AllDistinct(names) & otherExpr;

        var conj = (ConjNode)combined.Node!;
        await Assert.That(conj.Parts.Count).IsEqualTo(2);
        await Assert.That(conj.Parts[0]).IsTypeOf<AllDistinctNode>();
    }
}
