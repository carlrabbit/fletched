# Goal

Define and implement variable scope and non-terminal variable semantics for predicate bodies while preserving the compiled typed execution architecture.

# Status (2026-05-15)

- Overall: **Planned**
- Phase 1 — Documentation authority and spec synchronization: **complete**
- Phase 2 — Semantic analysis alignment: **not started**
- Phase 3 — Lowering and state model alignment: **not started**
- Phase 4 — Integration hardening and validation expansion: **not started**

# Scope

- `With<T>` source-variable and fresh-variable behavior
- Terminal and non-terminal variable classification
- Local variable visibility and scope boundaries
- Fresh variable slot allocation and binding behavior
- Result materialization exclusion for non-terminal variables
- Groundness analysis integration for scoped variables
- Diagnostics for ambiguous `With<T>` resolution and invalid variable escape

# Constraints

- Preserve compiled execution architecture and iterator/state-machine execution model
- Preserve deterministic lowering behavior and typed state model
- Preserve existing predicate invocation and negation correctness semantics
- Avoid introducing recursion semantics in this milestone
- Avoid introducing planner or fact storage redesigns
- Keep behavioral truth in `docs/specs/`

# Deliverables

- New authoritative spec:
  - `docs/specs/variable-scope-and-non-terminal-variables.md`
- Synchronized updates to:
  - `docs/SPECS.md`
  - `docs/specs/README.md`
  - `docs/TERMINOLOGY.md`
  - `docs/milestones/README.md`
- Implementation-ready follow-up task ordering and validation strategy
- Tests for fresh variables, source variables, scoping, result materialization, and grounding integration

# Non-Goals

- Recursive predicate semantics
- Tabling or memoization
- Query planner redesign
- Fact storage redesign
- Async semantic changes
- Explicit projection semantics
- Runtime reflection behavior changes

# Dependencies

- Existing `LogicExpr<T>` DSL behavior
- Existing `With<T...>` semantic analysis
- Existing terminal variable result materialization
- Existing typed state generation
- Existing predicate invocation and negation correctness milestone
- Existing grounding diagnostics for negation

# Implementation Phases

1. Documentation authority and spec synchronization
2. Semantic analysis alignment
3. Lowering and state model alignment
4. Diagnostics and validation expansion
5. Integration hardening

# Phase 1 — Documentation Authority and Spec Synchronization

- Create `docs/specs/variable-scope-and-non-terminal-variables.md`.
- Add the spec to `docs/SPECS.md`.
- Add the spec to `docs/specs/README.md`.
- Add milestone entry to `docs/milestones/README.md`.
- Add canonical terminology for terminal variable, non-terminal variable, source variable, fresh variable, and local scope.

# Phase 2 — Semantic Analysis Alignment

- Resolve each `With<T>` variable as either source variable or fresh variable.
- Classify `TerminalVar<T>` parameters as terminal variables.
- Classify `With<T...>` variables as non-terminal local variables.
- Reject ambiguous `With<T>` resolution.
- Reject scoped variable escape.
- Preserve nested scope resolution.

# Phase 3 — Lowering and State Model Alignment

- Lower source variables to fact-source enumeration.
- Lower fresh variables to local slots without fact-source enumeration.
- Ensure fresh variable slots start unbound.
- Ensure non-terminal variables are excluded from result materialization.
- Ensure terminal variable materialization remains unchanged.

# Phase 4 — Diagnostics and Validation Expansion

- Add diagnostics for unsupported or ambiguous `With<T>` resolution.
- Add diagnostics for invalid scoped variable escape.
- Extend grounding analysis to account for fresh and source variables.
- Validate ungrounded negation involving scoped variables.

# Phase 5 — Integration Hardening

- Add end-to-end tests for source and fresh variables in the same predicate.
- Add nested `With<T>` tests.
- Add disjunction and backtracking tests involving fresh variables.
- Add predicate invocation tests using fresh variables as intermediate values.

# Validation Strategy

- Documentation validation:
  - terminology consistency
  - no duplicated semantic authority
  - synchronized spec and milestone indexes
- Generator validation:
  - deterministic source/fresh classification
  - correct scope graph construction
  - correct local slot allocation
- Runtime validation:
  - fresh variables bind through unification
  - source variables enumerate facts
  - non-terminal variables do not appear in results
  - backtracking restores scoped variable bindings

# Risks

- `With<T>` overload behavior may become ambiguous if fact types and primitive types share DSL surface assumptions.
- Fresh variables may accidentally be treated as fact sources if type classification is not centralized.
- Result projection may drift if non-terminal variables are not explicitly excluded.
- Groundness analysis may become unsound if it ignores scope and execution order.

# Open Questions

- Should a future explicit spelling distinguish `With.Fresh<T>` from `With.Source<T>`?
- Should ambiguous fact/non-fact resolution be impossible by construction or enforced diagnostically?
- Should fresh variables support proxy member access when `T` is not a fact type?

# Related Documents

- `docs/specs/variable-scope-and-non-terminal-variables.md`
- `docs/SPECS.md`
- `docs/specs/README.md`
- `docs/TERMINOLOGY.md`
- `specs/DSL.md.txt`
- `specs/SemanticModel.md.txt`
- `specs/IR.md.txt`
- `specs/LoweringRules.md.txt`
- `specs/StateModel.md.txt`
- `specs/Diagnostics.md.txt`

# Authority

This milestone is authoritative for:
- staged implementation scope for variable scope and non-terminal variables
- delivery order and validation expectations for this milestone

This milestone is not authoritative for:
- permanent behavioral truth
- architecture-wide terminology
- recursion semantics

# Must Be Updated Together

When milestone scope or delivery status changes, review and update:
- `docs/milestones/README.md`
- `docs/specs/variable-scope-and-non-terminal-variables.md`
- `docs/SPECS.md`
- `docs/specs/README.md`
- `docs/TERMINOLOGY.md`
