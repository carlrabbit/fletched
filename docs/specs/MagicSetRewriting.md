# Magic-Set Rewriting Specification

## Authority

This spec defines supported magic-set rewriting behavior.

Magic-set rewriting is an optimization and must preserve the logical result set for supported positive recursive predicates.

## Terms

- **Magic Predicate**: Generated predicate that restricts recursive evaluation to relevant bound argument values.
- **Adorned Predicate**: Predicate specialized by a binding pattern.
- **Seed Fact**: Initial magic fact derived from a bound query or bound call.
- **Magic Rule**: Generated rule that propagates relevant bindings through recursive calls.
- **Modified Rule**: Original recursive rule guarded by a magic predicate.

## Rules

### MSR-001 — Positive Recursive Predicates Only

Magic-set rewriting applies only to supported positive recursive predicates.

### MSR-002 — Bound Query Requirement

Magic-set rewriting requires at least one bound argument in the recursive query or call.

If all arguments are free, no magic rewrite is applied.

### MSR-003 — Result Preservation

Rewritten execution must produce the same logical result set as unrevised execution for supported predicates.

### MSR-004 — Magic Predicate Generation

For each adorned recursive predicate, generate a deterministic magic predicate name and bound-argument projection.

Example:

```text
Ancestor^bf(parent, child)
Magic_Ancestor_bf(parent)
```

### MSR-005 — Seed Generation

A bound query or call generates a magic seed fact for the matching adorned predicate.

### MSR-006 — Rule Modification

Recursive rules for an adorned predicate are conceptually guarded by the corresponding magic predicate.

### MSR-007 — Magic Rule Propagation

Recursive calls produce propagation rules that move relevant bound values from an earlier successful goal into the next recursive call.

### MSR-008 — Deterministic Rewrite

Given the same predicate definitions and adornment, generated magic artifacts must be deterministic.

### MSR-009 — Inspectability

The plan or diagnostics must allow inspection of adornments, generated magic predicates, seed facts, modified rules, and propagation rules.

### MSR-010 — Interaction With Tabling

Magic-set rewriting may restrict the producer search space for tabled predicates, but table keys remain governed by `Tabling.md`.

### MSR-011 — Unsupported Cases

The planner must conservatively skip or reject magic rewriting for unsupported recursive negation, ambiguous predicate identity, unsupported binding patterns, or all-free recursive calls.

## Required Tests

- bound recursive call produces a magic predicate
- rewritten and unrevised results match
- all-free recursive call does not rewrite
- recursive negation does not rewrite
- inspectable plan includes magic artifacts
- tabled + magic planning remains correct
