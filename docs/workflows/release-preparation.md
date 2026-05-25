# Goal

Run release-readiness quality gates for a candidate version without performing tag creation, package publication, or GitHub release creation.

# Constraints

- Require an explicit candidate version provided by manual workflow input.
- Use `./eng/release-check.sh <version>` as the canonical quality-gate command.
- Keep workflow implementation focused on validation execution and artifact handling.
- Do not publish packages or call `./eng/publish.sh`.

# Non-Goals

- Tag creation.
- nuget.org publication.
- GitHub release creation.

# Relevant Other Workflows

- `release.md`
- `nuget-pack-and-publish.md`
- `build-and-test.md`

# Inputs

- Repository source at the checked-out revision
- Candidate package version from `workflow_dispatch` input
- `.NET 10` SDK container environment

# Outputs

- Release-check validation result for the candidate version
- Packed NuGet artifacts generated during release-check execution

# Trigger Conditions

- Manual `workflow_dispatch` with required version input

# Failure Conditions

- Missing or invalid version input
- Any failure from `./eng/release-check.sh <version>`, including quality-gate, hygiene, API, docs, packing, or package-smoke failures
- Artifact upload failure when package outputs are expected

# Synchronization Rules

- Update this document before changing `.github/workflows/release-preparation.yml`.
- Keep documented command usage synchronized with workflow YAML and `eng/release-check.sh`.
- Keep release-preparation guidance synchronized with `docs/tbps/release-preparation.md`.

# Authority

This document is authoritative for:
- automated release-preparation workflow intent
- release quality-gate command usage for pre-publish validation

This document is not authoritative for:
- GitHub Actions YAML syntax
- release policy rationale

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `docs/engineering/command-contract.md`
- `docs/workflows/release.md`
- `docs/tbps/release-preparation.md`
- `eng/release-check.sh`

## Must Be Updated Together

When this workflow changes, review and update:
- `.github/workflows/release-preparation.yml`
- `eng/release-check.sh`
- `docs/tbps/release-preparation.md`
