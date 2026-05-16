# Goal

Optimize recursive and bound-input queries through magic-set rewriting and fact-storage refinements.

# Status (2026-05-16)

- Overall: **complete**
- Phase 1 — Docs and specs: **complete**
- Phase 2 — Adornment analysis: **complete**
- Phase 3 — Magic rewrite model: **complete**
- Phase 4 — Planner integration: **complete**
- Phase 5 — Fact storage refinement: **complete**
- Phase 6 — Tabling integration: **complete**
- Phase 7 — Validation and benchmarks: **complete**

# Scope

- Magic-set rewriting metadata for supported positive recursive predicates
- Bound/free adornment analysis
- Deterministic magic predicate, seed, modified-rule, and propagation-rule artifacts
- Recursive access-path inspection
- Fact-storage refinements for generated index accessors and query-scoped magic sources
- Diagnostics for conservative fallback cases
- Documentation/spec synchronization
- Correctness and benchmark validation

# Constraints

- Preserve logical result sets.
- Preserve explicit negation restrictions.
- Preserve tabled predicate semantics from Milestone 7.
- Do not rewrite unsupported recursive negation.
- Do not introduce a general-purpose global optimizer.
- Optimized fact access must not require runtime reflection.
- Magic rewriting artifacts must remain deterministic and inspectable.

# Deliverables

- `docs/milestones/Milestone08_MagicSetRewritingFactStorage.md`
- `docs/specs/AdornmentAnalysis.md`
- `docs/specs/MagicSetRewriting.md`
- `docs/specs/FactStorageRefinement.md`
- `docs/specs/RecursiveAccessPaths.md`
- updates to `docs/specs/Tabling.md`
- updates to `docs/specs/RecursiveQueryPlanning.md`
- `docs/specs/FactSourcesAndIndexes.md`
- updates to `docs/specs/Diagnostics.md`
- adornment analysis and inspectable recursive planning metadata
- generated-accessor fact indexes and query-scoped magic source storage
- correctness tests for recursive adornments, diagnostics, and magic artifacts
- bound recursive benchmark coverage

# Acceptance Criteria

- [x] Adornment analysis spec exists.
- [x] Magic-set rewriting spec exists.
- [x] Recursive access-path spec exists.
- [x] Fact-storage refinement spec exists.
- [x] Bound/free patterns are computed conservatively.
- [x] Supported bound recursive queries can be magic-planned.
- [x] All-free recursive queries are not magic-planned.
- [x] Recursive negation is not magic-planned.
- [x] Rewritten and unrevised result sets remain logically equivalent.
- [x] Magic predicates and seed facts are inspectable.
- [x] Fact storage supports generated index accessors and query-scoped magic sources.
- [x] Tabled + magic planning remains compatible.
- [x] Performance benchmark coverage includes a bound recursive query.
