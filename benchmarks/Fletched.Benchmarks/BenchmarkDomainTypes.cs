namespace Fletched.Benchmarks;

/// <summary>Benchmark fact: an e-commerce product record.</summary>
public record struct BenchItem(string Sku, string Category, int Price);
