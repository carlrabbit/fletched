# Finish Milestone

# Goal

Close a milestone with synchronized documentation, validation, and follow-up tracking.

# Constraints

- Exit criteria must be checked explicitly.
- Follow-up work must be captured as issues or a subsequent milestone.
- Spec and decision drift must be resolved before closure.
- Closure must not redefine feature behavior.

# Non-Goals

- Expanding completed scope
- Rewriting prior milestone intent
- Introducing new undocumented behavior

# Required Reading

- milestone document
- related specs
- related decisions
- linked issues
- `docs/TBPS.md`

# Process

1. Review milestone scope and exit criteria.
2. Confirm completed deliverables.
3. Check related specs and decisions for drift.
4. Capture remaining work as issues or a new milestone.
5. Update milestone status and closure notes.
6. Verify validation evidence is linked or recorded.

# Validation

- Exit criteria are explicitly checked.
- Remaining work is captured outside the closed milestone.
- Related specs and decisions are synchronized.
- Closure notes do not redefine behavioral truth.

# Authority

This document is authoritative for:
- milestone closure process
- milestone completion validation

# Document Contract

## Related Documents

- `docs/tbps/create-milestone.md`
- `docs/tbps/start-milestone.md`
- `docs/milestones/README.md`

## Must Be Updated Together

When milestone lifecycle rules change, review and update:
- `docs/tbps/create-milestone.md`
- `docs/tbps/start-milestone.md`
- `.github/ISSUE_TEMPLATE/finish-milestone.yml`
