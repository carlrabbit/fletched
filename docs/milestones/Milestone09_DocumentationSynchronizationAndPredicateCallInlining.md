# Milestone 09: Documentation Synchronization and Predicate-Call Inlining

# Goal

Synchronize authoritative documentation with the current implementation and implement predicate-call inlining as a first-class roadmap optimization.

# Status (2026-05-24)

- Overall: **complete**
- Phase 1 — Documentation synchronization: **complete**
- Phase 2 — Predicate-call inlining implementation: **complete**
- Phase 3 — Tests and validation: **complete**

# Scope

## Documentation synchronization

- Rewrite `docs/specs/Overview.md` as a current authoritative system overview.
- Remove source-document inventory language from `docs/specs/Overview.md`.
- Update `docs/specs/Optimization.md` to clearly distinguish implemented behavior from roadmap behavior.
- Clarify dead-code elimination scope (instructions and unreachable blocks; unused slot-binding removal is not implemented).
- Clarify loop-specialization status (no-op analysis pass; loop removal is not implemented).
- Promote predicate-call inlining from optional mention to explicit roadmap/milestone scope.
- Update `docs/MILESTONES.md`.

## Predicate-call inlining implementation

Implement a conservative `PredicateCallInlining` plan optimization pass.

Supported inline cases (initial):
- Target predicate is non-recursive.
- Target predicate has a single execution-plan block.
- Target predicate body contains only deterministic instructions (no loops, no nested calls, no negation).
- Call argument count is within the configured threshold.
- Callee is not tabled.

Explicit fallback cases (never inlined):
- `CallInstr.IsTabledCall == true`.
- Callee plan is not available in the pass context.
- Callee is recursive or mutually recursive (metadata indicates recursive calls).
- Argument count exceeds `DefaultMaxArgumentCount` (8).
- Callee has multiple blocks (disjunctive structure).
- Callee terminator is not `SucceedTerm`.
- Callee contains loop, call, or negation instructions.
- `CallInstr` inside `NotInstr` subgoal (not traversed).

# Constraints

- Specs remain authoritative behavioral truth, not implementation notes.
- No behavior change may break existing public query APIs.
- No inlining may remove required backtracking points.
- No inlining may weaken negation grounding or recursive-negation diagnostics.
- No inlining may change tabled predicate semantics.
- Inlining must be deterministic for the same semantic input.

# Deliverables

- `docs/milestones/Milestone09_DocumentationSynchronizationAndPredicateCallInlining.md`
- Rewritten `docs/specs/Overview.md`
- Updated `docs/specs/Optimization.md`
- Updated `docs/MILESTONES.md`
- `PredicateCallInlining` optimization pass in `src/Fletched.Roslyn/Pipeline/OptimizationPipeline.cs`
- Tests in `tests/Fletched.Performance.Tests/OptimizationPipelineTests.cs`

# Required Specs

- `docs/specs/Overview.md`
- `docs/specs/Optimization.md`
- `docs/specs/ExecutionPlan.md`
- `docs/specs/PredicateInvocation.md`
- `docs/specs/Backtracking.md`
- `docs/specs/RecursivePredicates.md`
- `docs/specs/Tabling.md`
- `docs/specs/Not.md`

# Risks

- Inlining eligibility checks must be exhaustive; any missed check could silently miscompile a predicate.
- Slot remapping must be exact; off-by-one slot mapping produces incorrect bindings.
- Callee key derivation must be stable and match how programs are registered.

# Acceptance Criteria

## Documentation

- [x] `docs/specs/Overview.md` describes the current architecture and no longer reads as a historical document inventory.
- [x] `docs/specs/Optimization.md` accurately distinguishes implemented optimizations from roadmap optimizations.
- [x] Dead-code elimination documentation matches actual behavior.
- [x] Loop-specialization documentation matches actual behavior.
- [x] Predicate-call inlining is documented as milestone scope, not merely optional.
- [x] `docs/MILESTONES.md` is updated.

## Predicate-call inlining

- [x] `PredicateCallInlining` optimization pass exists.
- [x] Non-recursive eligible predicate calls can be inlined.
- [x] Recursive calls are not inlined.
- [x] Tabled calls are not inlined.
- [x] Calls inside negation are not traversed/inlined.
- [x] Inlined calls preserve caller result projection via slot remapping.
- [x] Unsupported inlining candidates fall back to normal `CallInstr` execution.
- [x] Tests cover successful inlining.
- [x] Tests cover fallback behavior (tabled, recursive, argument count, multi-block).
- [x] `./eng/check.sh` passes.
