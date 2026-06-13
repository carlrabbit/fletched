# Milestones

## Purpose

Milestones coordinate strategic implementation phases.

Milestones define sequencing, scoped deliverables, and exit criteria.
They do not define permanent behavioral truth.

## Available Milestones

| Milestone | Scope |
| --- | --- |
| [`milestones/milestone-01-foundation.md`](milestones/milestone-01-foundation.md) | Foundation for runtime/generator boundaries and authoritative docs |
| [`milestones/milestone-02-distribution-and-operations.md`](milestones/milestone-02-distribution-and-operations.md) | Distribution, release, and operational workflow guidance |
| [`milestones/milestone-03-predicate-invocation-and-negation-correctness.md`](milestones/milestone-03-predicate-invocation-and-negation-correctness.md) | Authoritative invocation boundary and negation correctness semantics |
| [`milestones/milestone-04-variable-scope-and-non-terminal-variables.md`](milestones/milestone-04-variable-scope-and-non-terminal-variables.md) | Variable scope, non-terminal variables, and `With<T>` source/fresh behavior |
| [`milestones/milestone-05-recursive-predicate-support.md`](milestones/milestone-05-recursive-predicate-support.md) | Recursive predicate call graph analysis, recursive-negation rejection, and recursive invocation alignment |
| [`milestones/Milestone06_RecursiveSafetyAndBaselines.md`](milestones/Milestone06_RecursiveSafetyAndBaselines.md) | Recursive guard safety, observability, diagnostics, and performance baseline stabilization |
| [`milestones/Milestone07_TablingMemoizationRecursivePlanning.md`](milestones/Milestone07_TablingMemoizationRecursivePlanning.md) | Tabled recursion, memoization, recursive planning, and async recursive semantics |
| [`milestones/Milestone08_MagicSetRewritingFactStorage.md`](milestones/Milestone08_MagicSetRewritingFactStorage.md) | Magic-set rewriting and fact-storage refinements |
| [`milestones/Milestone09_DocumentationSynchronizationAndPredicateCallInlining.md`](milestones/Milestone09_DocumentationSynchronizationAndPredicateCallInlining.md) | Documentation synchronization and predicate-call inlining |
| [`milestones/Milestone10_OptimizationMaturity.md`](milestones/Milestone10_OptimizationMaturity.md) | Optimization pipeline maturity: pass contract, trace model, DeadBindingElimination, LoopSpecialization narrowing |
| Milestone 13: Advanced Fact Indexing | Explicit `[FactIndex]` declarations, typed generated index descriptors, composite equality and range lookups, and planner-aware access-path selection |
| [`milestones/016-food-ontology-sample.md`](milestones/016-food-ontology-sample.md) | Food ontology sample with curated fixture data, recursive classification, profile safety checks, and deterministic console output |
| [`milestones/018-external-guide-system-migration.md`](milestones/018-external-guide-system-migration.md) | External guide-system migration that removes copied-guide authority while preserving localized Fletched project truth |

# Authority

This document is authoritative for:
- the milestone index under `docs/milestones/`
- milestone navigation and sequencing visibility

This document is not authoritative for:
- long-term behavioral rules (see `docs/SPECS.md`)
- architectural decisions (see `docs/DECISIONS.md`)
- one-off issue execution details

# Document Contract

## Related Documents

- `docs/SPECS.md`
- `docs/TBPS.md`
- `docs/tbps/create-milestone.md`
- `docs/tbps/start-milestone.md`
- `docs/tbps/finish-milestone.md`

## Must Be Updated Together

When the milestone index or milestone lifecycle expectations change, review and update:
- `docs/TBPS.md`
- the affected documents in `docs/milestones/`
- related issue templates under `.github/ISSUE_TEMPLATE/`
