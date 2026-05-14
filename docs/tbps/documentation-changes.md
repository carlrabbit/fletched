# Purpose

Keep authoritative repository documentation aligned with implementation, workflow intent, and contributor guidance.

# Preconditions

- The affected implementation or operational change is understood.
- The authoritative document set for the change area has been identified.

# Required Reading

- `README.md`
- `AGENTS.md`
- `docs/TERMINOLOGY.md`
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`

# Execution Steps

1. Identify the authoritative document that owns the change.
2. Update terminology before reusing new project terms in multiple documents.
3. Update architecture, workflow, or TBP documents before or together with implementation changes.
4. Reduce duplication by converting outdated documents into routing documents when necessary.
5. Verify that root documents still act as navigation rather than duplicated specification.

# Validation

- Check that the changed topic has one authoritative document owner.
- Check that related links point to the current authoritative docs.
- Check that workflow or process changes are reflected in the matching workflow or TBP documents.

# Common Failures

- New guidance added only to `README.md`
- Workflow intent documented only in YAML
- New terms used without a canonical definition
- Historical notes treated as current authority

# Synchronization Requirements

- Update `docs/TERMINOLOGY.md` for canonical vocabulary changes.
- Update `docs/workflows/` for workflow intent changes.
- Update `docs/tbps/` for new recurring execution patterns.
- Update `docs/decisions/` for long-term directional changes.

# Related Documents

- `docs/tbps/workflow-changes.md`
- `docs/workflows/`
- `docs/decisions/`
