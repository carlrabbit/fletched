using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

[Fact]
public partial record struct TabledParentEdge(string Parent, string Child);

[Predicate]
public partial record struct TabledParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<TabledParentEdge>(edge => edge.Parent == parent && edge.Child == child);
}

[Tabled]
[Predicate]
public partial record struct TabledAncestor
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        TabledParent(parent, child) ||
        TabledAncestorStep(parent, child);
}

[Predicate]
public partial record struct TabledAncestorStep
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<string>(middle =>
            TabledParent(parent, middle) &&
            TabledAncestor(middle, child));
}

public class TabledRecursiveTests
{
    private static EngineContext BuildAncestorContext()
    {
        var ctx = new EngineContext();
        ctx.TabledParentEdges = new FactTable<TabledParentEdge>(
        [
            new("A", "B"),
            new("A", "C"),
            new("B", "D"),
            new("C", "D"),
            new("D", "E"),
        ]);

        return ctx;
    }

    [Test]
    public async Task TabledDirectRecursion_AsyncReturnsExpectedTransitiveClosure()
    {
        EngineContext ctx = BuildAncestorContext();

        HashSet<(string Parent, string Child)> results = (await default(TabledAncestor)
                .ExecuteAsync(ctx)
                .ToListAsync())
            .Select(result => (result.parent, result.child))
            .ToHashSet();

        HashSet<(string Parent, string Child)> expected =
        [
            ("A", "B"),
            ("A", "C"),
            ("A", "D"),
            ("A", "E"),
            ("B", "D"),
            ("B", "E"),
            ("C", "D"),
            ("C", "E"),
            ("D", "E"),
        ];

        await Assert.That(results.SetEquals(expected)).IsTrue();
    }

    [Test]
    [Category("LongRunning")]
    [LongRunningIntegrationTest]
    public async Task TabledDirectRecursion_SyncAndAsyncReturnEquivalentSets()
    {
        EngineContext ctx = BuildAncestorContext();

        HashSet<(string Parent, string Child)> syncResults = default(TabledAncestor)
            .Execute(ctx)
            .Select(result => (result.parent, result.child))
            .ToHashSet();

        HashSet<(string Parent, string Child)> asyncResults = (await default(TabledAncestor)
                .ExecuteAsync(ctx)
                .ToListAsync())
            .Select(result => (result.parent, result.child))
            .ToHashSet();

        await Assert.That(asyncResults.SetEquals(syncResults)).IsTrue();
        await Assert.That(syncResults.Count).IsGreaterThan(0);
    }

    [Test]
    [Category("LongRunning")]
    [LongRunningIntegrationTest]
    public async Task TabledDirectRecursion_DuplicatePathsYieldSingleAnswer()
    {
        EngineContext ctx = BuildAncestorContext();

        int count = default(TabledAncestor).Execute(ctx)
            .Count(result => result.parent == "A" && result.child == "D");

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task TabledAsyncRecursion_HonorsCancellation()
    {
        EngineContext ctx = BuildAncestorContext();
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        System.OperationCanceledException? thrown = null;
        try
        {
            _ = await default(TabledAncestor).ExecuteAsync(ctx, cancellationToken: cts.Token).ToListAsync(cts.Token);
        }
        catch (System.OperationCanceledException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();
    }
}
