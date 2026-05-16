# Adornment Analysis Specification

## Authority

This spec defines conservative bound/free argument analysis used by recursive query planning, magic-set rewriting, and recursive access-path selection.

## Terms

- **Adornment**: Binding pattern assigned to a predicate call.
- **Bound Argument**: Argument known to be bound at call entry.
- **Free Argument**: Argument not proven bound at call entry.
- **Binding Pattern**: Ordered sequence of `b` and `f` markers for predicate arguments.

Example:

```text
Ancestor("alice", child) => Ancestor^bf
```

## Rules

### AA-001 — Binding Pattern Construction

An adornment is constructed from predicate argument state at call entry.

### AA-002 — Conservative Analysis

If binding state cannot be proven, the argument must be classified as free.

### AA-003 — Type Preservation

Adornment does not change predicate arity or argument types.

### AA-004 — Local Variable Propagation

Bindings created by unification or an earlier successful predicate call may affect later calls in the same conjunction.

Adornment analysis follows source/lowered execution order unless a later planning phase explicitly reorders goals.

### AA-005 — Negation Boundary

Bindings inside `Not(...)` must not propagate outward.

### AA-006 — Tabled Predicate Boundary

Adornment may inform table access planning, but it does not redefine table-key semantics beyond `Tabling.md`.

## Required Tests

- bound/free classification for constants
- bound/free classification for terminal variables
- local unification creates later bound argument
- successful earlier call can ground a later recursive argument
- unbound local variable remains free
- negation does not export bindings
- adorned recursive call detected correctly
