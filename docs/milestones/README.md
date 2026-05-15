# Milestones

## Purpose

Milestones coordinate strategic implementation phases.

Milestones define sequencing, scoped deliverables, and exit criteria.
They do not define permanent behavioral truth.

## Available Milestones

| Milestone | Scope |
| --- | --- |
| [`milestone-01-foundation.md`](milestone-01-foundation.md) | Foundation for runtime/generator boundaries and authoritative docs |
| [`milestone-02-distribution-and-operations.md`](milestone-02-distribution-and-operations.md) | Distribution, release, and operational workflow guidance |
| [`milestone-03-predicate-invocation-and-negation-correctness.md`](milestone-03-predicate-invocation-and-negation-correctness.md) | Authoritative invocation boundary and negation correctness semantics |
| [`milestone-04-variable-scope-and-non-terminal-variables.md`](milestone-04-variable-scope-and-non-terminal-variables.md) | Variable scope, non-terminal variables, and `With<T>` source/fresh behavior |

# Authority

This document is authoritative for:
- the milestone index under `docs/milestones/`
- milestone navigation and sequencing visibility

This document is not authoritative for:
- long-term behavioral rules
- architectural decisions
- one-off issue execution details

# Document Contract

## Related Documents

- `docs/specs/README.md`
- `docs/TBPS.md`
- `docs/tbps/create-milestone.md`
- `docs/tbps/start-milestone.md`
- `docs/tbps/finish-milestone.md`

## Must Be Updated Together

When the milestone index or milestone lifecycle expectations change, review and update:
- `docs/TBPS.md`
- the affected documents in `docs/milestones/`
- related issue templates under `.github/ISSUE_TEMPLATE/`
