# Goal

Validate repository restore, release build, and automated test execution for the maintained solution and sample test projects.

# Constraints

- Use `Fletched.slnx` as the build entry point.
- Use the canonical completion gate `./eng/check.sh`.
- Use `./eng/ci/collect-coverage.sh standard` for coverage-producing reruns after canonical validation succeeds.
- Keep workflow implementation focused on execution and artifact handling.

# Non-Goals

- Performance benchmarking.
- Package publishing.
- Detailed architectural explanation of the runtime or generator.

# Relevant Other Workflows

- `build-and-test-long-running.md`
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
- Release build, fast test, or formatting-verification failure from `./eng/check.sh`
- Any core, feature, or integration test failure during the coverage-producing rerun (excluding long-running integration tests unless `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS` is set)
- Coverage artifact generation failure caused by missing prerequisite outputs

# Synchronization Rules

- Update this document before changing `.github/workflows/build-and-test.yml`.
- Keep the documented command usage synchronized with the workflow YAML and `eng/ci/collect-coverage.sh`.
- Update `docs/TBPS.md` and relevant TBPs when the workflow introduces a new recurring execution pattern.

# Authority

This document is authoritative for:
- build-and-test workflow intent
- canonical command usage for the standard validation workflow

This document is not authoritative for:
- GitHub Actions YAML syntax
- detailed coverage artifact layout

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `docs/engineering/command-contract.md`
- `eng/check.sh`
- `eng/ci/collect-coverage.sh`

## Must Be Updated Together

When this workflow changes, review and update:
- `.github/workflows/build-and-test.yml`
- `eng/check.sh`
- `eng/ci/collect-coverage.sh`
