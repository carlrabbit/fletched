# Goal

Build, pack, and optionally publish Fletched NuGet packages from a documented version source.

# Constraints

- Resolve package version from either `workflow_dispatch` input or a `v*` tag.
- Restore and build before packing.
- Use `./eng/package.sh <version>` for package creation.
- Pack `Fletched.Core` and `Fletched.Roslyn` from repository source into `artifacts/nuget`.
- Publish only on tag-triggered runs and only when `NUGET_API_KEY` is available.
- Use `./eng/publish.sh` for nuget.org publication.

# Non-Goals

- General build-and-test validation beyond what is required to support packaging.
- Arbitrary package selection outside the maintained package set.
- Operational rationale embedded directly in workflow YAML.

# Relevant Other Workflows

- `build-and-test.md`
- `performance-testing.md`

# Inputs

- Repository source at the checked-out revision
- Version input from `workflow_dispatch` or the pushed tag name
- `src/Fletched.Core/Fletched.Core.csproj`
- `src/Fletched.Roslyn/Fletched.Roslyn.csproj`
- Repository secret `NUGET_API_KEY` for publish runs

# Outputs

- Packed `.nupkg` and `.snupkg` artifacts
- Uploaded workflow artifacts for packed packages
- Published nuget.org packages on successful tag-triggered publish runs

# Trigger Conditions

- Manual `workflow_dispatch` with required version input
- `push` for tags matching `v*`

# Failure Conditions

- Invalid or missing version input
- Restore, build, or pack failure
- Artifact upload failure
- Publish failure when a publish run is eligible and `NUGET_API_KEY` is configured

# Synchronization Rules

- Update this document before changing `.github/workflows/nuget-pack-and-publish.yml`.
- Keep documented package scope synchronized with `eng/package.sh`, `eng/publish.sh`, package metadata, and workflow steps.
- Keep release preparation guidance synchronized with `docs/tbps/release-preparation.md`.

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/packaging.md`
- `eng/package.sh`
- `eng/publish.sh`

## Must Be Updated Together

When this workflow changes, review and update:
- `.github/workflows/nuget-pack-and-publish.yml`
- `eng/package.sh`
- `eng/publish.sh`
