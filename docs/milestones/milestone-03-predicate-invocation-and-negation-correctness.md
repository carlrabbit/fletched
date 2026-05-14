# Goal

Define authoritative semantics and implementation boundaries for predicate invocation and negation-as-failure while preserving the compiled typed execution architecture.

# Status (2026-05-14)

- Overall: **In progress**
- Phase 1 — Documentation authority and terminology synchronization: **complete**
- Phase 2 — Invocation ABI alignment (frame contract + deterministic copy-in/copy-out): **complete**
- Phase 3 — Negation correctness alignment: **complete** (`FLG0001`–`FLG0004` enforcement implemented and validated)
- Phase 4 — Integration hardening and validation expansion: **in progress**

Remaining Milestone 3 focus:
- Expand end-to-end runtime coverage depth around invocation+negation compositions and additional backtracking edge cases.

# Scope

- Invocation lifecycle and invocation boundary semantics
- Caller/callee ownership and copy-in/copy-out behavior
- Predicate success and predicate exhaustion definitions
- Cross-predicate backtracking behavior
- Negation grounding requirements and isolated evaluation semantics
- Diagnostics for invalid negation usage and invocation binding conflicts

# Constraints

- Preserve compiled execution architecture and iterator/state-machine execution model
- Preserve deterministic lowering behavior and typed state model
- Avoid runtime interpretation or reflection-based semantics
- Keep workflow and document synchronization rules explicit

# Deliverables

- Synchronized updates to:
  - `specs/PredicateInvocation.md.txt`
  - `specs/Backtracking.md.txt`
  - `specs/LoweringRules.md.txt`
  - `specs/Diagnostics.md.txt`
  - `specs/DSL.md.txt`
  - `docs/TERMINOLOGY.md`
- Milestone index update under `docs/milestones/`
- Implementation-ready follow-up task ordering and validation strategy

# Non-Goals

- Recursive predicate optimization or tabling
- Async semantic redesign
- Parallel predicate execution redesign
- Broad planner or fact storage redesign

# Dependencies

- Existing execution plan architecture and typed state generation
- Existing predicate call and negation IR/plan constructs
- Existing backtracking and trail unwind semantics

# Implementation Phases

1. Documentation authority and terminology synchronization
2. Invocation ABI alignment (frame contract + deterministic copy-in/copy-out)
3. Negation correctness alignment (grounding diagnostics + isolation guarantees)
4. Integration hardening and validation expansion

# Validation Strategy

- Documentation validation:
  - terminology consistency
  - no duplicated semantic authority
  - synchronized invocation/negation semantics across affected docs
- Runtime/generator validation:
  - invocation resumability and exhaustion behavior
  - cross-boundary backtracking correctness
  - negation isolation and no outward binding leakage
