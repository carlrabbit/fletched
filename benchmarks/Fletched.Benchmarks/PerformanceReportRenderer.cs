using System.Text;
using System.Text.Json;

namespace Fletched.Benchmarks;

public static class PerformanceReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    public static string RenderJson(PerformanceReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);

    public static string RenderMarkdown(PerformanceReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Performance report");
        sb.AppendLine();
        sb.AppendLine($"- Commit: `{report.CommitSha}`");
        sb.AppendLine($"- Runtime: `{report.RuntimeVersion}`");
        sb.AppendLine($"- Configuration: `{report.Configuration}`");
        sb.AppendLine();

        sb.AppendLine("## Generator");
        sb.AppendLine();
        sb.AppendLine("| Scenario | Total (ms) | Files | Lines | Bytes | Diagnostics |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (GeneratorPerformanceResult result in report.GeneratorResults.OrderBy(r => r.Scenario, StringComparer.Ordinal))
            sb.AppendLine($"| {result.Scenario} | {result.TotalTime.TotalMilliseconds:F3} | {result.GeneratedFileCount} | {result.GeneratedLineCount} | {result.GeneratedByteCount} | {result.DiagnosticCount} |");
        sb.AppendLine();

        sb.AppendLine("## Query runtime");
        sb.AppendLine();
        sb.AppendLine("| Scenario | Facts | Results | Scanned | Lookups | Hits | Misses |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        foreach (QueryPerformanceResult result in report.QueryResults.OrderBy(r => (r.Scenario, r.FactCount), Comparer<(string, int)>.Default))
            sb.AppendLine($"| {result.Scenario} | {result.FactCount} | {result.ResultCount} | {result.Metrics.FactRowsScanned} | {result.Metrics.IndexLookups} | {result.Metrics.IndexHits} | {result.Metrics.IndexMisses} |");

        return sb.ToString().TrimEnd();
    }
}

