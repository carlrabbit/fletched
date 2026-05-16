# Recursive Memoization Specification

## Authority

This spec defines runtime memoization behavior for tabled recursive predicates.

It implements `Tabling.md` storage requirements.

## Rules

### RM-001 — Memoization Scope

Memoization tables are owned by a query execution context.

They must be cleared when query execution ends.

### RM-002 — Table Identity

A memo table is identified by the table key defined in `Tabling.md`.

### RM-003 — Producer Registration

The first invocation for a missing table key becomes the producer.

Subsequent invocations for the same key become consumers.

### RM-004 — Consumer Behavior

Consumers read answers already available for the table key.

If the table is incomplete, consumer behavior must be deterministic and must not recurse infinitely on the same key.

### RM-005 — Answer Deduplication

Answer insertion must check for duplicates using the answer tuple equality semantics defined by the predicate terminal variables.

### RM-006 — State Isolation

Memoized answers must not retain mutable execution state.

Stored answers are value snapshots.

### RM-007 — Error Propagation

Operational errors during table production must propagate to consumers.

They must not be represented as logical failure.

## Required Runtime Structures

Recommended initial structures:

```csharp
sealed class QueryTableStore
{
    Dictionary<TableKey, AnswerTable> Tables;
}

readonly record struct TableKey(...);

sealed class AnswerTable
{
    List<AnswerTuple> Answers;
    HashSet<AnswerTuple> Seen;
    TableStatus Status;
}

enum TableStatus
{
    Producing,
    Complete,
    Faulted
}
```

## Required Tests

- producer/consumer split
- repeated variant call reads table
- answer snapshot does not mutate
- duplicate answers suppressed
- faulted producer propagates error
