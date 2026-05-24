namespace Fletched.Core.Runtime;

public sealed record QueryMetricsSnapshot(
    long FactRowsScanned,
    long IndexLookups,
    long IndexHits,
    long IndexMisses,
    long UnificationAttempts,
    long UnificationSuccesses,
    long UnificationFailures,
    long ConstraintEvaluations,
    long ConstraintFailures,
    long PredicateCalls,
    long PredicateCallResults,
    long Backtracks,
    long ResultsEmitted,
    long TableProbes,
    long TableHits,
    long TableMisses,
    long TableInserts,
    long MagicSourceProbes,
    long MagicSourceHits,
    long MagicSourceMisses);

