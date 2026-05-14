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
| [`nuget-pack-and-publish.md`](workflows/nuget-pack-and-publish.md) | Pack NuGet artifacts and publish tagged releases |
| [`performance-testing.md`](workflows/performance-testing.md) | Run performance-focused tests and benchmarks |

## Cross-document synchronization workflow

Changes to predicate invocation or negation semantics must update the authoritative documentation set together:

- `specs/PredicateInvocation.md.txt`
- `specs/Backtracking.md.txt`
- `specs/LoweringRules.md.txt`
- `specs/Diagnostics.md.txt`
- `specs/DSL.md.txt`
- `docs/TERMINOLOGY.md`

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

- `docs/workflows/README.md`
- `.github/workflows/README.md`
- `.github/workflows/`

## Must Be Updated Together

When workflow intent scope or the workflow index changes, review and update:
- `docs/workflows/README.md`
- `.github/workflows/README.md`
- corresponding files in `.github/workflows/`
