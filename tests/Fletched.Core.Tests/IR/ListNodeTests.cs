using System;
using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests.IR;

public class ListNodeTests
{
    // ── ListEmptyNode ────────────────────────────────────────────────────────

    [Test]
    public async Task ListEmptyNode_StoresElementType()
    {
        var node = new ListEmptyNode(typeof(int));
        await Assert.That(node.ElementType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task ListEmptyNode_Equality_SameElementType()
    {
        var a = new ListEmptyNode(typeof(string));
        var b = new ListEmptyNode(typeof(string));
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task ListEmptyNode_Inequality_DifferentElementType()
    {
        var a = new ListEmptyNode(typeof(int));
        var b = new ListEmptyNode(typeof(string));
        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task ListEmptyNode_IsExprNode()
    {
        ExprNode node = new ListEmptyNode(typeof(int));
        await Assert.That(node).IsTypeOf<ListEmptyNode>();
    }

    // ── ListConsNode ─────────────────────────────────────────────────────────

    [Test]
    public async Task ListConsNode_StoresHeadAndTail()
    {
        var head = new ConstNode(1, typeof(int));
        var tail = new ListEmptyNode(typeof(int));
        var node = new ListConsNode(head, tail);

        await Assert.That(node.Head).IsEqualTo(head);
        await Assert.That(node.Tail).IsEqualTo(tail);
    }

    [Test]
    public async Task ListConsNode_Equality_SameHeadAndTail()
    {
        var head = new ConstNode(42, typeof(int));
        var tail = new ListEmptyNode(typeof(int));
        var a = new ListConsNode(head, tail);
        var b = new ListConsNode(head, tail);

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task ListConsNode_Inequality_DifferentHead()
    {
        var tail = new ListEmptyNode(typeof(int));
        var a = new ListConsNode(new ConstNode(1, typeof(int)), tail);
        var b = new ListConsNode(new ConstNode(2, typeof(int)), tail);

        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task ListConsNode_IsExprNode()
    {
        var head = new ConstNode(1, typeof(int));
        var tail = new ListEmptyNode(typeof(int));
        ExprNode node = new ListConsNode(head, tail);

        await Assert.That(node).IsTypeOf<ListConsNode>();
    }

    [Test]
    public async Task ListConsNode_NestedCons_Equality()
    {
        // Cons(1, Cons(2, Empty))
        var empty = new ListEmptyNode(typeof(int));
        var inner = new ListConsNode(new ConstNode(2, typeof(int)), empty);
        var outer = new ListConsNode(new ConstNode(1, typeof(int)), inner);

        var empty2 = new ListEmptyNode(typeof(int));
        var inner2 = new ListConsNode(new ConstNode(2, typeof(int)), empty2);
        var outer2 = new ListConsNode(new ConstNode(1, typeof(int)), inner2);

        await Assert.That(outer).IsEqualTo(outer2);
    }
}
