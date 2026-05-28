using System.Globalization;

namespace Ontology.FoodSafety;

public static class CsvLoader
{
    public static SampleData Load(string root)
    {
        string data = Path.Combine(root, "Data");
        var concepts = Read(Path.Combine(data, "concepts.csv"), c => new FoodSafetyModule.FoodConcept(c["Id"], c["Label"], c["Kind"]));
        var products = Read(Path.Combine(data, "products.csv"), c => new FoodSafetyModule.Product(c["ProductId"], c["Name"], c["Category"]));
        var profiles = Read(Path.Combine(data, "profiles.csv"), c => new FoodSafetyModule.DietaryProfile(c["ProfileId"], c["Label"]));
        var subclassOf = Read(Path.Combine(data, "subclass-of.csv"), c => new FoodSafetyModule.SubClassOf(c["Child"], c["Parent"]));
        var productIngredients = Read(Path.Combine(data, "product-ingredients.csv"), c => new FoodSafetyModule.ProductIngredient(c["ProductId"], c["Ingredient"]));
        var avoids = Read(Path.Combine(data, "avoids.csv"), c => new FoodSafetyModule.Avoids(c["ProfileId"], c["Concept"]));
        return new SampleData(concepts, subclassOf, products, productIngredients, profiles, avoids);
    }

    private static List<T> Read<T>(string path, Func<IReadOnlyDictionary<string, string>, T> map)
    {
        string[] lines = File.ReadAllLines(path);
        string[] headers = lines[0].Split(',');
        var list = new List<T>();
        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cells = line.Split(',');
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < headers.Length; i++) row[headers[i]] = i < cells.Length ? cells[i].Trim() : string.Empty;
            list.Add(map(row));
        }
        return list;
    }
}
