using Fletched.Core.IR;
using TUnit;

namespace Fletched.Core.Tests;

public class ListDslTests
{
    [Test]
    public async Task Empty_CreatesListEmptyNode_WithCorrectElementType()
    {
        LogicExpr<LogicList<int>> expr = Logic.Empty<int>();

        var node = (ListEmptyNode)expr.Node!;
        await Assert.That(node.ElementType).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task Empty_DifferentElementType_SetsCorrectElementType()
    {
        LogicExpr<LogicList<string>> expr = Logic.Empty<string>();

        var node = (ListEmptyNode)expr.Node!;
        await Assert.That(node.ElementType).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task Cons_CreatesListConsNode_WithCorrectHeadAndTail()
    {
        var headVar = new VarNode("h", typeof(int));
        LogicExpr<int> head = new(headVar);
        LogicExpr<LogicList<int>> tail = Logic.Empty<int>();

        LogicExpr<LogicList<int>> expr = Logic.Cons(head, tail);

        var node = (ListConsNode)expr.Node!;
        await Assert.That(node.Head).IsEqualTo(headVar);
        await Assert.That(node.Tail).IsEqualTo(tail.Node);
    }

    [Test]
    public async Task Cons_WithConstantHead_BuildsConsNode()
    {
        // Cons(1, Empty<int>()) — implicit conversion from literal
        LogicExpr<int> head = new(new ConstNode(1, typeof(int)));
        LogicExpr<LogicList<int>> tail = Logic.Empty<int>();

        LogicExpr<LogicList<int>> expr = Logic.Cons(head, tail);

        var node = (ListConsNode)expr.Node!;
        await Assert.That(node.Head).IsEqualTo(new ConstNode(1, typeof(int)));
        await Assert.That(node.Tail).IsTypeOf<ListEmptyNode>();
    }

    [Test]
    public async Task Cons_NestedCons_BuildsCorrectTree()
    {
        // Cons(1, Cons(2, Empty<int>()))
        LogicExpr<int> head1 = new(new ConstNode(1, typeof(int)));
        LogicExpr<int> head2 = new(new ConstNode(2, typeof(int)));
        LogicExpr<LogicList<int>> empty = Logic.Empty<int>();
        LogicExpr<LogicList<int>> inner = Logic.Cons(head2, empty);
        LogicExpr<LogicList<int>> outer = Logic.Cons(head1, inner);

        var outerNode = (ListConsNode)outer.Node!;
        await Assert.That(outerNode.Head).IsEqualTo(new ConstNode(1, typeof(int)));

        var innerNode = (ListConsNode)outerNode.Tail;
        await Assert.That(innerNode.Head).IsEqualTo(new ConstNode(2, typeof(int)));
        await Assert.That(innerNode.Tail).IsTypeOf<ListEmptyNode>();
    }

    [Test]
    public async Task Cons_ResultCanBeUnifiedWithVar()
    {
        // list == Cons(1, Empty<int>())
        var listVar = new VarNode("list", typeof(LogicList<int>));
        LogicExpr<LogicList<int>> listExpr = new(listVar);
        LogicExpr<LogicList<int>> cons = Logic.Cons<int>(
            new(new ConstNode(1, typeof(int))),
            Logic.Empty<int>());

        LogicExpr<bool> unify = listExpr == cons;

        var unifyNode = (UnifyNode)unify.Node!;
        await Assert.That(unifyNode.Left).IsEqualTo(listVar);
        await Assert.That(unifyNode.Right).IsTypeOf<ListConsNode>();
    }
}
