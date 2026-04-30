using Fletched.Core.Models;

namespace Fletched.Core.Tests.Models;

public class TypeModelTests
{
    [Test]
    public async Task TypeModel_StoresNameAndNamespace()
    {
        // Arrange / Act
        TypeModel model = new("MyClass", "MyApp.Domain");

        // Assert
        await Assert.That(model.Name).IsEqualTo("MyClass");
        await Assert.That(model.Namespace).IsEqualTo("MyApp.Domain");
    }

    [Test]
    public async Task TypeModel_Equality_SameValues_AreEqual()
    {
        // Arrange
        TypeModel a = new("Foo", "Bar");
        TypeModel b = new("Foo", "Bar");

        // Act / Assert
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task TypeModel_Equality_DifferentName_AreNotEqual()
    {
        // Arrange
        TypeModel a = new("Foo", "Bar");
        TypeModel b = new("Baz", "Bar");

        // Act / Assert
        await Assert.That(a).IsNotEqualTo(b);
    }
}
