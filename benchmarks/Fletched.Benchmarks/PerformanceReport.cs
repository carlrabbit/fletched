using System;
using System.Collections.Immutable;
using Fletched.Core.Runtime;

namespace Fletched.Benchmarks;

public sealed record GeneratorPerformanceResult(
    string Scenario,
    TimeSpan TotalTime,
    TimeSpan SyntaxDiscoveryTime,
    TimeSpan SemanticBindingTime,
    TimeSpan DslAnalysisTime,
    TimeSpan LoweringTime,
    TimeSpan PlanningTime,
    TimeSpan RecursivePlanningTime,
    TimeSpan OptimizationTime,
    TimeSpan EmissionTime,
    int GeneratedFileCount,
    int GeneratedLineCount,
    long GeneratedByteCount,
    int GeneratedMemberCount,
    int DiagnosticCount);

public sealed record QueryPerformanceResult(
    string Scenario,
    int FactCount,
    int ResultCount,
    QueryMetricsSnapshot Metrics,
    long GeneratedSourceBytes,
    int GeneratedSourceLines);

public sealed record PerformanceReport(
    string CommitSha,
    string RuntimeVersion,
    string Configuration,
    ImmutableArray<GeneratorPerformanceResult> GeneratorResults,
    ImmutableArray<QueryPerformanceResult> QueryResults);

