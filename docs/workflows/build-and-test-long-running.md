# Goal

Validate repository restore, release build, and automated test execution including all integration tests.

# Constraints

- Use `Fletched.slnx` as the build entry point.
- Build in `Release` configuration.
- Verify formatting with `./eng/format.sh --verify-no-changes`.
- Use `./eng/ci/collect-coverage.sh long-running` for coverage-producing test execution.
- Keep workflow implementation focused on execution and artifact handling.

# Non-Goals

- Performance benchmarking.
- Package publishing.
- Detailed architectural explanation of the runtime or generator.

# Relevant Other Workflows

- `build-and-test.md`
- `performance-testing.md`
- `nuget-pack-and-publish.md`

# Inputs

- Repository source at the checked-out revision
- `.NET 10` SDK container environment
- Test project definitions under `tests/`

# Outputs

- Release build output for repository projects
- Test result artifacts
- Coverage report artifacts

# Trigger Conditions

- Manual `workflow_dispatch`

# Failure Conditions

- Restore failure
- Release build failure
- Formatting verification failure
- Any core, feature, or integration test failure
- Coverage artifact generation failure caused by missing prerequisite outputs

# Synchronization Rules

- Update this document before changing `.github/workflows/build-and-test-long-running.yml`.
- Keep the documented command usage synchronized with the workflow YAML and `eng/ci/collect-coverage.sh`.
- Update `docs/TBPS.md` and relevant TBPs when the workflow introduces a new recurring execution pattern.

# Authority

This document is authoritative for:
- long-running build-and-test workflow intent

This document is not authoritative for:
- GitHub Actions YAML syntax
- detailed coverage artifact layout

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `docs/engineering/command-contract.md`
- `eng/format.sh`
- `eng/ci/collect-coverage.sh`

## Must Be Updated Together

When this workflow changes, review and update:
- `.github/workflows/build-and-test-long-running.yml`
- `eng/ci/collect-coverage.sh`
