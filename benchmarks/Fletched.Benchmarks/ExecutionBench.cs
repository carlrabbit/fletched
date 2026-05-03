using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Fletched.Core.Runtime;

namespace Fletched.Benchmarks;

/// <summary>
/// Benchmarks Fletched query execution patterns across varying dataset sizes.
/// The loops below replicate the control flow emitted by the source generator,
/// allowing performance measurement without requiring the generator to run at
/// benchmark build time.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(nameof(Fletched.Core.Performance.BenchmarkCategory.Execution))]
public class ExecutionBench
{
    // ── Dataset sizes ─────────────────────────────────────────────────────────

    [Params(10, 100, 1_000)]
    public int N { get; set; }

    // ── State ─────────────────────────────────────────────────────────────────

    private BenchItem[] _items = null!;

    [GlobalSetup]
    public void Setup()
    {
        _items = Enumerable.Range(1, N)
            .Select(i => new BenchItem(
                Sku: $"SKU-{i:D5}",
                Category: i % 3 == 0 ? "Electronics" : "Other",
                Price: i * 10))
            .ToArray();
    }

    // ── Benchmarks ────────────────────────────────────────────────────────────

    /// <summary>
    /// Full table scan: enumerates every item and binds its SKU field.
    /// Mirrors a generated predicate of the form <c>Logic.With&lt;T&gt;(x => x.Sku == sku)</c>.
    /// </summary>
    [Benchmark(Description = "Simple scan — enumerate all SKUs")]
    public int SimpleScan_AllSkus()
    {
        int count = 0;
        for (int i = 0; i < _items.Length; i++)
        {
            // Simulate: state.sku = item.Sku; state.sku_bound = true; yield return result;
            _ = _items[i].Sku;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Filtered scan: enumerates items and applies a category constraint.
    /// Mirrors a conjunction predicate with an extra equality check.
    /// </summary>
    [Benchmark(Description = "Filtered scan — Electronics category")]
    public int FilteredScan_Electronics()
    {
        int count = 0;
        for (int i = 0; i < _items.Length; i++)
        {
            // Simulate: sku unify + category constraint
            if (_items[i].Category != "Electronics") continue;
            _ = _items[i].Sku;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Single key lookup.
    /// Mirrors a predicate that scans for one specific SKU value.
    /// </summary>
    [Benchmark(Description = "Single lookup — first SKU")]
    public bool SingleLookup_BySku()
    {
        string target = "SKU-00001";
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].Sku == target) return true;
        }
        return false;
    }

    /// <summary>
    /// Disjunction pattern: matches items from two alternative branches (A or B category).
    /// Mirrors a predicate with a <c>||</c> body.
    /// </summary>
    [Benchmark(Description = "Disjunction — two-branch scan")]
    public int DisjunctionScan_TwoBranches()
    {
        int count = 0;
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].Price < 500 || _items[i].Category == "Electronics")
            {
                _ = _items[i].Sku;
                count++;
            }
        }
        return count;
    }
}

