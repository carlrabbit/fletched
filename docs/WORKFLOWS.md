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
| [`release.md`](workflows/release.md) | Human release process and pre-publish gate sequence |
| [`performance-testing.md`](workflows/performance-testing.md) | Run performance-focused tests and benchmarks |

## Engineering Command Contract Integration

CI workflows call canonical `eng/` scripts:

- `./eng/check.sh` — canonical completion gate (restore + build + fast tests + format verification)
- `./eng/ci/collect-coverage.sh` — CI-only helper for coverage-producing reruns
- `./eng/benchmark.sh` — benchmark build (performance-testing workflow only; benchmark execution remains explicit per `docs/decisions/0002-benchmark-command-build-only.md`)
- `./eng/package.sh` and `./eng/publish.sh` — packaging/publishing entry points for the NuGet workflow
- `./eng/public-api.sh`, `./eng/public-docs.sh`, `./eng/package-smoke.sh`, and `./eng/release-check.sh <version>` — release-readiness validation commands

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
