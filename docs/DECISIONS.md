# Decisions

## Purpose

Decision records preserve durable engineering decisions and their rationale.

Decisions are authoritative for why a structural or architectural choice was made.
They are not authoritative for current architecture structure or behavioral specifications.

## Available Decision Records

| Decision | Purpose |
| --- | --- |
| [`decisions/0001-compiled-typed-relational-engine.md`](decisions/0001-compiled-typed-relational-engine.md) | Compiled, typed relational engine as the core approach |

# Authority

This document is authoritative for:
- the decision record index for `docs/decisions/`
- routing readers to durable rationale documents

This document is not authoritative for:
- current architecture structure (see `docs/ARCHITECTURE.md`)
- behavioral specifications (see `docs/SPECS.md`)
- milestone planning (see `docs/MILESTONES.md`)

# Document Contract

## Related Documents

- `docs/ARCHITECTURE.md`
- `docs/architecture/system-overview.md`
- `docs/SPECS.md`

## Must Be Updated Together

When the decision record set changes, review and update:
- `docs/ARCHITECTURE.md`
- related architecture documents
- related specifications when decision-linked behavior changes
