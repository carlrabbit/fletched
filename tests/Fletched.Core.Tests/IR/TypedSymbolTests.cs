using Fletched.Core.IR;

namespace Fletched.Core.Tests.IR;

public class TypedSymbolTests
{
    [Test]
    public async Task TypedSymbol_Fact_HasCorrectKind()
    {
        // Arrange / Act
        TypedSymbol symbol = new("User", "MyApp", TypedSymbolKind.Fact, []);

        // Assert
        await Assert.That(symbol.Kind).IsEqualTo(TypedSymbolKind.Fact);
    }

    [Test]
    public async Task TypedSymbol_Predicate_HasCorrectKind()
    {
        // Arrange / Act
        TypedSymbol symbol = new("AdminUsers", "MyApp", TypedSymbolKind.Predicate, []);

        // Assert
        await Assert.That(symbol.Kind).IsEqualTo(TypedSymbolKind.Predicate);
    }

    [Test]
    public async Task TypedSymbol_StoresNameAndNamespace()
    {
        // Arrange / Act
        TypedSymbol symbol = new("User", "MyApp.Domain", TypedSymbolKind.Fact, []);

        // Assert
        await Assert.That(symbol.Name).IsEqualTo("User");
        await Assert.That(symbol.Namespace).IsEqualTo("MyApp.Domain");
    }

    [Test]
    public async Task TypedSymbol_StoresFields()
    {
        // Arrange
        TypedField loginField = new("Login", "string");
        TypedField nameField = new("Name", "string");

        // Act
        TypedSymbol symbol = new("User", "MyApp", TypedSymbolKind.Fact, [loginField, nameField]);

        // Assert
        await Assert.That(symbol.Fields.Count).IsEqualTo(2);
        await Assert.That(symbol.Fields[0].Name).IsEqualTo("Login");
        await Assert.That(symbol.Fields[1].Name).IsEqualTo("Name");
    }

    [Test]
    public async Task TypedSymbol_Equality_SameValues_AreEqual()
    {
        // Arrange — use the same fields list instance so record equality holds
        IReadOnlyList<TypedField> fields = [new TypedField("Login", "string")];
        TypedSymbol a = new("User", "MyApp", TypedSymbolKind.Fact, fields);
        TypedSymbol b = new("User", "MyApp", TypedSymbolKind.Fact, fields);

        // Act / Assert
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task TypedField_StoresNameAndType()
    {
        // Arrange / Act
        TypedField field = new("Login", "string");

        // Assert
        await Assert.That(field.Name).IsEqualTo("Login");
        await Assert.That(field.TypeDisplayString).IsEqualTo("string");
    }
}
