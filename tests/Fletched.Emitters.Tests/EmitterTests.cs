using Fletched.Core.IR;
using Fletched.Emitters;

namespace Fletched.Emitters.Tests;

public class EmitterTests
{
    private static PlanBlock EmptyBlock() =>
        new("test", [], new NoopTerminator());

    private sealed record NoopTerminator : PlanTerminator;

    [Test]
    public async Task StateEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        StateEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SlotMappingEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        SlotMappingEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ExpressionEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        ExpressionEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ControlFlowEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        ControlFlowEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FactAccessEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        FactAccessEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task PredicateCallEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        PredicateCallEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FrameEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        FrameEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResultProjectionEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        ResultProjectionEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task MethodEmitter_Emit_ReturnsEmpty()
    {
        // Arrange
        MethodEmitter emitter = new();

        // Act
        string result = emitter.Emit(EmptyBlock());

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }
}
