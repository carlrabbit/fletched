# Goal

Run performance-focused validation and benchmarks independently from the standard build-and-test workflow.

# Constraints

- Use the same `.NET 10` SDK family as other GitHub workflows.
- Build the repository in `Release` before performance tests.
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
- Keep the documented test and benchmark projects synchronized with the workflow YAML.
- Update related TBPs when performance validation becomes part of a repeated engineering process.
