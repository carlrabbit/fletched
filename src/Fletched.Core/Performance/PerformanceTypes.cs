using System;

namespace Fletched.Core.Performance;

/// <summary>Classifies a benchmark by its subject area.</summary>
public enum BenchmarkCategory
{
    /// <summary>Benchmarks that measure the source-generator pipeline (IR, plan, codegen).</summary>
    Generator,

    /// <summary>Benchmarks that measure query execution throughput.</summary>
    Execution,

    /// <summary>Benchmarks for primitive engine operations (unification, trail, etc.).</summary>
    Primitive
}

/// <summary>Input descriptor for a generator benchmark.</summary>
public record GeneratorBenchmarkInput(string SourceCode);

/// <summary>Input descriptor for an execution benchmark.</summary>
public record ExecutionBenchmarkInput<TContext>(TContext Context, Delegate Query);

/// <summary>
/// Metrics derived from a compiled execution plan, used for cost analysis and optimisation
/// decisions.
/// </summary>
public record PlanMetrics(int NodeCount, int InstructionCount, int EstimatedCost);

/// <summary>
/// Pinned expected values for a predicate's compiled artefacts.  A test failure here means
/// the generator pipeline has changed in a way that affects code size or complexity.
/// </summary>
public record PerformanceBaseline(
    string PredicateName,
    int IRNodeCount,
    int PlanInstructionCount,
    int GeneratedLOC);
