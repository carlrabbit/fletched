# Optimization

## Purpose

Defines compile-time transformations applied to the Plan to improve generated execution performance while preserving logical equivalence.

## Contracts

### Pass contract

```csharp
public interface IPlanOptimization
{
    string Name { get; }

    PlanOptimizationResult Optimize(
        PlanProgram program,
        PlanOptimizationContext context);
}
```

### Compatibility contract

Existing call sites may continue to use `Apply(PlanProgram)` via `PlanOptimizationExtensions`, which invokes `Optimize` with default `OptimizationOptions`.

### Trace contract

```csharp
public sealed record PlanOptimizationChange(
    string Pass,
    PlanChangeKind Kind,
    string Target,
    string Reason);

public sealed record PlanOptimizationPassTrace(
    string PassName,
    string InputHash,
    string OutputHash,
    ImmutableArray<PlanOptimizationChange> Changes);

public sealed record PlanOptimizationTrace(
    ImmutableArray<PlanOptimizationPassTrace> Passes);
```

When `OptimizationOptions.EmitOptimizationTrace` is enabled, the pipeline records deterministic per-pass input/output hashes and per-pass change sets.

## Invariants

- All transformations preserve logical equivalence.
- No optimization introduces additional observable bindings.
- No optimization removes required backtracking behavior.
- Slot identities remain stable except for fresh temporary slots introduced by hoisting or inlining.
- Control-flow labels remain resolvable after every pass.
- Predicate-call inlining is conservative and must preserve caller result projection and invocation-boundary semantics.

## Pass behavior

### NormalizeSequence

- Merges straight-line `GotoTerm` chains when the target has a single inbound edge.
- Does not currently emit per-change entries.

### RemoveRedundantUnify

- Removes `Unify(X, X)`.
- Rewrites constant mismatches such as `Unify(Const(1), Const(2))` to `FailTerm`.
- Emits `SimplifiedUnification` changes.

### DependencyAnalysis

- Computes instruction read/write sets for downstream passes.
- Does not change the Plan.

### PredicateCallInlining

- Runs before reordering so downstream passes can optimize newly inlined instructions.
- Inlines only non-tabled, non-recursive, single-block callees with deterministic `SucceedTerm` exits.
- Rejects callees that exceed configured size/growth thresholds or contain unsupported loop, call, or negation instructions.
- Emits `InlinedPredicateCall` for successful inline rewrites.
- Emits `SkippedCandidate` for rejected call sites.

### ReorderConjunction

- Performs dependency-safe instruction reordering within a block.
- Prefers constraints/comparisons first, then unifications, assignments, and loop instructions last.
- Emits `ReorderedConjunction` changes.

### IndexSelection

- Promotes loop key-filter checks as early as dependencies allow.
- Emits `SelectedIndex` changes when a block is reordered to favor loop key filters.

### ConstraintHoisting

- Moves `ConstraintInstr` and `CompInstr` earlier when all dependencies remain satisfied.
- Emits `HoistedConstraint` changes.

### DeadBindingElimination

- Removes pure `AssignInstr` bindings whose written slot is never read by any other instruction in the same block.
- Does not remove loop binds, call effects, or projection-relevant writes.
- Emits `RemovedDeadBinding` changes.

### DeadCodeElimination

- Rewrites provably failing instructions to `FailTerm`.
- Removes trailing instructions after an unconditional fail point.
- Removes unreachable blocks from the control-flow graph.
- Emits `RemovedInstruction` and `RemovedUnreachableBlock` changes.

### LoopSpecialization

- Preserves the current loop structure.
- Records loop-specialization candidates for loop checks and loop binds when optimization tracing is enabled.
- Emits `SpecializedLoop` changes for analysis-visible loop candidates.
- No structural loop rewrite is currently required by the Plan contract.

### TempHoisting

- Reuses repeated `FieldValue` reads inside read-only instruction segments by hoisting them into fresh temporary slots.
- Does not currently emit per-change entries.

## Pipeline order

1. NormalizeSequence
2. RemoveRedundantUnify
3. DependencyAnalysis
4. PredicateCallInlining
5. NormalizeSequence
6. DependencyAnalysis
7. ReorderConjunction
8. IndexSelection
9. ConstraintHoisting
10. DeadBindingElimination
11. DeadCodeElimination
12. LoopSpecialization
13. TempHoisting
14. NormalizeSequence

## Options

```csharp
public sealed record OptimizationOptions
{
    public bool EnablePredicateCallInlining { get; init; } = true;
    public bool EnableDeadBindingElimination { get; init; } = true;
    public bool EnableLoopSpecialization { get; init; } = true;

    public int MaxInlineInstructionCount { get; init; } = 32;
    public int MaxInlineDepth { get; init; } = 2;
    public int MaxGeneratedInstructionGrowthPercent { get; init; } = 150;

    public bool EmitOptimizationTrace { get; init; } = false;
}
```

## Deterministic hashing

- `OptimizationPipeline.RunWithTrace` computes a deterministic Plan hash per pass when tracing is enabled.
- The hash is derived from a normalized rendering of block labels, instructions, values, and terminators.
- Hashes are intended for trace correlation, regression detection, and pass-order verification.

## Related documents

- `docs/specs/ExecutionPlan.md`
- `docs/specs/FactSourcesAndIndexes.md`
- `docs/specs/PredicateInvocation.md`
