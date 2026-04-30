using Fletched.Core.IR;

namespace Fletched.Core.Tests.IR;

public class NodeKindTests
{
    [Test]
    public async Task NodeKind_HasExpectedValues()
    {
        // Arrange / Act
        NodeKind[] kinds = Enum.GetValues<NodeKind>();

        // Assert
        await Assert.That(kinds).Contains(NodeKind.Var);
        await Assert.That(kinds).Contains(NodeKind.Const);
        await Assert.That(kinds).Contains(NodeKind.Field);
        await Assert.That(kinds).Contains(NodeKind.Unify);
        await Assert.That(kinds).Contains(NodeKind.Conj);
        await Assert.That(kinds).Contains(NodeKind.Disj);
        await Assert.That(kinds).Contains(NodeKind.Constraint);
    }
}
