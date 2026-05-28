namespace Ontology.FoodSafety;

public static class Program
{
    public static int Main()
    {
        string root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Data"))) root = Directory.GetParent(root)!.FullName;
        SampleData data = CsvLoader.Load(root);

        var ctx = new FoodSafetyModule.EngineContext
        {
            FoodConcepts = new(data.Concepts),
            SubClassOfs = new(data.SubclassOf),
            Products = new(data.Products),
            ProductIngredients = new(data.ProductIngredients),
            DietaryProfiles = new(data.Profiles),
            Avoids = new(data.Avoids)
        };

        ConsoleReporter.Print("Unsafe products for nut_free");
        foreach (var row in default(FoodSafetyModule.UnsafeProductForProfile).ExecuteArity4(ctx).Where(r => r.profileId == "nut_free").OrderBy(r => r.productId))
            Console.WriteLine($"{row.productId} ingredient={row.ingredient} reason={row.reasonConcept}");

        ConsoleReporter.Print("Safe products for nut_free");
        foreach (var row in default(FoodSafetyModule.SafeProductForProfile).ExecuteArity2(ctx).Where(r => r.profileId == "nut_free").OrderBy(r => r.productId))
            Console.WriteLine(row.productId);

        ConsoleReporter.Print("Major allergen classification");
        foreach (var row in default(FoodSafetyModule.ProductHasMajorAllergen).ExecuteArity2(ctx).OrderBy(r => r.productId).ThenBy(r => r.ingredient))
            Console.WriteLine($"{row.productId} -> {row.ingredient}");

        return 0;
    }
}
