using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests.IR;

public class ExprNodeTests
{
    [Test]
    public async Task VarNode_Equality()
    {
        var a = new VarNode("x", typeof(string));
        var b = new VarNode("x", typeof(string));
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task VarNode_DifferentName_NotEqual()
    {
        var a = new VarNode("x", typeof(string));
        var b = new VarNode("y", typeof(string));
        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task ConstNode_NullValue()
    {
        var c = new ConstNode(null, typeof(string));
        await Assert.That(c.Value).IsNull();
        await Assert.That(c.Type).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task ConjNormalizer_FlattensNestedConj()
    {
        var a = new VarNode("a", typeof(bool));
        var b = new VarNode("b", typeof(bool));
        var c = new VarNode("c", typeof(bool));
        var nested = new ConjNode(new ExprNode[] {
            new ConjNode(new ExprNode[] { a, b }),
            c
        });
        ExprNode result = ConjNormalizer.Normalize(nested);
        var flat = (ConjNode)result;
        await Assert.That(flat.Parts.Count).IsEqualTo(3);
        await Assert.That(flat.Parts[0]).IsEqualTo(a);
        await Assert.That(flat.Parts[1]).IsEqualTo(b);
        await Assert.That(flat.Parts[2]).IsEqualTo(c);
    }

    [Test]
    public async Task ConjNormalizer_SingleLevel_Unchanged()
    {
        var a = new VarNode("a", typeof(bool));
        var b = new VarNode("b", typeof(bool));
        var conj = new ConjNode(new ExprNode[] { a, b });
        ExprNode result = ConjNormalizer.Normalize(conj);
        var flat = (ConjNode)result;
        await Assert.That(flat.Parts.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ConjNormalizer_NonConj_Unchanged()
    {
        var v = new VarNode("x", typeof(int));
        ExprNode result = ConjNormalizer.Normalize(v);
        await Assert.That(result).IsEqualTo(v);
    }

    [Test]
    public async Task DisjNode_HasLeftAndRight()
    {
        var left = new VarNode("a", typeof(bool));
        var right = new VarNode("b", typeof(bool));
        var disj = new DisjNode(left, right);
        await Assert.That(disj.Left).IsEqualTo(left);
        await Assert.That(disj.Right).IsEqualTo(right);
    }

    [Test]
    public async Task UnifyNode_HasLeftAndRight()
    {
        var left = new VarNode("x", typeof(string));
        var right = new ConstNode("hello", typeof(string));
        var unify = new UnifyNode(left, right);
        await Assert.That(unify.Left).IsEqualTo(left);
        await Assert.That(unify.Right).IsEqualTo(right);
    }

    [Test]
    public async Task NotNode_WrapsGoal()
    {
        var goal = new VarNode("x", typeof(bool));
        var not = new NotNode(goal);
        await Assert.That(not.Goal).IsEqualTo(goal);
    }

    [Test]
    public async Task NotNode_Equality()
    {
        var goal = new ConstNode(true, typeof(bool));
        var a = new NotNode(goal);
        var b = new NotNode(goal);
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task NotNode_DifferentGoals_NotEqual()
    {
        var a = new NotNode(new VarNode("x", typeof(bool)));
        var b = new NotNode(new VarNode("y", typeof(bool)));
        await Assert.That(a).IsNotEqualTo(b);
    }
}
