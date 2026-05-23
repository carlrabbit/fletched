# Workflows

## Purpose

Workflow specifications describe operational intent before CI implementation.

Workflow specs are authoritative over:
- workflow purpose
- workflow constraints
- high-level behavior
- validation expectations

GitHub Actions YAML files are implementation artifacts.

## Available Workflows

| Workflow | Purpose |
| --- | --- |
| [`build-and-test.md`](workflows/build-and-test.md) | Validate restore, build, and automated test execution |
| [`build-and-test-long-running.md`](workflows/build-and-test-long-running.md) | Validate restore, build, and automated test execution with long-running integration tests enabled |
| [`nuget-pack-and-publish.md`](workflows/nuget-pack-and-publish.md) | Pack NuGet artifacts and publish tagged releases |
| [`performance-testing.md`](workflows/performance-testing.md) | Run performance-focused tests and benchmarks |

## Engineering Command Contract Integration

CI workflows call canonical `eng/` scripts:

- `./eng/check.sh` — canonical completion gate (restore + build + fast tests)
- `./eng/benchmark.sh` — benchmark build (performance-testing workflow only)

See `docs/ENGINEERING.md` for the full command contract.

# Authority

This document is authoritative for:
- workflow specification indexing
- workflow documentation scope
- workflow/document synchronization expectations

This document is not authoritative for:
- GitHub Actions implementation details
- release policy rationale
- runtime architecture

# Document Contract

## Related Documents

- `.github/workflows/`

## Must Be Updated Together

When workflow intent scope or the workflow index changes, review and update:
- corresponding files in `.github/workflows/`
