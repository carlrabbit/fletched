using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

/// <summary>
/// Collects diagnostics during source generation analysis.
/// When any error is reported, code generation is suppressed for the affected predicate.
/// </summary>
public sealed class DiagnosticReporter
{
    private readonly List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void Report(DiagnosticDescriptor descriptor, Location? location, params object[] args)
    {
        _diagnostics.Add(Diagnostic.Create(descriptor, location, args));
    }

    public void Error(DiagnosticDescriptor descriptor, Location? location, params object[] args) =>
        Report(descriptor, location, args);
}
