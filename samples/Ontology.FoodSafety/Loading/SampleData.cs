namespace Ontology.FoodSafety;

public sealed record SampleData(
    IReadOnlyList<FoodSafetyModule.FoodConcept> Concepts,
    IReadOnlyList<FoodSafetyModule.SubClassOf> SubclassOf,
    IReadOnlyList<FoodSafetyModule.Product> Products,
    IReadOnlyList<FoodSafetyModule.ProductIngredient> ProductIngredients,
    IReadOnlyList<FoodSafetyModule.DietaryProfile> Profiles,
    IReadOnlyList<FoodSafetyModule.Avoids> Avoids);
