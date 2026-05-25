using System.Collections.Generic;
using Fletched.Core;
using Fletched.Core.Performance;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Performance.Tests;

// ── Domain types used by observer tests ─────────────────────────────────────

[Fact]
[FactIndex(nameof(ObserverProduct.Sku))]
[FactIndex(nameof(ObserverProduct.Category))]
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

[Predicate]
public partial record struct ObserverSkuLookupAfterBinding
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> sku) =>
        Logic.With<ObserverProduct>(p =>
            p.Sku == sku &&
            Logic.With<ObserverProduct>(q => q.Sku == sku));
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

[Fact]
[FactIndex(nameof(ObserverEdge.Parent))]
public partial record struct ObserverEdge(string Parent, string Child);

[Predicate]
public partial record struct ObserverAncestor
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<ObserverEdge>(edge => edge.Parent == parent && edge.Child == child) ||
        ObserverAncestorStep(parent, child);
}

[Predicate]
public partial record struct ObserverAncestorStep
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<string>(middle =>
            Logic.With<ObserverEdge>(edge => edge.Parent == parent && edge.Child == middle) &&
            ObserverAncestor(middle, child));
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
    public void OnRecursiveInvocation(string predicateName, int depth) => Events.Add(("RecursiveInvocation", (predicateName, depth)));
    public void OnRecursiveDepthExceeded(string predicateName, int depth, int maxDepth, bool insideNegation) =>
        Events.Add(("RecursiveDepthExceeded", (predicateName, depth, maxDepth, insideNegation)));
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

    private static EngineContext BuildRecursiveContext()
    {
        var ctx = new EngineContext();
        ctx.ObserverEdges = new FactTable<ObserverEdge>(new[]
        {
            new ObserverEdge("A", "B"),
            new ObserverEdge("B", "C"),
            new ObserverEdge("C", "D"),
        });
        return ctx;
    }

    // ── Null-observer overload ────────────────────────────────────────────────

    [Test]
    public async Task Execute_WithoutObserver_ProducesCorrectResults()
    {
        EngineContext ctx = BuildContext();
        var results = await default(ObserverSkus).ExecuteAsync(ctx).ToListAsync();
        await Assert.That(results.Count).IsEqualTo(3);
    }

    // ── Observer receives FactScan events ─────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesFactScanEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        await default(ObserverSkus).ExecuteAsync(ctx, observer).ToListAsync();

        bool hasFactScan = observer.Events.Any(e => e.Event == "FactScan");
        await Assert.That(hasFactScan).IsTrue();
    }

    [Test]
    public async Task Execute_WithUnboundKey_DoesNotReceiveIndexHitEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        default(ObserverSkus).Execute(ctx, observer).ToList();

        bool hasIndexHit = observer.Events.Any(e => e.Event == "IndexHit");
        await Assert.That(hasIndexHit).IsFalse();
    }

    // ── Observer receives Unify events ────────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesUnifyEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        await default(ObserverSkus).ExecuteAsync(ctx, observer).ToListAsync();

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
        await default(ObserverSkus).ExecuteAsync(ctx, observer).ToListAsync();

        bool hasBacktrack = observer.Events.Any(e => e.Event == "Backtrack");
        await Assert.That(hasBacktrack).IsTrue();
    }

    // ── Observer receives ChoicePoint events ──────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ReceivesChoicePointEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        await default(ObserverSkus).ExecuteAsync(ctx, observer).ToListAsync();

        bool hasChoicePoint = observer.Events.Any(e => e.Event == "ChoicePoint");
        await Assert.That(hasChoicePoint).IsTrue();
    }

    // ── Observer does not affect results ─────────────────────────────────────

    [Test]
    public async Task Execute_WithObserver_ProducesSameResultsAsWithout()
    {
        EngineContext ctx = BuildContext();
        var withoutObserver = (await default(ObserverSkus).ExecuteAsync(ctx).ToListAsync()).Select(r => r.sku).ToList();
        var withObserver = (await default(ObserverSkus).ExecuteAsync(ctx, new RecordingObserver()).ToListAsync()).Select(r => r.sku).ToList();

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

        await default(ObserverElectronics).ExecuteAsync(ctx, observer).ToListAsync();

        bool hasUnify = observer.Events.Any(e => e.Event == "Unify");
        await Assert.That(hasUnify).IsTrue();
    }

    [Test]
    public async Task ConstantFilterExecute_WithObserver_ReceivesIndexHitEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        default(ObserverElectronics).Execute(ctx, observer).ToList();

        bool hasIndexHit = observer.Events.Any(e => e.Event == "IndexHit" && Equals(e.Arg, "ObserverProduct"));
        await Assert.That(hasIndexHit).IsTrue();
    }

    // ── Cross-product join — verify UnifyFailure when join key mismatches ────

    [Test]
    public async Task JoinExecute_WithObserver_ReceivesUnifyFailureEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        // In the self-join, the inner loop binds 'sku' from p.Sku (outer fact).
        // For items where q.Sku differs from the bound 'sku', UnifyFailure fires.
        await default(ObserverSkuJoin).ExecuteAsync(ctx, observer).ToListAsync();

        bool hasUnifyFailure = observer.Events.Any(e => e.Event == "UnifyFailure");
        await Assert.That(hasUnifyFailure).IsTrue();
    }

    [Test]
    public async Task NestedLookupExecute_WithObserver_ReceivesIndexHitEvents()
    {
        EngineContext ctx = BuildContext();
        var observer = new RecordingObserver();

        var results = default(ObserverSkuLookupAfterBinding).Execute(ctx, observer).ToList();

        bool hasIndexHit = observer.Events.Any(e => e.Event == "IndexHit" && Equals(e.Arg, "ObserverProduct"));
        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(hasIndexHit).IsTrue();
    }

    [Test]
    public async Task RecursiveExecute_WithObserver_ReceivesRecursiveInvocationEvents()
    {
        EngineContext ctx = BuildRecursiveContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, 4);
        var observer = new RecordingObserver();
        try
        {
            _ = default(ObserverAncestor).Execute(ctx, observer).ToList();
        }
        catch (RecursiveDepthExceededException)
        {
        }

        bool hasRecursiveInvocation = observer.Events.Any(e => e.Event == "RecursiveInvocation");
        bool hasDepthExceeded = observer.Events.Any(e => e.Event == "RecursiveDepthExceeded");
        await Assert.That(hasRecursiveInvocation).IsTrue();
        await Assert.That(hasDepthExceeded).IsTrue();
    }
}
