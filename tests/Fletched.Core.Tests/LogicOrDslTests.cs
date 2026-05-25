using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests;

public class LogicOrDslTests
{
    [Test]
    public async Task Or_WithExpressionBranches_BuildsNestedDisjunction()
    {
        LogicExpr<bool> expr = Logic.Or(
            new LogicExpr<bool>(new ConstNode(true, typeof(bool))),
            new LogicExpr<bool>(new ConstNode(false, typeof(bool))),
            new LogicExpr<bool>(new ConstNode(true, typeof(bool))));

        var root = (DisjNode)expr.Node!;
        await Assert.That(root.Left).IsTypeOf<DisjNode>();
        await Assert.That(root.Right).IsEqualTo(new ConstNode(true, typeof(bool)));
    }

    [Test]
    public async Task Or_WithLambdaBranches_BuildsNestedDisjunction()
    {
        LogicExpr<bool> expr = Logic.Or(
            () => new LogicExpr<bool>(new ConstNode(true, typeof(bool))),
            () => new LogicExpr<bool>(new ConstNode(false, typeof(bool))));

        await Assert.That(expr.Node).IsTypeOf<DisjNode>();
    }
}
