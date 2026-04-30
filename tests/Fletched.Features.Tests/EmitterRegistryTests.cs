using Moq;
using Fletched.Core.Models;
using Fletched.Emitters;

namespace Fletched.Features.Tests;

[NotInParallel]
public class EmitterRegistryTests
{
    [Before(Test)]
    [After(Test)]
    public void ClearRegistry() => EmitterRegistry.Clear();

    [Test]
    public async Task Get_AfterRegister_ReturnsRegisteredEmitter()
    {
        // Arrange
        Mock<ICodeEmitter> mock = new();
        mock.SetupGet(e => e.Feature).Returns("IProxy");
        EmitterRegistry.Register(mock.Object);

        // Act
        ICodeEmitter result = EmitterRegistry.Get("IProxy");

        // Assert
        await Assert.That(result).IsEqualTo(mock.Object);
    }

    [Test]
    public async Task Register_OverwritesExistingEmitterForSameFeature()
    {
        // Arrange
        Mock<ICodeEmitter> first = new();
        first.SetupGet(e => e.Feature).Returns("IProxy");
        Mock<ICodeEmitter> second = new();
        second.SetupGet(e => e.Feature).Returns("IProxy");

        EmitterRegistry.Register(first.Object);
        EmitterRegistry.Register(second.Object);

        // Act
        ICodeEmitter result = EmitterRegistry.Get("IProxy");

        // Assert
        await Assert.That(result).IsEqualTo(second.Object);
    }

    [Test]
    public async Task Get_UnknownFeature_ThrowsKeyNotFoundException()
    {
        // Act / Assert
        await Assert.That(() => EmitterRegistry.Get("Unknown"))
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task Emit_DelegatesToRegisteredEmitter()
    {
        // Arrange
        TypeModel target = new("MyClass", "MyApp");
        GenerationRequest request = new(target, "IProxy");

        Mock<ICodeEmitter> mock = new();
        mock.SetupGet(e => e.Feature).Returns("IProxy");
        mock.Setup(e => e.Emit(request)).Returns("// generated");
        EmitterRegistry.Register(mock.Object);

        // Act
        string source = EmitterRegistry.Get("IProxy").Emit(request);

        // Assert
        await Assert.That(source).IsEqualTo("// generated");
    }
}
