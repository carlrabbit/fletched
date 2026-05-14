# Add or Revise a Task Best Practice

# Goal

Create or revise a TBP without duplicating existing repository guidance.

# Constraints

- TBPs must remain methodology-oriented.
- TBPs must not define feature semantics.
- TBPs must not define one-off tasks.
- TBPs must use canonical terminology.

# Non-Goals

- Creating specs
- Creating architecture decisions
- Creating implementation plans

# Required Reading

- `docs/TERMINOLOGY.md`
- `docs/TBPS.md`
- existing `docs/tbps/`

# Process

1. Identify whether the new guidance is reusable.
2. Check whether an existing TBP should be extended instead.
3. Define the TBP goal.
4. Define constraints and non-goals.
5. Define required reading.
6. Define validation expectations.
7. Add the TBP to `docs/TBPS.md`.
8. Update terminology if new terms are introduced.

# Validation

- TBP is reusable.
- TBP does not duplicate an existing TBP.
- TBP is linked from `docs/TBPS.md`.
- New terminology is added to `docs/TERMINOLOGY.md`.

# Authority

This document is authoritative for:
- adding TBPs
- revising TBPs
- TBP scope validation

# Document Contract

## Related Documents

- `docs/TBPS.md`
- `docs/TERMINOLOGY.md`

## Must Be Updated Together

When TBP creation rules change, review and update:
- `docs/TBPS.md`
- `.github/ISSUE_TEMPLATE/add-tbp.yml`
