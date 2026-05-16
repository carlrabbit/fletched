using System.Diagnostics.Metrics;

namespace Fletched.Core.Performance;

/// <summary>
/// Global counters for the Prolog engine. Fields are initialised once at application startup
/// (see section 5.4 of the performance spec) and then incremented from generated code when
/// the <c>METRICS</c> compilation symbol is defined.
/// </summary>
public static class EngineMetrics
{
    /// <summary>Incremented on every unification attempt.</summary>
    public static Counter<long> UnifyAttempts = null!;

    /// <summary>Incremented on every failed unification.</summary>
    public static Counter<long> UnifyFailures = null!;

    /// <summary>Incremented on every backtrack step.</summary>
    public static Counter<long> BacktrackCount = null!;

    /// <summary>Incremented whenever a new choice point is pushed.</summary>
    public static Counter<long> ChoicePointCount = null!;

    /// <summary>Incremented each time a full fact-table scan is initiated.</summary>
    public static Counter<long> FactScans = null!;

    /// <summary>Incremented on every index-based lookup.</summary>
    public static Counter<long> IndexHits = null!;

    /// <summary>Incremented on every predicate invocation.</summary>
    public static Counter<long> PredicateInvocations = null!;

    /// <summary>Incremented whenever an invocation frame is resumed for another solution.</summary>
    public static Counter<long> PredicateInvocationResumes = null!;

    /// <summary>Incremented whenever an invocation frame is exhausted.</summary>
    public static Counter<long> PredicateInvocationExhaustions = null!;

    /// <summary>Incremented whenever a predicate invocation fails in the caller.</summary>
    public static Counter<long> PredicateInvocationFailures = null!;

    /// <summary>Incremented when a recursive invocation frame is entered.</summary>
    public static Counter<long> RecursiveInvocations = null!;

    /// <summary>Records observed recursive invocation depth values.</summary>
    public static Histogram<long> RecursiveDepth = null!;

    /// <summary>
    /// Convenience initialiser: creates a <see cref="Meter"/> named <paramref name="meterName"/>
    /// and registers all counters. Call once at application startup before running queries.
    /// </summary>
    public static Meter Initialize(string meterName = "FletchedEngine")
    {
        var meter = new Meter(meterName);
        UnifyAttempts = meter.CreateCounter<long>("unify_attempts");
        UnifyFailures = meter.CreateCounter<long>("unify_failures");
        BacktrackCount = meter.CreateCounter<long>("backtrack_count");
        ChoicePointCount = meter.CreateCounter<long>("choice_point_count");
        FactScans = meter.CreateCounter<long>("fact_scans");
        IndexHits = meter.CreateCounter<long>("index_hits");
        PredicateInvocations = meter.CreateCounter<long>("predicate_invocations");
        PredicateInvocationResumes = meter.CreateCounter<long>("predicate_invocation_resumes");
        PredicateInvocationExhaustions = meter.CreateCounter<long>("predicate_invocation_exhaustions");
        PredicateInvocationFailures = meter.CreateCounter<long>("predicate_invocation_failures");
        RecursiveInvocations = meter.CreateCounter<long>("recursive_invocations");
        RecursiveDepth = meter.CreateHistogram<long>("recursive_depth");
        return meter;
    }
}
