using System.Collections.Generic;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

/// <summary>A bread slice with a brand and a slot size.</summary>
[Fact]
public partial record struct Bread(string Brand, int Size);

// ── Predicates ────────────────────────────────────────────────────────────────

/// <summary>
/// Returns every (toaster name, bread brand) pair where the bread fits the toaster
/// (i.e. both share the same slot size).
/// </summary>
[Predicate]
public partial record struct RightSizedBread
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> toasterName, TerminalVar<string> brand) =>
        Logic.With<Toaster, Bread>((t, b) =>
            t.Name == toasterName &&
            b.Brand == brand &&
            t.Size == b.Size
        );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class BreadTests
{
    // Toasters (shared with ToasterTests data set):
    //   ("Compact",  2)
    //   ("Standard", 4)
    //   ("Standard", 6)
    //   ("Jumbo",    4)
    //   ("Deluxe",   8)
    //
    // Breads:
    //   ("WheatWonder",   2)  – fits only Compact
    //   ("SourdoughSlim", 4)  – fits Standard(4) and Jumbo
    //   ("RusticRye",     6)  – fits only Standard(6)
    //   ("GiantLoaf",    12)  – fits nobody
    //   ("MultiGrain",    4)  – fits Standard(4) and Jumbo
    //
    // Expected join results (6 rows):
    //   (Compact,  WheatWonder)
    //   (Standard, SourdoughSlim)
    //   (Standard, MultiGrain)
    //   (Jumbo,    SourdoughSlim)
    //   (Jumbo,    MultiGrain)
    //   (Standard, RusticRye)     ← the size-6 Standard
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.Toasters = new FactTable<Toaster>(new[]
        {
            new Toaster("Compact",  2),
            new Toaster("Standard", 4),
            new Toaster("Standard", 6),
            new Toaster("Jumbo",    4),
            new Toaster("Deluxe",   8),
        });
        ctx.Breads = new FactTable<Bread>(new[]
        {
            new Bread("WheatWonder",    2),
            new Bread("SourdoughSlim",  4),
            new Bread("RusticRye",      6),
            new Bread("GiantLoaf",     12),
            new Bread("MultiGrain",     4),
        });
        return ctx;
    }

    // ── Empty tables ──────────────────────────────────────────────────────────

    [Test]
    public async Task Execute_EmptyToasterTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.Toasters = new FactTable<Toaster>(System.Array.Empty<Toaster>());
        ctx.Breads = new FactTable<Bread>(new[] { new Bread("WheatWonder", 2) });

        List<RightSizedBreadResult> results =
            await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_EmptyBreadTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.Toasters = new FactTable<Toaster>(new[] { new Toaster("Compact", 2) });
        ctx.Breads = new FactTable<Bread>(System.Array.Empty<Bread>());

        List<RightSizedBreadResult> results =
            await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Full join ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Execute_AllPairs_ReturnsExpectedCount()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(6);
    }

    // ── Filter by toaster name ────────────────────────────────────────────────

    [Test]
    public async Task Execute_FilterByToasterName_Compact_OneResult()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
                .Where(r => r.toasterName == "Compact")
                .ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].brand).IsEqualTo("WheatWonder");
    }

    [Test]
    public async Task Execute_FilterByToasterName_Jumbo_TwoResults()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
                .Where(r => r.toasterName == "Jumbo")
                .ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_FilterByToasterName_Deluxe_NoResults()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
                .Where(r => r.toasterName == "Deluxe")
                .ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Filter by bread brand ─────────────────────────────────────────────────

    [Test]
    public async Task Execute_FilterByBrand_RusticRye_OneResult()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
                .Where(r => r.brand == "RusticRye")
                .ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].toasterName).IsEqualTo("Standard");
    }

    [Test]
    public async Task Execute_FilterByBrand_SourdoughSlim_TwoResults()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
                .Where(r => r.brand == "SourdoughSlim")
                .ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_FilterByBrand_GiantLoaf_NoResults()
    {
        EngineContext ctx = BuildContext();
        List<RightSizedBreadResult> results =
            (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
                .Where(r => r.brand == "GiantLoaf")
                .ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Exact pair lookup ─────────────────────────────────────────────────────

    [Test]
    public async Task Execute_ExactPair_CompactWheatWonder_Exists()
    {
        EngineContext ctx = BuildContext();
        bool exists = (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.toasterName == "Compact" && r.brand == "WheatWonder");

        await Assert.That(exists).IsTrue();
    }

    [Test]
    public async Task Execute_ExactPair_CompactSourdoughSlim_DoesNotExist()
    {
        EngineContext ctx = BuildContext();
        bool exists = (await default(RightSizedBread).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.toasterName == "Compact" && r.brand == "SourdoughSlim");

        await Assert.That(exists).IsFalse();
    }
}
