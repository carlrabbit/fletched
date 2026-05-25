namespace Fletched.Core.Runtime;

public sealed record QueryMetricsDerived(
    double? IndexHitRate,
    double? UnificationFailureRate,
    double? ConstraintFailureRate,
    double? TableHitRate,
    double? MagicSourceHitRate,
    double? ScanToResultRatio)
{
    public static QueryMetricsDerived FromSnapshot(QueryMetricsSnapshot snapshot) =>
        new(
            Divide(snapshot.IndexHits, snapshot.IndexLookups),
            Divide(snapshot.UnificationFailures, snapshot.UnificationAttempts),
            Divide(snapshot.ConstraintFailures, snapshot.ConstraintEvaluations),
            Divide(snapshot.TableHits, snapshot.TableProbes),
            Divide(snapshot.MagicSourceHits, snapshot.MagicSourceProbes),
            Divide(snapshot.FactRowsScanned, snapshot.ResultsEmitted));

    private static double? Divide(long numerator, long denominator) =>
        denominator == 0 ? null : (double)numerator / denominator;
}

