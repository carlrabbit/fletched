# Goal

Validate repository restore, release build, and automated test execution including long-running integration tests that are guarded by `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS`.

# Constraints

- Use `Fletched.slnx` as the build entry point.
- Build in `Release` configuration.
- Run existing test projects without rebuilding after the release build step.
- Enable long-running integration tests by setting `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS` for the integration test execution step.
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
- Any core, feature, or integration test failure (including long-running integration tests when enabled)
- Coverage artifact generation failure caused by missing prerequisite outputs

# Synchronization Rules

- Update this document before changing `.github/workflows/build-and-test-long-running.yml`.
- Keep the documented test project set synchronized with the workflow YAML.
- Update `docs/TBPS.md` and relevant TBPs when the workflow introduces a new recurring execution pattern.
