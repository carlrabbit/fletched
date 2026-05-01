using Fletched.Core.IR;
using Fletched.Roslyn.Pipeline;

namespace Fletched.Features.Tests;

public class TypedIrToPlanIrMapperTests
{
    [Test]
    public async Task Map_FactSymbol_ReturnsPlanProgramWithEntryBlock()
    {
        // Arrange
        TypedSymbol typed = new("User", "MyApp", TypedSymbolKind.Fact, []);

        // Act
        PlanProgram plan = TypedIrToPlanIrMapper.Map(typed);

        // Assert
        await Assert.That(plan).IsNotNull();
        await Assert.That(plan.Entry).IsNotNull();
    }

    [Test]
    public async Task Map_FactSymbol_EntryBlockLabelContainsNamespaceAndName()
    {
        // Arrange
        TypedSymbol typed = new("User", "MyApp", TypedSymbolKind.Fact, []);

        // Act
        PlanProgram plan = TypedIrToPlanIrMapper.Map(typed);

        // Assert
        await Assert.That(plan.Entry.Label).IsEqualTo("MyApp.User");
    }

    [Test]
    public async Task Map_PredicateSymbol_ReturnsPlanProgramWithEntryBlock()
    {
        // Arrange
        TypedSymbol typed = new("AdminUsers", "MyApp", TypedSymbolKind.Predicate, []);

        // Act
        PlanProgram plan = TypedIrToPlanIrMapper.Map(typed);

        // Assert
        await Assert.That(plan).IsNotNull();
        await Assert.That(plan.Entry).IsNotNull();
    }

    [Test]
    public async Task Map_EntryBlockHasReturnTerminator()
    {
        // Arrange
        TypedSymbol typed = new("User", "MyApp", TypedSymbolKind.Fact, []);

        // Act
        PlanProgram plan = TypedIrToPlanIrMapper.Map(typed);

        // Assert
        await Assert.That(plan.Entry.Terminator).IsTypeOf<ReturnTerminator>();
    }

    [Test]
    public async Task Map_EntryBlockHasNoInstructions()
    {
        // Arrange
        TypedSymbol typed = new("User", "MyApp", TypedSymbolKind.Fact, []);

        // Act
        PlanProgram plan = TypedIrToPlanIrMapper.Map(typed);

        // Assert
        await Assert.That(plan.Entry.Instructions).IsEmpty();
    }

    [Test]
    public async Task Map_BlocksContainsEntryBlock()
    {
        // Arrange
        TypedSymbol typed = new("User", "MyApp", TypedSymbolKind.Fact, []);

        // Act
        PlanProgram plan = TypedIrToPlanIrMapper.Map(typed);

        // Assert
        await Assert.That(plan.Blocks).Contains(plan.Entry);
    }
}
