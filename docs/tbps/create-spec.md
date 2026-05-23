# Create Specification

# Goal

Create a specification that defines behavioral truth before implementation.

# Constraints

- Specs must define invariants.
- Specs must use canonical terminology.
- Specs must reference related architecture and decisions.
- Specs must avoid implementation details unless they are contractual.

# Non-Goals

- Planning a milestone
- Implementing code
- Recording decision rationale

# Required Reading

- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- related architecture documents
- related decisions

# Process

1. Identify the behavior or contract to specify.
2. Define scope and non-goals.
3. List relevant terminology.
4. Define invariants.
5. Define behavioral rules.
6. Define failure semantics.
7. Define validation expectations.
8. Add the spec to `docs/SPECS.md`.
9. Create or update related decisions if rationale is missing.

# Validation

- Spec defines observable behavior.
- Spec includes invariants.
- Spec avoids accidental implementation planning.
- Spec is linked from `docs/SPECS.md`.

# Authority

This document is authoritative for:
- spec creation process
- spec validation expectations

# Document Contract

## Related Documents

- `docs/SPECS.md`
- `docs/TERMINOLOGY.md`

## Must Be Updated Together

When spec authoring rules change, review and update:
- `docs/SPECS.md`
- `.github/ISSUE_TEMPLATE/create-spec.yml`
