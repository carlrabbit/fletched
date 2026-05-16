# Recursive Query Planning Specification

## Authority

This spec defines planning behavior for recursive predicates before magic-set rewriting exists.

It does not define global query optimization.

## Rules

### RQP-001 — Planner Must Preserve Logical Results

Recursive planning must not add or remove logical answers.

### RQP-002 — Tabled Calls Are Planning Boundaries

A tabled predicate call is a planning boundary.

The planner may choose table access operations but must not inline or reorder across the table boundary unless explicitly allowed by this spec.

### RQP-003 — Bound Arguments Must Be Reflected in Table Keys

If a recursive call has bound terminal arguments, the table key must preserve those bindings.

### RQP-004 — Existing Index Selection Remains Valid

Fact-source index selection inside recursive predicate bodies remains valid.

No recursive-specific index rewrite is introduced in this milestone.

### RQP-005 — No Magic-Set Rewriting

The planner must not synthesize magic predicates or rewrite recursive definitions in this milestone.

### RQP-006 — Planning Diagnostics

The planner may emit informational diagnostics for:

- tabled recursive call
- non-tabled recursive call
- recursive call without usable bound arguments
- potential duplicate recursive paths

These diagnostics must not change behavior.

## Required Tests

- tabled call lowers to table access plan
- non-tabled call lowers to baseline call plan
- bound arguments are included in table key plan values
- fact indexes still apply inside recursive predicate bodies
