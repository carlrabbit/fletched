using System.Collections.Generic;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

/// <summary>A toaster that is both a stored fact and a self-referential predicate.</summary>
[Fact, Predicate]
public partial record struct Toaster(string Name, int Size)
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name, TerminalVar<int> size) =>
        Logic.With<Toaster>(t =>
            t.Name == name &&
            t.Size == size
        );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class ToasterTests
{
    // Data set:
    //   ("Compact",  2)  – unique name, unique size
    //   ("Standard", 4)  – "Standard" appears twice (different sizes)
    //   ("Standard", 6)  – same name, different size
    //   ("Jumbo",    4)  – different name, same size as first "Standard"
    //   ("Deluxe",   8)  – unique name, unique size
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
        return ctx;
    }

    // ── Empty table ───────────────────────────────────────────────────────────

    [Test]
    public async Task Execute_EmptyFactTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.Toasters = new FactTable<Toaster>(System.Array.Empty<Toaster>());

        List<ToasterResult> results =
            default(Toaster).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── List all toasters ─────────────────────────────────────────────────────

    [Test]
    public async Task Execute_AllToasters_ReturnsAllRows()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(5);
    }

    // ── Filter by size ────────────────────────────────────────────────────────

    [Test]
    public async Task Execute_FilterBySize_ManyMatches()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).Where(r => r.size == 4).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_FilterBySize_OneMatch()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).Where(r => r.size == 2).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("Compact");
    }

    [Test]
    public async Task Execute_FilterBySize_NoMatch()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).Where(r => r.size == 99).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Filter by name ────────────────────────────────────────────────────────

    [Test]
    public async Task Execute_FilterByName_MultipleSizes()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).Where(r => r.name == "Standard").ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_FilterByName_OneSize()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).Where(r => r.name == "Deluxe").ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].size).IsEqualTo(8);
    }

    [Test]
    public async Task Execute_FilterByName_NoMatch()
    {
        EngineContext ctx = BuildContext();
        List<ToasterResult> results =
            default(Toaster).Execute(ctx).Where(r => r.name == "NonExistent").ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Check existence by name and size ─────────────────────────────────────

    [Test]
    public async Task Execute_FilterByNameAndSize_Exists()
    {
        EngineContext ctx = BuildContext();
        bool exists = default(Toaster).Execute(ctx)
            .Any(r => r.name == "Standard" && r.size == 4);

        await Assert.That(exists).IsTrue();
    }

    [Test]
    public async Task Execute_FilterByNameAndSize_DoesNotExist()
    {
        EngineContext ctx = BuildContext();
        bool exists = default(Toaster).Execute(ctx)
            .Any(r => r.name == "Standard" && r.size == 99);

        await Assert.That(exists).IsFalse();
    }
}
