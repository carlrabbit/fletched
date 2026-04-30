using Fletched.Core.DSL;
using Fletched.Core.IR;

namespace Fletched.Core.Tests.DSL;

public class LogicExprTests
{
    [Test]
    public async Task EqualityOperator_ProducesUnifyNode()
    {
        // Arrange
        TerminalVar<string> name = new();
        LogicExpr<string> nameExpr = name;
        LogicExpr<string> aliceExpr = new(new ConstNode("Alice", typeof(string)));

        // Act
        LogicExpr<bool> result = nameExpr == aliceExpr;

        // Assert
        await Assert.That(result.Node).IsTypeOf<UnifyNode>();
    }

    [Test]
    public async Task AndOperator_ProducesConjNode()
    {
        // Arrange
        TerminalVar<string> name = new();
        LogicExpr<string> nameExpr = name;
        LogicExpr<string> alice = new(new ConstNode("Alice", typeof(string)));
        LogicExpr<string> bob = new(new ConstNode("Bob", typeof(string)));

        LogicExpr<bool> left = nameExpr == alice;
        LogicExpr<bool> right = nameExpr == bob;

        // Act
        LogicExpr<bool> result = left & right;

        // Assert
        await Assert.That(result.Node).IsTypeOf<ConjNode>();
        ConjNode conj = (ConjNode)result.Node;
        await Assert.That(conj.Parts.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AndOperator_FlattensNestedConjunctions()
    {
        // Arrange
        TerminalVar<string> name = new();
        LogicExpr<string> nameExpr = name;
        LogicExpr<string> alice = new(new ConstNode("Alice", typeof(string)));
        LogicExpr<string> bob = new(new ConstNode("Bob", typeof(string)));
        LogicExpr<string> carol = new(new ConstNode("Carol", typeof(string)));

        LogicExpr<bool> ab = (nameExpr == alice) & (nameExpr == bob);
        LogicExpr<bool> abc = ab & (nameExpr == carol);

        // Assert: flattened to 3 parts, not nested
        await Assert.That(abc.Node).IsTypeOf<ConjNode>();
        ConjNode conj = (ConjNode)abc.Node;
        await Assert.That(conj.Parts.Count).IsEqualTo(3);
    }

    [Test]
    public async Task OrOperator_ProducesDisjNode()
    {
        // Arrange
        TerminalVar<string> name = new();
        LogicExpr<string> nameExpr = name;
        LogicExpr<string> alice = new(new ConstNode("Alice", typeof(string)));
        LogicExpr<string> bob = new(new ConstNode("Bob", typeof(string)));

        LogicExpr<bool> left = nameExpr == alice;
        LogicExpr<bool> right = nameExpr == bob;

        // Act
        LogicExpr<bool> result = left | right;

        // Assert
        await Assert.That(result.Node).IsTypeOf<DisjNode>();
    }
}
