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
| [`Architecture.md`](Architecture.md) | Architecture specification |
| [`Backtracking.md`](Backtracking.md) | Backtracking behavior specification |
| [`BuiltinPrecicate_AllDistinct.md`](BuiltinPrecicate_AllDistinct.md) | Built-in `allDistinct` behavior specification |
| [`CodeGeneration.md`](CodeGeneration.md) | Code generation specification |
| [`Diagnostics.md`](Diagnostics.md) | Diagnostics behavior specification |
| [`DSL.md`](DSL.md) | DSL behavior specification |
| [`EngineContext.md`](EngineContext.md) | Engine context specification |
| [`ExecutionPlan.md`](ExecutionPlan.md) | Execution plan specification |
| [`FactStorage.md`](FactStorage.md) | Fact storage specification |
| [`IndexedSources.md`](IndexedSources.md) | Indexed source specification |
| [`IR.md`](IR.md) | IR specification |
| [`Lists.md`](Lists.md) | List behavior specification |
| [`LoweringRules.md`](LoweringRules.md) | Lowering rules specification |
| [`ModulesAndScopes.md`](ModulesAndScopes.md) | Module and scope specification |
| [`Not.md`](Not.md) | Negation behavior specification |
| [`Optimization.md`](Optimization.md) | Optimization specification |
| [`Overview.md`](Overview.md) | System overview specification |
| [`Performance.md`](Performance.md) | Performance specification |
| [`PredicateInvocation.md`](PredicateInvocation.md) | Predicate invocation specification |
| [`ResultProjection.md`](ResultProjection.md) | Result projection specification |
| [`SemanticModel.md`](SemanticModel.md) | Semantic model specification |
| [`StateModel.md`](StateModel.md) | State model specification |
| [`Unification.md`](Unification.md) | Unification specification |
| [`example-spec.md`](example-spec.md) | Example structure for future specs |
| [`variable-scope-and-non-terminal-variables.md`](variable-scope-and-non-terminal-variables.md) | Variable scope, terminal variables, non-terminal variables, and `With<T>` source/fresh behavior |

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
- `docs/tbps/create-spec.md`

## Must Be Updated Together

When specification authoring rules or the spec index change, review and update:
- `docs/tbps/create-spec.md`
- related issue templates under `.github/ISSUE_TEMPLATE/`
