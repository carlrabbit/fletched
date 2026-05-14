# Purpose

Change GitHub workflow behavior without separating workflow intent from workflow implementation.

# Preconditions

- The affected workflow and its repository impact are identified.
- The corresponding workflow document in `docs/workflows/` exists or will be added first.

# Required Reading

- `docs/WORKFLOWS.md`
- `docs/workflows/build-and-test.md`
- `docs/workflows/performance-testing.md`
- `docs/workflows/nuget-pack-and-publish.md`
- `AGENTS.md`

# Execution Steps

1. Update or create the workflow intent document under `docs/workflows/`.
2. Change the corresponding `.github/workflows/*.yml` implementation.
3. Update related TBPs if the workflow introduces a repeated operational pattern.
4. Re-run the relevant repository validation commands.
5. Confirm artifact, trigger, and failure descriptions still match the YAML implementation.

# Validation

- Check that documented triggers match workflow triggers.
- Check that documented inputs and outputs match workflow steps and artifacts.
- Check that documented failure conditions still reflect the implementation.

# Common Failures

- YAML trigger changes without workflow documentation updates
- New artifacts added without output documentation
- Publish or secret-gating logic changed without release guidance updates

# Synchronization Requirements

- Keep `docs/workflows/` and `.github/workflows/` synchronized.
- Update `docs/tbps/release-preparation.md` for release workflow changes.
- Update `docs/TERMINOLOGY.md` when workflows introduce new canonical terms.

# Related Documents

- `docs/tbps/documentation-changes.md`
- `docs/tbps/release-preparation.md`
- `.github/workflows/`
