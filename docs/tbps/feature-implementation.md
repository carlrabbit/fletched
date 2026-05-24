# Feature Implementation

# Goal

Implement feature work without separating code changes from the authoritative specifications, terminology, and validation that define the feature.

# Constraints

- Feature behavior must be specified before or together with implementation.
- Implementation must follow canonical terminology.
- Related architecture or decision documents must be updated when the feature changes durable structure or rationale.
- Validation must cover observable behavior.

# Non-Goals

- One-off task planning detached from specs
- Architecture rewrites unrelated to the feature
- Unvalidated behavior changes

# Required Reading

- related specs
- related architecture documents
- related decisions
- `docs/TERMINOLOGY.md`
- relevant workflow documents or TBPs

# Process

1. Confirm the governing spec and terminology.
2. Identify required architecture or decision updates.
3. Make the smallest implementation change that satisfies the spec.
4. Add or update tests for observable behavior.
5. Update docs that changed because of the implementation.
6. Validate build, tests, and relevant workflows.

# Validation

- Behavior matches the governing spec.
- Tests cover the changed behavior.
- Related authoritative documents are synchronized.
- Validation commands pass.

# Authority

This document is authoritative for:
- feature implementation process
- feature-change synchronization expectations

# Document Contract

## Related Documents

- `docs/SPECS.md`
- `docs/TERMINOLOGY.md`
- `docs/TBPS.md`

## Must Be Updated Together

When feature-implementation rules change, review and update:
- `docs/TBPS.md`
- related issue templates that request implementation work
