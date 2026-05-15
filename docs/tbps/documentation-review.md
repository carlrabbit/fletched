# Documentation Review

# Goal

Review repository documentation for authority clarity, terminology consistency, and synchronization drift.

# Constraints

- Prefer authoritative documents over duplicated summaries.
- Check terminology against `docs/TERMINOLOGY.md`.
- Check workflow/process guidance against workflow documents and TBPs.
- Record gaps without inventing feature behavior.

# Non-Goals

- Implementing product changes
- Replacing specs with architecture or milestone prose
- Broadly rewriting documents without a concrete drift reason

# Required Reading

- `README.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/README.md`
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`

# Process

1. Identify the document set under review.
2. Check authority boundaries and duplicated content.
3. Check terminology consistency.
4. Check document contracts and synchronization targets.
5. Record required updates or missing documents.
6. Validate that authoritative indexes still route correctly.

# Validation

- Authority boundaries are clear.
- Terms align with `docs/TERMINOLOGY.md`.
- Related documents and issue templates are synchronized.
- Root routing documents remain concise.

# Authority

This document is authoritative for:
- documentation review process
- documentation consistency validation

# Document Contract

## Related Documents

- `docs/TBPS.md`
- `docs/TERMINOLOGY.md`
- `.github/ISSUE_TEMPLATE/documentation-review.yml`

## Must Be Updated Together

When documentation review rules change, review and update:
- `docs/TBPS.md`
- `.github/ISSUE_TEMPLATE/documentation-review.yml`
