# Goal

Run performance-focused validation and benchmarks independently from the standard build-and-test workflow.

# Constraints

- Use the same `.NET 10` SDK family as other GitHub workflows.
- Build the repository in `Release` before performance tests.
- Use `./eng/ci/collect-coverage.sh performance` for the performance-test coverage run.
- Use `./eng/benchmark.sh` as the benchmark build step; execute the benchmark project separately.
- Keep benchmark execution isolated from correctness-focused test workflow intent.
- Preserve artifact upload for performance reports and benchmark results.

# Non-Goals

- Standard core, feature, or integration validation.
- NuGet packaging or publication.
- Ad hoc benchmark configuration outside repository-defined benchmark projects.

# Relevant Other Workflows

- `build-and-test.md`
- `nuget-pack-and-publish.md`

# Inputs

- Repository source at the checked-out revision
- `tests/Fletched.Performance.Tests/Fletched.Performance.Tests.csproj`
- `benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj`

# Outputs

- Performance test results
- Coverage output for the performance test project
- Benchmark artifacts

# Trigger Conditions

- Manual `workflow_dispatch`

# Failure Conditions

- Restore or release build failure
- Performance test failure
- Benchmark project build failure
- Benchmark execution failure

# Synchronization Rules

- Update this document before changing `.github/workflows/performance-testing.yml`.
- Keep the documented command usage synchronized with the workflow YAML and `eng/ci/collect-coverage.sh`.
- Update related TBPs when performance validation becomes part of a repeated engineering process.

# Authority

This document is authoritative for:
- performance workflow intent
- benchmark build-versus-execution semantics in CI

This document is not authoritative for:
- GitHub Actions YAML syntax
- benchmark implementation details inside the benchmark project

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `docs/engineering/command-contract.md`
- `eng/benchmark.sh`
- `eng/ci/collect-coverage.sh`

## Must Be Updated Together

When this workflow changes, review and update:
- `.github/workflows/performance-testing.yml`
- `eng/benchmark.sh`
- `eng/ci/collect-coverage.sh`
