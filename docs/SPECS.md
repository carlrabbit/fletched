# Specifications

## Purpose

Specifications define behavioral truth for the Fletched runtime and source generator.

Specs are authoritative for:
- behavior
- invariants
- contracts
- state transitions
- failure semantics
- validation expectations

Specs are not milestone plans.
Specs are not implementation plans.
Specs are not architecture overviews.

## Spec Rules

- Specs must use canonical terminology.
- Specs must define invariants explicitly.
- Specs must avoid implementation details unless the implementation detail is itself part of the contract.
- Specs must link related architecture and decisions.
- Specs should exist before implementation whenever practical.

## Available Specs

| Spec | Purpose |
| --- | --- |
| [`specs/AdornmentAnalysis.md`](specs/AdornmentAnalysis.md) | Bound/free adornment analysis specification |
| [`specs/AsyncRecursivePredicates.md`](specs/AsyncRecursivePredicates.md) | Async recursive predicate semantics specification |
| [`specs/Architecture.md`](specs/Architecture.md) | Architecture specification |
| [`specs/Backtracking.md`](specs/Backtracking.md) | Backtracking behavior specification |
| [`specs/BuiltinPrecicate_AllDistinct.md`](specs/BuiltinPrecicate_AllDistinct.md) | Built-in `allDistinct` behavior specification |
| [`specs/CodeGeneration.md`](specs/CodeGeneration.md) | Code generation specification |
| [`specs/Diagnostics.md`](specs/Diagnostics.md) | Diagnostics behavior specification |
| [`specs/DSL.md`](specs/DSL.md) | DSL behavior specification |
| [`specs/EngineContext.md`](specs/EngineContext.md) | Engine context specification |
| [`specs/ExecutionPlan.md`](specs/ExecutionPlan.md) | Execution plan specification |
| [`specs/FactSourcesAndIndexes.md`](specs/FactSourcesAndIndexes.md) | Fact source and index selection specification |
| [`specs/FactStorage.md`](specs/FactStorage.md) | Fact storage specification |
| [`specs/FactStorageRefinement.md`](specs/FactStorageRefinement.md) | Recursive fact-storage refinement specification |
| [`specs/IndexedSources.md`](specs/IndexedSources.md) | Indexed source specification |
| [`specs/IR.md`](specs/IR.md) | IR specification |
| [`specs/Lists.md`](specs/Lists.md) | List behavior specification |
| [`specs/LoweringRules.md`](specs/LoweringRules.md) | Lowering rules specification |
| [`specs/ModulesAndScopes.md`](specs/ModulesAndScopes.md) | Module and scope specification |
| [`specs/Negation.md`](specs/Negation.md) | Compatibility alias for negation semantics (`Not.md`) |
| [`specs/Not.md`](specs/Not.md) | Negation behavior specification |
| [`specs/MagicSetRewriting.md`](specs/MagicSetRewriting.md) | Magic-set rewriting behavior specification |
| [`specs/Optimization.md`](specs/Optimization.md) | Optimization specification |
| [`specs/Observability.md`](specs/Observability.md) | Runtime observability and metrics specification |
| [`specs/Overview.md`](specs/Overview.md) | System overview specification |
| [`specs/Performance.md`](specs/Performance.md) | Performance specification |
| [`specs/PredicateInvocation.md`](specs/PredicateInvocation.md) | Predicate invocation specification |
| [`specs/RecursiveMemoization.md`](specs/RecursiveMemoization.md) | Runtime memoization behavior for tabled recursion |
| [`specs/RecursiveAccessPaths.md`](specs/RecursiveAccessPaths.md) | Recursive access-path selection specification |
| [`specs/RecursivePerformanceBaselines.md`](specs/RecursivePerformanceBaselines.md) | Recursive benchmark and baseline specification |
| [`specs/RecursivePredicates.md`](specs/RecursivePredicates.md) | Recursive predicate behavior and constraints specification |
| [`specs/RecursiveQueryPlanning.md`](specs/RecursiveQueryPlanning.md) | Planning behavior for recursive predicates with table boundaries |
| [`specs/RecursiveSafety.md`](specs/RecursiveSafety.md) | Recursive operational safety and guard behavior specification |
| [`specs/ResultProjection.md`](specs/ResultProjection.md) | Result projection specification |
| [`specs/SemanticModel.md`](specs/SemanticModel.md) | Semantic model specification |
| [`specs/StateModel.md`](specs/StateModel.md) | State model specification |
| [`specs/Tabling.md`](specs/Tabling.md) | Tabled predicate semantics and table lifecycle specification |
| [`specs/Unification.md`](specs/Unification.md) | Unification specification |
| [`specs/example-spec.md`](specs/example-spec.md) | Example structure for future specs |
| [`specs/variable-scope-and-non-terminal-variables.md`](specs/variable-scope-and-non-terminal-variables.md) | Variable scope, terminal variables, non-terminal variables, and `With<T>` source/fresh behavior |

# Authority

This document is authoritative for:
- specification authoring rules
- specification indexing under `docs/specs/`
- specification/documentation synchronization expectations

This document is not authoritative for:
- milestone sequencing (see `docs/MILESTONES.md`)
- workflow behavior (see `docs/WORKFLOWS.md`)
- implementation-only details that are not behavioral contracts

# Document Contract

## Related Documents

- `docs/TERMINOLOGY.md`
- `docs/tbps/create-spec.md`

## Must Be Updated Together

When specification authoring rules or the spec index change, review and update:
- `docs/tbps/create-spec.md`
- related issue templates under `.github/ISSUE_TEMPLATE/`
