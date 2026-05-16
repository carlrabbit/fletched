# Tabling Specification

## Authority

This spec defines tabled predicate semantics.

Tabling changes execution strategy but must not change the logical result set of supported positive recursive predicates.

## Terms

- **Tabled Predicate**: Predicate whose calls and results are stored during evaluation.
- **Variant Call**: Predicate call equivalent to another call modulo variable names.
- **Table Key**: Canonical representation of a tabled predicate call.
- **Answer Table**: Stored result set for a tabled call.
- **Consumer**: Invocation waiting for or reading answers from an existing table.
- **Producer**: Invocation responsible for generating answers for a table key.
- **Completion**: State where no additional answers can be produced for a table key.

## Rules

### TAB-001 — Variant Tabling Only

Initial tabling must use variant tabling.

Calls are table-compatible when they have the same predicate identity and equivalent binding pattern modulo variable names.

Subsumptive tabling is unsupported.

### TAB-002 — Explicit Tabled Predicate Selection

A predicate must be explicitly tabled.

Recommended syntax:

```csharp
[Tabled]
[Predicate]
partial record struct Ancestor
{
    ...
}
```

### TAB-003 — Non-Tabled Behavior Preserved

Predicates without tabled configuration must preserve baseline recursive invocation behavior.

### TAB-004 — Table Key Construction

A table key must include:

- predicate identity
- arity
- parameter types where needed
- bound argument values
- free argument positions

Local variable names must not affect the table key.

### TAB-005 — Answer Storage

Answers must be stored as terminal-variable result tuples for the tabled call.

Answers must be unique per table key.

Duplicate answer production must not produce duplicate yielded results.

### TAB-006 — Table Scope

Initial table scope is query execution scope.

Tables must not persist across unrelated query executions unless explicitly enabled by a future spec.

### TAB-007 — Positive Recursion Only

Tabling supports positive recursive predicates.

Recursive negation remains unsupported.

### TAB-008 — Completion

A table is complete when all producer branches for the table key are exhausted.

Consumers may read completed answers without re-entering producer execution.

### TAB-009 — Observable Ordering

Untabled predicates preserve source-order depth-first behavior.

Tabled predicates may produce answers in table-production order.

If ordering differs from untabled depth-first behavior, the difference must be documented and tested.

## Magic-Set Interaction

Magic-set rewriting may restrict the producer search space for tabled predicates.

Table keys remain defined by tabled predicate call identity and binding state.

Magic predicates must not be included in answer tuples, and magic planning must not create duplicate table answers.

## Required Tests

- tabled direct recursion returns expected answers
- tabled mutual recursion returns expected answers
- duplicate recursive paths do not duplicate answers
- table key ignores variable names
- bound/free argument patterns produce distinct table keys
- non-tabled recursion remains unchanged
- table scope is per query execution

## Must Be Updated Together

- `RecursiveMemoization.md`
- `RecursiveQueryPlanning.md`
- `PredicateInvocation.md`
- `Diagnostics.md`
