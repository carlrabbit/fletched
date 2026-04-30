using Fletched.Core.IR;

namespace Fletched.Core.Tests.IR;

public class ExprNodeTests
{
    [Test]
    public async Task VarNode_Kind_IsVar()
    {
        // Arrange / Act
        VarNode node = new(typeof(string), 0);

        // Assert
        await Assert.That(node.Kind).IsEqualTo(NodeKind.Var);
    }

    [Test]
    public async Task VarNode_StoresTypeAndId()
    {
        // Arrange / Act
        VarNode node = new(typeof(int), 42);

        // Assert
        await Assert.That(node.Type).IsEqualTo(typeof(int));
        await Assert.That(node.Id).IsEqualTo(42);
    }

    [Test]
    public async Task ConstNode_Kind_IsConst()
    {
        // Arrange / Act
        ConstNode node = new("hello", typeof(string));

        // Assert
        await Assert.That(node.Kind).IsEqualTo(NodeKind.Const);
    }

    [Test]
    public async Task ConstNode_StoresValueAndType()
    {
        // Arrange / Act
        ConstNode node = new(99, typeof(int));

        // Assert
        await Assert.That(node.Value).IsEqualTo(99);
        await Assert.That(node.Type).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task UnifyNode_Kind_IsUnify()
    {
        // Arrange
        VarNode left = new(typeof(string), 0);
        ConstNode right = new("Alice", typeof(string));

        // Act
        UnifyNode node = new(left, right);

        // Assert
        await Assert.That(node.Kind).IsEqualTo(NodeKind.Unify);
    }

    [Test]
    public async Task UnifyNode_StoresLeftAndRight()
    {
        // Arrange
        VarNode left = new(typeof(string), 0);
        ConstNode right = new("Alice", typeof(string));

        // Act
        UnifyNode node = new(left, right);

        // Assert
        await Assert.That(node.Left).IsEqualTo(left);
        await Assert.That(node.Right).IsEqualTo(right);
    }

    [Test]
    public async Task ConjNode_Kind_IsConj()
    {
        // Arrange
        VarNode v = new(typeof(string), 0);
        ConstNode c = new("x", typeof(string));
        UnifyNode u = new(v, c);

        // Act
        ConjNode node = new([u]);

        // Assert
        await Assert.That(node.Kind).IsEqualTo(NodeKind.Conj);
    }

    [Test]
    public async Task ConjNode_StoresParts()
    {
        // Arrange
        VarNode v = new(typeof(string), 0);
        ConstNode c = new("x", typeof(string));
        UnifyNode u1 = new(v, c);
        UnifyNode u2 = new(c, v);

        // Act
        ConjNode node = new([u1, u2]);

        // Assert
        await Assert.That(node.Parts.Count).IsEqualTo(2);
        await Assert.That(node.Parts[0]).IsEqualTo(u1);
        await Assert.That(node.Parts[1]).IsEqualTo(u2);
    }

    [Test]
    public async Task DisjNode_Kind_IsDisj()
    {
        // Arrange
        VarNode v = new(typeof(string), 0);
        ConstNode left = new("Alice", typeof(string));
        ConstNode right = new("Bob", typeof(string));

        // Act
        DisjNode node = new(new UnifyNode(v, left), new UnifyNode(v, right));

        // Assert
        await Assert.That(node.Kind).IsEqualTo(NodeKind.Disj);
    }

    [Test]
    public async Task ConstraintNode_Kind_IsConstraint()
    {
        // Arrange
        System.Reflection.MethodInfo method =
            typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        VarNode arg = new(typeof(string), 0);
        ConstNode prefix = new("A", typeof(string));

        // Act
        ConstraintNode node = new(method, [arg, prefix]);

        // Assert
        await Assert.That(node.Kind).IsEqualTo(NodeKind.Constraint);
    }
}
