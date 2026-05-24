# Milestone 10: Optimization Maturity

# Goal

Make the optimization pipeline a stable, inspectable, deterministic, semantics-preserving compiler stage.

# Status (2026-05-24)

- Overall: **complete**
- Phase 1 — Formal pass contract and trace model: **complete**
- Phase 2 — DeadBindingElimination: **complete**
- Phase 3 — LoopSpecialization narrowing and trace reporting: **complete**
- Phase 4 — Pipeline order update: **complete**
- Phase 5 — Tests and validation: **complete**

# Scope

## Formal optimization pass contract

Replaced `IPlanOptimization.Apply(PlanProgram)` with:

```csharp
public interface IPlanOptimization
{
    string Name { get; }
    PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context);
}
```

Added `PlanOptimizationResult`, `PlanOptimizationChange`, `PlanChangeKind`, `PlanOptimizationPassTrace`, `PlanOptimizationTrace`, `PlanOptimizationContext`, and `OptimizationOptions` types. Backward-compatible `Apply(PlanProgram)` extension method retained for existing call sites.

## Optimization trace

`OptimizationPipeline.RunWithTrace()` produces a `PlanOptimizationTrace` with per-pass entries when `EmitOptimizationTrace` is enabled. Each entry records pass name, deterministic SHA-256 input/output plan hashes, and applied changes.

## DeadBindingElimination

New pass separate from `DeadCodeElimination`. Removes pure `AssignInstr` writes whose slot is never read by any subsequent instruction. Emits `RemovedDeadBinding` trace changes.

## LoopSpecialization

Pass emits `SkippedCandidate` trace entries for recognized loop candidates when tracing is enabled. Structural loop rewriting (singleton/empty source) remains out of scope pending plan IR extensions.

## Pipeline order

Updated to match the spec:

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

## PredicateCallInlining option control

`PredicateCallInlining` respects `OptimizationOptions.EnablePredicateCallInlining`. Emits `InlinedPredicateCall` and `SkippedCandidate` trace changes.

# Constraints

- Every optimization must be deterministic and semantics-preserving.
- No optimization may change result cardinality, binding behavior, failure behavior, or backtracking behavior.
- Unsafe removal candidates (Constraint, Call, Not, LoopBind, failing Unify) are never removed.

# Deliverables

- `docs/milestones/Milestone10_OptimizationMaturity.md`
- Updated `docs/specs/Optimization.md`
- Updated `docs/MILESTONES.md`
- New types in `src/Fletched.Roslyn/Pipeline/PlanTypes.cs`
- Updated `src/Fletched.Roslyn/Pipeline/OptimizationPipeline.cs`
- Tests in `tests/Fletched.Performance.Tests/OptimizationPipelineTests.cs`

# Acceptance Criteria

## Pass contract and trace

- [x] `IPlanOptimization` interface has `Name` and `Optimize(PlanProgram, PlanOptimizationContext)`.
- [x] `PlanOptimizationResult`, `PlanOptimizationChange`, `PlanChangeKind` types exist.
- [x] `PlanOptimizationTrace` and `PlanOptimizationPassTrace` types exist.
- [x] `OptimizationOptions` and `PlanOptimizationContext` types exist.
- [x] `OptimizationPipeline.RunWithTrace()` produces per-pass trace when `EmitOptimizationTrace` is enabled.
- [x] Plan hashes are deterministic.

## DeadBindingElimination

- [x] Pass exists and is separate from `DeadCodeElimination`.
- [x] Removes unused pure `AssignInstr` writes.
- [x] Preserves writes whose slot is read by subsequent instructions.
- [x] Does not remove `ConstraintInstr`, `CallInstr`, `NotInstr`, `LoopBindInstr`, or failing `UnifyInstr`.
- [x] Emits `RemovedDeadBinding` trace changes.
- [x] Controlled by `OptimizationOptions.EnableDeadBindingElimination`.

## LoopSpecialization

- [x] Emits `SkippedCandidate` trace changes when tracing is enabled.
- [x] Controlled by `OptimizationOptions.EnableLoopSpecialization`.

## Pipeline

- [x] Pass order matches spec.
- [x] `./eng/check.sh` passes.
