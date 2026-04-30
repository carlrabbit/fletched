using Fletched.Core.Models;

namespace Fletched.Core.Tests.Models;

public class GenerationRequestTests
{
    [Test]
    public async Task GenerationRequest_StoresTargetAndFeature()
    {
        // Arrange
        TypeModel target = new("MyClass", "MyApp.Domain");

        // Act
        GenerationRequest request = new(target, "IProxy");

        // Assert
        await Assert.That(request.Target).IsEqualTo(target);
        await Assert.That(request.Feature).IsEqualTo("IProxy");
    }

    [Test]
    public async Task GenerationRequest_Equality_SameValues_AreEqual()
    {
        // Arrange
        TypeModel target = new("MyClass", "MyApp.Domain");

        // Act
        GenerationRequest a = new(target, "IProxy");
        GenerationRequest b = new(target, "IProxy");

        // Assert
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task GenerationRequest_Equality_DifferentFeature_AreNotEqual()
    {
        // Arrange
        TypeModel target = new("MyClass", "MyApp.Domain");

        // Act
        GenerationRequest a = new(target, "IProxy");
        GenerationRequest b = new(target, "IFactory");

        // Assert
        await Assert.That(a).IsNotEqualTo(b);
    }
}
