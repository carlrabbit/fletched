using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Performance.Tests;

[Fact]
[FactIndex(nameof(MetricUser.Login))]
public partial record struct MetricUser(string Login, bool Active);

[Fact]
[FactIndex(nameof(MetricEdge.Parent))]
public partial record struct MetricEdge(string Parent, string Child);

[Predicate]
public partial record struct MetricUsersScan
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<MetricUser>(user => user.Login == login);
}

[Predicate]
public partial record struct MetricUsersLookupHit
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<MetricUser>(user => user.Login == "u-001" && user.Login == login);
}

[Predicate]
public partial record struct MetricUsersLookupMiss
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<MetricUser>(user => user.Login == "u-missing" && user.Login == login);
}

[Predicate]
public partial record struct MetricConstraintFilter
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<MetricUser>(user => user.Login == login && user.Login != "u-001");
}

[Predicate]
public partial record struct MetricJoin
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<MetricUser>(left =>
            Logic.With<MetricUser>(right =>
                left.Login == login &&
                right.Login == login));
}

[Predicate]
public partial record struct MetricCallee
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<MetricUser>(user => user.Login == login);
}

[Predicate]
public partial record struct MetricCaller
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        MetricCallee(login);
}

[Predicate]
public partial record struct MetricParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<MetricEdge>(edge => edge.Parent == parent && edge.Child == child);
}

[Tabled]
[Predicate]
public partial record struct MetricAncestor
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        MetricParent(parent, child) ||
        MetricAncestorStep(parent, child);
}

[Predicate]
public partial record struct MetricAncestorStep
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<string>(middle =>
            MetricParent(parent, middle) &&
            MetricAncestor(middle, child));
}

public class QueryScopedMetricsTests
{
    [Test]
    public async Task QueryMetrics_ScanAndResultCounters_AreIncremented()
    {
        EngineContext ctx = BuildContext();
        var metrics = new QueryMetrics();

        int resultCount = default(MetricUsersScan).Execute(ctx, options: new QueryExecutionOptions { Metrics = metrics }).Count();

        await Assert.That(resultCount).IsEqualTo(3);
        await Assert.That(metrics.FactRowsScanned).IsGreaterThan(0);
        await Assert.That(metrics.ResultsEmitted).IsEqualTo(3);
    }

    [Test]
    public async Task QueryMetrics_IndexLookupHitAndMiss_AreIncremented()
    {
        EngineContext ctx = BuildContext();

        var hitMetrics = new QueryMetrics();
        _ = default(MetricUsersLookupHit).Execute(ctx, options: new QueryExecutionOptions { Metrics = hitMetrics }).ToList();
        await Assert.That(hitMetrics.IndexLookups).IsGreaterThan(0);
        await Assert.That(hitMetrics.IndexHits).IsGreaterThan(0);
        await Assert.That(hitMetrics.EqualityIndexLookups + hitMetrics.CompositeIndexLookups + hitMetrics.RangeIndexLookups).IsGreaterThan(0);

        var missMetrics = new QueryMetrics();
        _ = default(MetricUsersLookupMiss).Execute(ctx, options: new QueryExecutionOptions { Metrics = missMetrics }).ToList();
        await Assert.That(missMetrics.IndexLookups).IsGreaterThan(0);
        await Assert.That(missMetrics.IndexMisses).IsGreaterThan(0);
    }

    [Test]
    public async Task QueryMetrics_UnificationAndConstraintCounters_AreIncremented()
    {
        EngineContext ctx = BuildContext();

        var joinMetrics = new QueryMetrics();
        _ = default(MetricJoin).Execute(ctx, options: new QueryExecutionOptions { Metrics = joinMetrics }).ToList();
        await Assert.That(joinMetrics.UnificationAttempts).IsGreaterThan(0);
        await Assert.That(joinMetrics.UnificationSuccesses).IsGreaterThan(0);
        await Assert.That(joinMetrics.UnificationFailures).IsGreaterThan(0);

        var constraintMetrics = new QueryMetrics();
        _ = default(MetricConstraintFilter).Execute(ctx, options: new QueryExecutionOptions { Metrics = constraintMetrics }).ToList();
        await Assert.That(constraintMetrics.ConstraintEvaluations).IsGreaterThan(0);
        await Assert.That(constraintMetrics.ConstraintFailures).IsGreaterThan(0);
    }

