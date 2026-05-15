# Specifications

## Purpose

Specifications define behavioral truth.

Specs are authoritative for:
- behavior
- invariants
- contracts
- state transitions
- failure semantics
- validation expectations

Specs are not milestone plans.
Specs are not implementation plans.
Specs are not architecture overviews.

## Spec Rules

- Specs must use canonical terminology.
- Specs must define invariants explicitly.
- Specs must avoid implementation details unless the implementation detail is itself part of the contract.
- Specs must link related architecture and decisions.
- Specs should exist before implementation whenever practical.

## Available Specs

| Spec | Purpose |
| --- | --- |
| [`specs/example-spec.md`](specs/example-spec.md) | Example structure for future specs |

## Relationship to `specs/`

The root `specs/` directory continues to hold detailed design notes that support architecture and implementation work.
Authoritative behavioral specifications belong in `docs/specs/`.

# Authority

This document is authoritative for:
- specification authoring rules
- specification indexing under `docs/specs/`
- specification/documentation synchronization expectations

This document is not authoritative for:
- milestone sequencing
- workflow behavior
- implementation-only design notes in `specs/`

# Document Contract

## Related Documents

- `docs/TERMINOLOGY.md`
- `docs/specs/README.md`
- `docs/tbps/create-spec.md`

## Must Be Updated Together

When specification authoring rules or the spec index change, review and update:
- `docs/specs/README.md`
- `docs/tbps/create-spec.md`
- related issue templates under `.github/ISSUE_TEMPLATE/`
