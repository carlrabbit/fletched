# Recursive Safety Specification

## Authority

This spec defines operational safety behavior for recursive predicate execution.

It extends `RecursivePredicates.md` and does not redefine recursive logical semantics.

## Terms

- **Recursive Depth**: Number of active predicate invocation frames in a recursive call chain.
- **Recursion Guard**: Operational limit that prevents unbounded recursive execution from exhausting runtime resources.
- **Guard Violation**: Runtime or diagnostic event emitted when recursive depth exceeds the configured limit.
- **Productive Recursion**: Recursive execution that yields at least one solution before reaching a guard or exhausting search.
- **Non-Productive Recursion**: Recursive execution that does not yield before continuing recursive expansion.

## Rules

### RS-001 — Recursion Guard Is Operational

The recursion guard is not part of logical semantics.

It must not be used by the planner to infer logical failure.

### RS-002 — Default Guard Behavior

The runtime default recursion-depth policy is:

- no guard by default (`MaxRecursionDepth = null`)

### RS-003 — Guard Configuration

The recursion guard is configurable through runtime options:

```csharp
MaxRecursionDepth: int?
```

where:

- `null` means no depth limit
- positive integer means maximum allowed recursive invocation depth
- zero or negative values are invalid configuration

### RS-004 — Guard Violation Behavior

A guard violation is operational failure and must not silently fail as logical predicate failure.

Current behavior:

- throw `RecursiveDepthExceededException`
- report observer callbacks for recursive depth and guard violation

### RS-005 — Guard Location

The guard is checked before entering a recursive predicate invocation frame.

### RS-006 — Guard and Backtracking

If a guard violation occurs during a branch, caller-visible state remains consistent:

- invocation frame depth is unwound
- no partial bindings from failed recursive entry leak into caller state

### RS-007 — Guard and Negation

Guard violations inside `Not(...)` are not converted into negation success.

A guard violation is operational failure.

### RS-008 — Diagnostics

Runtime exposes guard diagnostics through `RecursiveDepthExceededException` and observer callbacks, including:

- predicate where guard was exceeded
- active recursion depth
- configured maximum depth
- recursive call chain when available
- whether violation occurred inside negation

## Required Tests

- depth guard disabled allows productive recursion
- finite depth guard allows shallow recursion
- finite depth guard rejects excessive recursion
- guard violation does not become logical failure
- guard violation inside `Not(...)` does not become success
- guard violation preserves caller-visible state consistency

## Must Be Updated Together

- `RecursivePredicates.md`
- `PredicateInvocation.md`
- `Diagnostics.md`
- runtime options documentation
- benchmark documentation
