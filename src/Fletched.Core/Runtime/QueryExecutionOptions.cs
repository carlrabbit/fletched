namespace Fletched.Core.Runtime;

/// <summary>
/// Query-scoped execution options for generated predicate entry points.
/// </summary>
public sealed record QueryExecutionOptions
{
    /// <summary>
    /// Optional query-scoped metrics collector.
    /// </summary>
    public QueryMetrics? Metrics { get; init; }
}
