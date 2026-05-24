# Purpose

Prepare package and release changes without drifting from the repository's documented packaging workflow.

# Preconditions

- Package scope and target versioning approach are known.
- The packaging workflow and package metadata have been reviewed.

# Required Reading

- `docs/workflows/nuget-pack-and-publish.md`
- `docs/engineering/packaging.md`
- `.github/workflows/nuget-pack-and-publish.yml`

# Execution Steps

1. Confirm the package set and version source match the documented workflow intent.
2. Update package metadata and workflow documentation before changing publish implementation.
3. Validate restore, release build, and the relevant automated tests.
4. Confirm artifact naming, tag rules, and publish gating remain synchronized.
5. Record any long-term release direction change in `docs/decisions/` if it changes repository policy.

# Validation

- Check that pack steps cover the documented package set.
- Check that tag and manual triggers match the documented version rules.
- Check that publish gating still depends on the documented secret configuration.

# Common Failures

- Version rules changed only in YAML
- Package metadata drift between projects
- Publish behavior changed without updating workflow intent

# Synchronization Requirements

- Keep this TBP synchronized with `docs/workflows/nuget-pack-and-publish.md`.
- Update `docs/decisions/` when release policy changes materially.
- Update issue templates when release work needs additional routing prompts.

# Related Documents

- `docs/tbps/workflow-changes.md`
- `docs/workflows/nuget-pack-and-publish.md`
