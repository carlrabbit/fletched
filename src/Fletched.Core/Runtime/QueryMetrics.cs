namespace Fletched.Core.Runtime;

/// <summary>
/// Mutable query-scoped counters. Not thread-safe.
/// </summary>
public sealed class QueryMetrics
{
    public long FactRowsScanned;
    public long IndexLookups;
    public long IndexHits;
    public long IndexMisses;

    public long UnificationAttempts;
    public long UnificationSuccesses;
    public long UnificationFailures;

    public long ConstraintEvaluations;
    public long ConstraintFailures;

    public long PredicateCalls;
    public long PredicateCallResults;

    public long Backtracks;
    public long ResultsEmitted;

    public long TableProbes;
    public long TableHits;
    public long TableMisses;
    public long TableInserts;

    public long MagicSourceProbes;
    public long MagicSourceHits;
    public long MagicSourceMisses;

    public QueryMetricsSnapshot Snapshot() =>
        new(
            FactRowsScanned,
            IndexLookups,
            IndexHits,
            IndexMisses,
            UnificationAttempts,
            UnificationSuccesses,
            UnificationFailures,
            ConstraintEvaluations,
            ConstraintFailures,
            PredicateCalls,
            PredicateCallResults,
            Backtracks,
            ResultsEmitted,
            TableProbes,
            TableHits,
            TableMisses,
            TableInserts,
            MagicSourceProbes,
            MagicSourceHits,
            MagicSourceMisses);
}

