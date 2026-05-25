using System;
using System.Collections.Immutable;
using Fletched.Benchmarks;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Performance.Tests;

public class BenchmarkSmokeTests
{
    [Test]
    public async Task GeneratorBenchmarks_RunRepresentativeScenario()
    {
        var bench = new GeneratorBenchmarks { Scenario = "SmallFacts" };
        GeneratorPerformanceResult result = bench.RunGenerator();

        await Assert.That(result.Scenario).IsEqualTo("SmallFacts");
        await Assert.That(result.GeneratedByteCount).IsGreaterThan(0);
        await Assert.That(result.GeneratedLineCount).IsGreaterThan(0);
    }

    [Test]
    public async Task QueryRuntimeBenchmarks_RunRepresentativeScenario()
    {
        var bench = new QueryRuntimeBenchmarks
        {
            FactCount = 100,
            SelectivityPercent = 10
        };

        bench.Setup();
        int scanCount = bench.SimpleFactScan();
        int lookupCount = bench.IndexedFactLookup();

        await Assert.That(scanCount).IsGreaterThan(0);
        await Assert.That(lookupCount).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task PerformanceReportRenderer_ProducesDeterministicJsonAndMarkdown()
    {
        var report = new PerformanceReport(
            CommitSha: "abc123",
            RuntimeVersion: Environment.Version.ToString(),
            Configuration: "Release",
            GeneratorResults: ImmutableArray.Create(new GeneratorPerformanceResult(
                Scenario: "SmallFacts",
                TotalTime: TimeSpan.FromMilliseconds(1),
                SyntaxDiscoveryTime: TimeSpan.FromMilliseconds(0.1),
                SemanticBindingTime: TimeSpan.FromMilliseconds(0.1),
                DslAnalysisTime: TimeSpan.FromMilliseconds(0.1),
                LoweringTime: TimeSpan.FromMilliseconds(0.1),
                PlanningTime: TimeSpan.Zero,
                RecursivePlanningTime: TimeSpan.Zero,
                OptimizationTime: TimeSpan.FromMilliseconds(0.1),
                EmissionTime: TimeSpan.FromMilliseconds(0.1),
                GeneratedFileCount: 1,
                GeneratedLineCount: 10,
                GeneratedByteCount: 100,
                GeneratedMemberCount: 2,
                DiagnosticCount: 0)),
            QueryResults: ImmutableArray.Create(new QueryPerformanceResult(
                Scenario: "SimpleFactScan",
                FactCount: 100,
                ResultCount: 100,
                Metrics: new QueryMetricsSnapshot(
                    FactRowsScanned: 100,
                    IndexLookups: 1,
                    IndexHits: 1,
                    IndexMisses: 0,
                    EqualityIndexLookups: 1,
                    CompositeIndexLookups: 0,
                    RangeIndexLookups: 0,
                    IndexRowsReturned: 1,
                    UnificationAttempts: 10,
                    UnificationSuccesses: 9,
                    UnificationFailures: 1,
                    ConstraintEvaluations: 5,
                    ConstraintFailures: 1,
                    ResidualConstraintEvaluations: 0,
                    ResidualConstraintFailures: 0,
                    PredicateCalls: 2,
                    PredicateCallResults: 2,
                    Backtracks: 3,
                    ResultsEmitted: 100,
                    TableProbes: 0,
                    TableHits: 0,
                    TableMisses: 0,
                    TableInserts: 0,
                    MagicSourceProbes: 0,
                    MagicSourceHits: 0,
                    MagicSourceMisses: 0),
                GeneratedSourceBytes: 1000,
                GeneratedSourceLines: 40)));

        string json1 = PerformanceReportRenderer.RenderJson(report);
        string json2 = PerformanceReportRenderer.RenderJson(report);
        string markdown1 = PerformanceReportRenderer.RenderMarkdown(report);
        string markdown2 = PerformanceReportRenderer.RenderMarkdown(report);

        await Assert.That(json1).IsEqualTo(json2);
        await Assert.That(markdown1).IsEqualTo(markdown2);
        await Assert.That(json1.Contains("\"GeneratorResults\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(markdown1.Contains("SimpleFactScan", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task QueryMetricsDerived_UsesNullForDivisionByZero()
    {
        QueryMetricsDerived derived = QueryMetricsDerived.FromSnapshot(new QueryMetricsSnapshot(
            FactRowsScanned: 1,
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
            MagicSourceMisses: 0));

        await Assert.That(derived.IndexHitRate).IsNull();
        await Assert.That(derived.TableHitRate).IsNull();
        await Assert.That(derived.MagicSourceHitRate).IsNull();
    }
}
