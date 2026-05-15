# Bug Investigation

# Goal

Investigate and fix defects while preserving authoritative behavior and documenting any resulting clarifications.

# Constraints

- Start from observed incorrect behavior.
- Check the governing spec, architecture, workflow, or milestone context before changing code.
- Fix the defect without introducing unrelated rewrites.
- Add or update regression coverage for the defect when feasible.

# Non-Goals

- Broad refactoring unrelated to the defect
- Redefining behavior without updating the authoritative document owner
- Leaving reproduction or validation ambiguous

# Required Reading

- governing spec or authoritative document for the defect area
- related tests
- `docs/TERMINOLOGY.md`
- relevant workflow documents or TBPs

# Process

1. Capture the incorrect behavior and expected behavior.
2. Identify the authoritative document owner for the behavior.
3. Reproduce the defect.
4. Implement the minimal fix.
5. Add or update regression coverage when feasible.
6. Re-run targeted validation and review related docs for drift.

# Validation

- The defect is reproducible before the fix or otherwise evidenced.
- The changed behavior matches the authoritative document.
- Regression coverage exists when feasible.
- Targeted validation passes.

# Authority

This document is authoritative for:
- bug investigation process
- defect-fix synchronization expectations

# Document Contract

## Related Documents

- `docs/TBPS.md`
- `.github/ISSUE_TEMPLATE/bug.yml`

## Must Be Updated Together

When bug-investigation rules change, review and update:
- `docs/TBPS.md`
- `.github/ISSUE_TEMPLATE/bug.yml`
