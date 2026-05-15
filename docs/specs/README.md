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
| [`Architecture.md.txt`](Architecture.md.txt) | Architecture specification |
| [`Backtracking.md.txt`](Backtracking.md.txt) | Backtracking behavior specification |
| [`BuiltinPrecicate_AllDistinct.md.txt`](BuiltinPrecicate_AllDistinct.md.txt) | Built-in `allDistinct` behavior specification |
| [`CodeGeneration.md.txt`](CodeGeneration.md.txt) | Code generation specification |
| [`Diagnostics.md.txt`](Diagnostics.md.txt) | Diagnostics behavior specification |
| [`DSL.md.txt`](DSL.md.txt) | DSL behavior specification |
| [`EngineContext.md.txt`](EngineContext.md.txt) | Engine context specification |
| [`ExecutionPlan.md.txt`](ExecutionPlan.md.txt) | Execution plan specification |
| [`FactStorage.md.txt`](FactStorage.md.txt) | Fact storage specification |
| [`IndexedSources.md.txt`](IndexedSources.md.txt) | Indexed source specification |
| [`IR.md.txt`](IR.md.txt) | IR specification |
| [`Lists.md.txt`](Lists.md.txt) | List behavior specification |
| [`LoweringRules.md.txt`](LoweringRules.md.txt) | Lowering rules specification |
| [`ModulesAndScopes.md.txt`](ModulesAndScopes.md.txt) | Module and scope specification |
| [`Not.md.txt`](Not.md.txt) | Negation behavior specification |
| [`Optimization.md.txt`](Optimization.md.txt) | Optimization specification |
| [`Overview.md.txt`](Overview.md.txt) | System overview specification |
| [`Performance.md.txt`](Performance.md.txt) | Performance specification |
| [`PredicateInvocation.md.txt`](PredicateInvocation.md.txt) | Predicate invocation specification |
| [`ResultProjection.md.txt`](ResultProjection.md.txt) | Result projection specification |
| [`SemanticModel.md.txt`](SemanticModel.md.txt) | Semantic model specification |
| [`StateModel.md.txt`](StateModel.md.txt) | State model specification |
| [`Unification.md.txt`](Unification.md.txt) | Unification specification |
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
