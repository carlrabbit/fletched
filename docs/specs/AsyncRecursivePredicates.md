# Async Recursive Predicates Specification

## Authority

This spec defines async recursive predicate semantics.

It applies only to generated async execution paths.

## Rules

### ARP-001 — Async Results Match Sync Results

For supported predicates, async recursive execution must produce the same logical result set as sync execution.

### ARP-002 — Async Enumeration Boundary

Async recursion is exposed through `IAsyncEnumerable<TResult>`.

Each recursive async predicate call must preserve invocation state across awaits.

### ARP-003 — Cancellation

Async recursive execution must accept cancellation where the repository's async execution model supports it.

Cancellation is operational termination, not logical failure.

### ARP-004 — Table Scope in Async Execution

Async table stores are scoped to the async query execution.

They must remain valid across awaits.

### ARP-005 — Concurrent Consumers

Initial implementation does not require parallel table production.

If multiple async consumers observe the same incomplete table, behavior must be deterministic and documented.

Recommended initial behavior:

```text
single producer, sequential consumers
```

### ARP-006 — Exceptions

Operational exceptions inside async recursive execution must propagate through async enumeration.

They must not be converted into logical failure.

## Required Tests

- async recursion returns same results as sync recursion
- async tabled recursion returns same set as sync tabled recursion
- cancellation terminates async recursion operationally
- exceptions propagate through async enumeration
- table scope survives awaits
