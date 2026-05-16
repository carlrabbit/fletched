# Goal

Introduce controlled recursive execution improvements through tabling, memoization, recursive query planning, and explicitly specified async recursive predicate semantics.

# Status (2026-05-16)

- Overall: **complete**
- Phase 1 — Docs and specs: **complete**
- Phase 2 — Tabled predicate declaration: **complete**
- Phase 3 — Table key and answer table runtime: **complete**
- Phase 4 — Tabled recursive execution: **complete**
- Phase 5 — Recursive planning: **complete**
- Phase 6 — Async recursive semantics: **complete**
- Phase 7 — Validation and benchmarks: **complete**

# Scope

- Variant tabling for selected predicates
- Memoization table key definition
- Table lifecycle and scope
- Duplicate result suppression for tabled calls
- Recursive query planning for tabled predicates
- Async recursive predicate semantics
- Interaction with negation restrictions
- Documentation/spec synchronization
- Tests and benchmarks for tabled recursion

# Constraints

- Use variant tabling first.
- Do not implement subsumptive tabling in this milestone.
- Do not implement magic-set rewriting in this milestone.
- Preserve non-tabled predicate behavior.
- Preserve negation correctness rules.
- Preserve deterministic source generation.
- Preserve existing predicate invocation ABI unless explicitly extended by spec.
- Async recursive semantics must be documented before implementation.

# Deliverables

- `docs/specs/Tabling.md`
- `docs/specs/RecursiveMemoization.md`
- `docs/specs/RecursiveQueryPlanning.md`
- `docs/specs/AsyncRecursivePredicates.md`
- updates to `docs/specs/RecursivePredicates.md`
- updates to `docs/specs/PredicateInvocation.md`
- updates to `docs/specs/Not.md`
- updates to `docs/specs/Diagnostics.md`
- tabled predicate declaration marker and validation diagnostics
- tests covering tabled declaration validation

# Acceptance Criteria

- [x] Tabling spec exists.
- [x] Recursive memoization spec exists.
- [x] Recursive planning spec exists.
- [x] Async recursive predicate spec exists.
- [x] Tabled predicates are explicitly selectable.
- [x] Variant table keys are deterministic.
- [x] Query-scoped table store exists.
- [x] Duplicate answers are suppressed.
- [x] Positive direct recursion supports tabling.
- [x] Positive mutual recursion supports tabling or is explicitly constrained.
- [x] Recursive negation remains rejected.
- [x] Async recursive behavior is specified and tested.
- [x] Performance comparison against Milestone 6 exists.
