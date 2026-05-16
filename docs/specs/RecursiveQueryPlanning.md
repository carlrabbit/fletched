# Recursive Query Planning Specification

## Authority

This spec defines planning behavior for recursive predicates, table boundaries, adornment analysis, and magic-set planning.

It does not define a global query optimizer.

## Rules

### RQP-001 — Planner Must Preserve Logical Results

Recursive planning must not add or remove logical answers.

### RQP-002 — Tabled Calls Are Planning Boundaries

A tabled predicate call is a planning boundary.

### RQP-003 — Bound Arguments Must Be Reflected in Table Keys

If a recursive call has bound terminal arguments, the table key must preserve those bindings.

### RQP-004 — Existing Index Selection Remains Valid

Fact-source index selection inside recursive predicate bodies remains valid and may be refined by recursive access-path analysis.

### RQP-005 — Magic-Set Planning Order

After adornment analysis, the planner may invoke magic-set rewriting for supported positive recursive predicates with at least one bound argument.

Magic-set planning must occur before final fact-source access-path selection.

### RQP-006 — Planning Diagnostics

The planner may emit diagnostics for tabled recursive calls, unsupported magic rewriting, all-free recursive calls, recursive negation, ambiguous adornments, and missing recursive indexes.

## Required Tests

- tabled call lowers to table access plan
- non-tabled call lowers to baseline call plan
- bound arguments are included in table key plan values
- recursive call adornment is inspectable
- magic artifacts are inspectable in recursive plans
