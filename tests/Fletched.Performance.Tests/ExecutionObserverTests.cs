using System.Collections.Generic;
using Fletched.Core;
using Fletched.Core.Performance;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Performance.Tests;

// ── Domain types used by observer tests ─────────────────────────────────────

[Fact]
public partial record struct ObserverProduct(string Sku, string Category, int Price);

[Predicate]
public partial record struct ObserverSkus
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> sku) =>
        Logic.With<ObserverProduct>(p => p.Sku == sku);
}

[Predicate]
public partial record struct ObserverElectronics
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> sku) =>
        Logic.With<ObserverProduct>(p => p.Sku == sku && p.Category == "Electronics");
}

/// <summary>
/// Cross-product join: finds pairs of products that share the same SKU.
/// When the inner loop's SKU differs from the already-bound outer SKU,
/// <see cref="IExecutionObserver.OnUnifyFailure"/> is raised.
/// </summary>
[Predicate]
public partial record struct ObserverSkuJoin
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> sku) =>
        Logic.With<ObserverProduct>(p =>
            Logic.With<ObserverProduct>(q =>
                p.Sku == sku && q.Sku == sku));
}

// ── Recording observer implementation ───────────────────────────────────────

internal sealed class RecordingObserver : IExecutionObserver
{
    public List<(string Event, object? Arg)> Events { get; } = new();

    public void OnUnify(int slotId) => Events.Add(("Unify", slotId));
    public void OnUnifyFailure(int slotId) => Events.Add(("UnifyFailure", slotId));
    public void OnBacktrack() => Events.Add(("Backtrack", null));
    public void OnChoicePoint() => Events.Add(("ChoicePoint", null));
    public void OnFactScan(string factName) => Events.Add(("FactScan", factName));
    public void OnIndexHit(string factName) => Events.Add(("IndexHit", factName));
    public void OnPredicateInvocation(string predicateName) => Events.Add(("PredicateInvocation", predicateName));
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class ExecutionObserverTests
{
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.ObserverProducts = new FactTable<ObserverProduct>(new[]
        {
            new ObserverProduct("SKU-001", "Electronics", 299),
            new ObserverProduct("SKU-002", "Books",       15),
            new ObserverProduct("SKU-003", "Electronics", 499),
        });
        return ctx;
    }

    // ── Null-observer overload ────────────────────────────────────────────────

    [Test]
    public async Task Execute_WithoutObserver_ProducesCorrectResults()
    {
        EngineContext ctx = BuildContext();
        var results = default(ObserverSkus).Execute(ctx).ToList();
        await Assert.That(results.Count).IsEqualTo(3);
    }

    // ── Observer receives FactScan events ─────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesFactScanEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        default(ObserverSkus).Execute(ctx, observer).ToList();

        bool hasFactScan = observer.Events.Any(e => e.Event == "FactScan");
        await Assert.That(hasFactScan).IsTrue();
    }

    // ── Observer receives Unify events ────────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesUnifyEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        default(ObserverSkus).Execute(ctx, observer).ToList();

        bool hasUnify = observer.Events.Any(e => e.Event == "Unify");
        await Assert.That(hasUnify).IsTrue();
    }

    // ── Observer receives Backtrack events ────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesBacktrackEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        // With 3 items in the table, there will be backtracking after each yield.
        default(ObserverSkus).Execute(ctx, observer).ToList();

        bool hasBacktrack = observer.Events.Any(e => e.Event == "Backtrack");
        await Assert.That(hasBacktrack).IsTrue();
    }

    // ── Observer receives ChoicePoint events ──────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesChoicePointEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        default(ObserverSkus).Execute(ctx, observer).ToList();

        bool hasChoicePoint = observer.Events.Any(e => e.Event == "ChoicePoint");
        await Assert.That(hasChoicePoint).IsTrue();
    }

    // ── Observer does not affect results ─────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ProducesSameResultsAsWithout()
    {
        EngineContext ctx = BuildContext();
        var withoutObserver = default(ObserverSkus).Execute(ctx).Select(r => r.sku).ToList();
        var withObserver = default(ObserverSkus).Execute(ctx, new RecordingObserver()).Select(r => r.sku).ToList();

        await Assert.That(withObserver.Count).IsEqualTo(withoutObserver.Count);
        for (int i = 0; i < withoutObserver.Count; i++)
            await Assert.That(withObserver[i]).IsEqualTo(withoutObserver[i]);
    }

    // ── Electronics filter — verify observer with constraint ─────────────────

    [Test]
    public async Task FilteredExecute_WithObserver_ReceivesUnifyEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        default(ObserverElectronics).Execute(ctx, observer).ToList();

        bool hasUnify = observer.Events.Any(e => e.Event == "Unify");
        await Assert.That(hasUnify).IsTrue();
    }

    // ── Cross-product join — verify UnifyFailure when join key mismatches ────

    [Test]
    public async Task JoinExecute_WithObserver_ReceivesUnifyFailureEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        // In the self-join, the inner loop binds 'sku' from p.Sku (outer fact).
        // For items where q.Sku differs from the bound 'sku', UnifyFailure fires.
        default(ObserverSkuJoin).Execute(ctx, observer).ToList();

        bool hasUnifyFailure = observer.Events.Any(e => e.Event == "UnifyFailure");
        await Assert.That(hasUnifyFailure).IsTrue();
    }
}
