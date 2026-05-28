using Fletched.Core;

namespace Ontology.FoodSafety;

public static partial class FoodSafetyModule
{
    [Fact]
    public readonly partial record struct FoodConcept(string Id, string Label, string Kind);

    [Fact]
    [FactIndex(nameof(SubClassOf.Child))]
    [FactIndex(nameof(SubClassOf.Parent))]
    public readonly partial record struct SubClassOf(string Child, string Parent);

    [Fact]
    [FactIndex(nameof(Product.ProductId))]
    public readonly partial record struct Product(string ProductId, string Name, string Category);

    [Fact]
    [FactIndex(nameof(ProductIngredient.ProductId))]
    [FactIndex(nameof(ProductIngredient.Ingredient))]
    public readonly partial record struct ProductIngredient(string ProductId, string Ingredient);

    [Fact]
    [FactIndex(nameof(DietaryProfile.ProfileId))]
    public readonly partial record struct DietaryProfile(string ProfileId, string Label);

    [Fact]
    [FactIndex(nameof(Avoids.ProfileId))]
    [FactIndex(nameof(Avoids.Concept))]
    public readonly partial record struct Avoids(string ProfileId, string Concept);
}