    [Test]
    public async Task QueryMetrics_PredicateAndTableCounters_AreIncremented()
    {
        EngineContext ctx = BuildContext();
        var callMetrics = new QueryMetrics();
        _ = default(MetricCaller).Execute(ctx, options: new QueryExecutionOptions { Metrics = callMetrics }).ToList();
        await Assert.That(callMetrics.PredicateCalls).IsGreaterThan(0);
        await Assert.That(callMetrics.PredicateCallResults).IsGreaterThan(0);

        var tableMetrics = new QueryMetrics();
        _ = default(MetricAncestor).Execute(ctx, options: new QueryExecutionOptions { Metrics = tableMetrics }).ToList();
        await Assert.That(tableMetrics.TableProbes).IsGreaterThan(0);
        await Assert.That(tableMetrics.TableHits + tableMetrics.TableMisses).IsEqualTo(tableMetrics.TableProbes);
        await Assert.That(tableMetrics.TableInserts).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task QueryMetrics_Omitted_DoesNotChangeResultOrderOrCardinality()
    {
        EngineContext ctx = BuildContext();
        string[] withoutMetrics = default(MetricUsersScan).Execute(ctx).Select(result => result.login).ToArray();
        string[] withMetrics = default(MetricUsersScan).Execute(ctx, options: new QueryExecutionOptions { Metrics = new QueryMetrics() })
            .Select(result => result.login)
            .ToArray();

        await Assert.That(withMetrics.Length).IsEqualTo(withoutMetrics.Length);
        for (int i = 0; i < withoutMetrics.Length; i++)
            await Assert.That(withMetrics[i]).IsEqualTo(withoutMetrics[i]);
    }

    [Test]
    public async Task QueryMetricsSnapshot_IsDeterministic_ForFixedData()
    {
        EngineContext ctx = BuildContext();

        var first = new QueryMetrics();
        _ = default(MetricAncestor).Execute(ctx, options: new QueryExecutionOptions { Metrics = first }).ToList();

        var second = new QueryMetrics();
        _ = default(MetricAncestor).Execute(ctx, options: new QueryExecutionOptions { Metrics = second }).ToList();

        await Assert.That(second.Snapshot()).IsEqualTo(first.Snapshot());
        await Assert.That(second.MagicSourceProbes).IsEqualTo(0);
        await Assert.That(second.MagicSourceHits).IsEqualTo(0);
        await Assert.That(second.MagicSourceMisses).IsEqualTo(0);
    }

    [Test]
    public async Task QueryMetricsDerived_UsesNullForDivisionByZero()
    {
        QueryMetricsSnapshot snapshot = new(
            FactRowsScanned: 0,
            IndexLookups: 0,
            IndexHits: 0,
            IndexMisses: 0,
            EqualityIndexLookups: 0,
            CompositeIndexLookups: 0,
            RangeIndexLookups: 0,
            IndexRowsReturned: 0,
            UnificationAttempts: 0,
            UnificationSuccesses: 0,
            UnificationFailures: 0,
            ConstraintEvaluations: 0,
            ConstraintFailures: 0,
            ResidualConstraintEvaluations: 0,
            ResidualConstraintFailures: 0,
            PredicateCalls: 0,
            PredicateCallResults: 0,
            Backtracks: 0,
            ResultsEmitted: 0,
            TableProbes: 0,
            TableHits: 0,
            TableMisses: 0,
            TableInserts: 0,
            MagicSourceProbes: 0,
            MagicSourceHits: 0,
            MagicSourceMisses: 0);

        QueryMetricsDerived derived = QueryMetricsDerived.FromSnapshot(snapshot);
        await Assert.That(derived.IndexHitRate).IsNull();
        await Assert.That(derived.ScanToResultRatio).IsNull();
    }

    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.MetricUsers = new FactTable<MetricUser>(new[]
        {
            new MetricUser("u-001", true),
            new MetricUser("u-002", false),
            new MetricUser("u-003", true),
        });
        ctx.MetricEdges = new FactTable<MetricEdge>(new[]
        {
            new MetricEdge("A", "B"),
            new MetricEdge("B", "C"),
            new MetricEdge("C", "D"),
        });
        return ctx;
    }
}
