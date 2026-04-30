using Moq;
using Fletched.Abstractions;
using Fletched.Core.Models;
using Fletched.Features;

namespace Fletched.Features.Tests;

[NotInParallel]
public class FeatureRegistryTests
{
    [Before(Test)]
    [After(Test)]
    public void ClearRegistry() => FeatureRegistry.Clear();

    [Test]
    public async Task All_WhenEmpty_ReturnsEmptyList()
    {
        // Assert
        await Assert.That(FeatureRegistry.All).IsEmpty();
    }

    [Test]
    public async Task Register_AddsModuleToAll()
    {
        // Arrange
        Mock<IFeatureModule> mock = new();
        mock.SetupGet(m => m.Name).Returns("TestFeature");

        // Act
        FeatureRegistry.Register(mock.Object);

        // Assert
        await Assert.That(FeatureRegistry.All.Count).IsEqualTo(1);
        await Assert.That(FeatureRegistry.All[0].Name).IsEqualTo("TestFeature");
    }

    [Test]
    public async Task Register_MultipleModules_AllPresent()
    {
        // Arrange
        Mock<IFeatureModule> mock1 = new();
        mock1.SetupGet(m => m.Name).Returns("Feature1");
        Mock<IFeatureModule> mock2 = new();
        mock2.SetupGet(m => m.Name).Returns("Feature2");

        // Act
        FeatureRegistry.Register(mock1.Object);
        FeatureRegistry.Register(mock2.Object);

        // Assert
        await Assert.That(FeatureRegistry.All.Count).IsEqualTo(2);
    }
}
