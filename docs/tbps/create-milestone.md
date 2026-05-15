# Create Milestone

# Goal

Define a controlled implementation phase with explicit scope and exit criteria.

# Constraints

- Milestones must reference required specs.
- Milestones must not replace specs.
- Milestones must not define permanent behavior.
- Milestones must identify non-goals.

# Non-Goals

- Implementing the milestone
- Defining behavioral truth
- Recording decision rationale

# Required Reading

- `docs/TERMINOLOGY.md`
- `docs/TBPS.md`
- `docs/SPECS.md`
- related specs
- related decisions

# Process

1. Define the milestone goal.
2. Define scope.
3. Define non-goals.
4. Identify required specs.
5. Identify required decisions.
6. Define deliverables.
7. Define risks.
8. Define exit criteria.
9. Add the milestone document under `docs/milestones/`.
10. Update `docs/milestones/README.md`.
11. Link related issues if they already exist.

# Validation

- Milestone has explicit exit criteria.
- Milestone references required specs.
- Milestone does not duplicate spec behavior.
- Milestone scope is bounded.

# Authority

This document is authoritative for:
- milestone creation process
- milestone structure

# Document Contract

## Related Documents

- `docs/TBPS.md`
- `docs/SPECS.md`
- `docs/milestones/README.md`

## Must Be Updated Together

When milestone creation rules change, review and update:
- `docs/milestones/README.md`
- `.github/ISSUE_TEMPLATE/create-milestone.yml`
