# Specifications

## Purpose

Specifications define behavioral truth.

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
| [`specs/Architecture.md.txt`](specs/Architecture.md.txt) | Architecture specification |
| [`specs/Backtracking.md.txt`](specs/Backtracking.md.txt) | Backtracking behavior specification |
| [`specs/BuiltinPrecicate_AllDistinct.md.txt`](specs/BuiltinPrecicate_AllDistinct.md.txt) | Built-in `allDistinct` behavior specification |
| [`specs/CodeGeneration.md.txt`](specs/CodeGeneration.md.txt) | Code generation specification |
| [`specs/Diagnostics.md.txt`](specs/Diagnostics.md.txt) | Diagnostics behavior specification |
| [`specs/DSL.md.txt`](specs/DSL.md.txt) | DSL behavior specification |
| [`specs/EngineContext.md.txt`](specs/EngineContext.md.txt) | Engine context specification |
| [`specs/ExecutionPlan.md.txt`](specs/ExecutionPlan.md.txt) | Execution plan specification |
| [`specs/FactStorage.md.txt`](specs/FactStorage.md.txt) | Fact storage specification |
| [`specs/IndexedSources.md.txt`](specs/IndexedSources.md.txt) | Indexed source specification |
| [`specs/IR.md.txt`](specs/IR.md.txt) | IR specification |
| [`specs/Lists.md.txt`](specs/Lists.md.txt) | List behavior specification |
| [`specs/LoweringRules.md.txt`](specs/LoweringRules.md.txt) | Lowering rules specification |
| [`specs/ModulesAndScopes.md.txt`](specs/ModulesAndScopes.md.txt) | Module and scope specification |
| [`specs/Not.md.txt`](specs/Not.md.txt) | Negation behavior specification |
| [`specs/Optimization.md.txt`](specs/Optimization.md.txt) | Optimization specification |
| [`specs/Overview.md.txt`](specs/Overview.md.txt) | System overview specification |
| [`specs/Performance.md.txt`](specs/Performance.md.txt) | Performance specification |
| [`specs/PredicateInvocation.md.txt`](specs/PredicateInvocation.md.txt) | Predicate invocation specification |
| [`specs/ResultProjection.md.txt`](specs/ResultProjection.md.txt) | Result projection specification |
| [`specs/SemanticModel.md.txt`](specs/SemanticModel.md.txt) | Semantic model specification |
| [`specs/StateModel.md.txt`](specs/StateModel.md.txt) | State model specification |
| [`specs/Unification.md.txt`](specs/Unification.md.txt) | Unification specification |
| [`specs/example-spec.md`](specs/example-spec.md) | Example structure for future specs |
| [`specs/variable-scope-and-non-terminal-variables.md`](specs/variable-scope-and-non-terminal-variables.md) | Variable scope, terminal variables, non-terminal variables, and `With<T>` source/fresh behavior |

# Authority

This document is authoritative for:
- specification authoring rules
- specification indexing under `docs/specs/`
- specification/documentation synchronization expectations

This document is not authoritative for:
- milestone sequencing
- workflow behavior
- implementation-only details that are not behavioral contracts

# Document Contract

## Related Documents

- `docs/TERMINOLOGY.md`
- `docs/specs/README.md`
- `docs/tbps/create-spec.md`

## Must Be Updated Together

When specification authoring rules or the spec index change, review and update:
- `docs/specs/README.md`
- `docs/tbps/create-spec.md`
- related issue templates under `.github/ISSUE_TEMPLATE/`
