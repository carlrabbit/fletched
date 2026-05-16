Performance Specification

1. Overview

Defines measurement, instrumentation, and validation mechanisms for build-time and runtime performance of the Prolog engine. Establishes mandatory metrics, benchmarks, and regression guards.

---

2. Core Concepts / Data Structures

2.1 Benchmark Categories

enum BenchmarkCategory
{
    Generator,
    Execution,
    Primitive
}

---

2.2 Generator Benchmark Input

record GeneratorBenchmarkInput(
    string SourceCode
);

---

2.3 Execution Benchmark Input

record ExecutionBenchmarkInput<TContext>(
    TContext Context,
    Delegate Query
);

---

2.4 Metrics

static class EngineMetrics
{
    public static Counter<long> UnifyAttempts;
    public static Counter<long> UnifyFailures;

    public static Counter<long> BacktrackCount;
    public static Counter<long> ChoicePointCount;

    public static Counter<long> FactScans;
    public static Counter<long> IndexHits;

    public static Counter<long> PredicateInvocations;
    public static Counter<long> RecursiveInvocations;
    public static Histogram<long> RecursiveDepth;
}

---

2.5 Plan Metrics

record PlanMetrics(
    int NodeCount,
    int InstructionCount,
    int EstimatedCost
);

---

2.6 Golden Baseline

record PerformanceBaseline(
    string PredicateName,
    int IRNodeCount,
    int PlanInstructionCount,
    int GeneratedLOC
);

---

2.7 Execution Observer

interface IExecutionObserver
{
    void OnUnify(int slotId);
    void OnUnifyFailure(int slotId);

    void OnBacktrack();
    void OnChoicePoint();

    void OnFactScan(string factName);
    void OnIndexHit(string factName);

    void OnPredicateInvocation(string predicateName);
    void OnRecursiveInvocation(string predicateName, int depth);
    void OnRecursiveDepthExceeded(string predicateName, int depth, int maxDepth, bool insideNegation);
}

---

3. Rules and Invariants

3.1 Generator Performance

- Generator benchmarks MUST measure:
  - IR construction time
  - Plan lowering time
  - Code generation time
- Generator benchmarks MUST be executed using BenchmarkDotNet.

---

3.2 Golden Baselines

- Each predicate MUST have a baseline entry.
- The following MUST be asserted:
  - IR node count
  - Plan instruction count
  - Generated LOC
- Any deviation MUST fail tests.

---

3.3 Runtime Benchmarks

- Benchmarks MUST include:
  - simple lookup
  - join
  - conjunction chain
  - disjunction
- Each benchmark MUST run with multiple dataset sizes.

---

3.4 Metrics Collection

- Metrics MUST be implemented using System.Diagnostics.Metrics.
- Metrics MUST be incremented in generated code.
- Metrics MUST be conditionally compiled:

#if METRICS
...
#endif

---

3.5 Unification Metrics

- Every unification attempt MUST increment:
  - "UnifyAttempts"
- Every failed unification MUST increment:
  - "UnifyFailures"

---

3.6 Backtracking Metrics

- Every backtrack MUST increment:
  - "BacktrackCount"
- Every choice point creation MUST increment:
  - "ChoicePointCount"

---

3.7 Fact Access Metrics

- Every full scan MUST increment:
  - "FactScans"
- Every indexed lookup MUST increment:
  - "IndexHits"

---

3.8 Predicate Invocation Metrics

- Each predicate call MUST increment:
  - "PredicateInvocations"
- Recursive invocation entry MUST increment:
  - "RecursiveInvocations"
- Recursive invocation depth MUST be recorded:
  - "RecursiveDepth"

---

3.9 Execution Observer

- Observer invocation MUST be optional.
- Observer calls MUST NOT allocate.
- Observer calls MUST NOT affect control flow.

---

3.10 Benchmark Stability

- Benchmarks MUST run without allocation spikes.
- Benchmarks MUST include memory diagnostics.
- Benchmarks MUST be deterministic.

---

3.11 CI Integration

- Performance regression threshold MUST be enforced:

Assert.True(current >= baseline * 0.9);

---

4. Execution / Behavior

4.1 Generated Unification (with metrics)

#if METRICS
EngineMetrics.UnifyAttempts.Add(1);
#endif

if (!state.name_bound)
{
    state.name = value;
    state.name_bound = true;
}
else if (state.name != value)
{
#if METRICS
    EngineMetrics.UnifyFailures.Add(1);
#endif
    goto Fail;
}

---

4.2 Choice Point Creation

#if METRICS
EngineMetrics.ChoicePointCount.Add(1);
#endif

cps.Push(new ChoicePoint { ... });

---

4.3 Backtracking

#if METRICS
EngineMetrics.BacktrackCount.Add(1);
#endif

state.Trail.UnwindTo(ref state, cp.TrailTop);

---

4.4 Fact Scan

#if METRICS
EngineMetrics.FactScans.Add(1);
#endif

foreach (var user in ctx.Users.Data)
{
    ...
}

---

4.5 Indexed Lookup

#if METRICS
EngineMetrics.IndexHits.Add(1);
#endif

if (!ctx.Users.ByLogin.TryGetValue(key, out var matches))
    goto Fail;

---

4.6 Predicate Invocation

#if METRICS
EngineMetrics.PredicateInvocations.Add(1);
#endif

if (!Predicate_Exec(ref state, ctx))
    goto Fail;

---

5. Examples

5.1 Generator Benchmark

[MemoryDiagnoser]
public class GeneratorBench
{
    [Benchmark]
    public void Build_IR_And_Plan()
    {
        var model = BuildSemanticModel(Source);
        var ir = BuildIR(model);
        var plan = Lower(ir);
    }
}

---

5.2 Execution Benchmark

[MemoryDiagnoser]
public class ExecutionBench
{
    [Benchmark]
    public void Join_Query()
    {
        foreach (var _ in Engine.Query())
        {
        }
    }
}

---

5.3 Golden Baseline Assertion

Assert.Equal(12, metrics.IRNodeCount);
Assert.Equal(18, metrics.PlanInstructionCount);
Assert.Equal(85, metrics.GeneratedLOC);

---

5.4 Metrics Initialization

static readonly Meter Meter = new("PrologEngine");

EngineMetrics.UnifyAttempts =
    Meter.CreateCounter<long>("unify_attempts");

EngineMetrics.BacktrackCount =
    Meter.CreateCounter<long>("backtrack_count");

---

5.5 Observer Integration

observer?.OnUnify(slotId);

observer?.OnBacktrack();
