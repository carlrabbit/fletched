# Command Contract

## Purpose

This document defines the canonical `eng/` script command contract for the Fletched repository.

CI workflows and agents must use these scripts instead of duplicating command logic.

## Canonical Commands

| Script | Purpose | Notes |
| --- | --- | --- |
| `./eng/restore.sh` | Restore dependencies | Run before build |
| `./eng/build.sh` | Release build | Requires restore first |
| `./eng/test.sh` | Fast test execution | No benchmarks, no long-running tests |
| `./eng/format.sh` | Apply or verify repository formatting | Uses `dotnet format whitespace`; pass `--verify-no-changes` for validation |
| `./eng/check.sh` | Canonical completion gate | Calls restore + build + fast tests + formatting verification |
| `./eng/benchmark.sh` | Build benchmarks | Optional; repository decision `0002` keeps this build-only |
| `./eng/package.sh <version>` | Pack maintained NuGet projects | Requires one SemVer-compatible version; writes to `artifacts/nuget` |
| `./eng/package-smoke.sh [version]` | Validate package consumption via local NuGet artifacts | Uses packed packages (not project references) in a clean consumer project |
| `./eng/public-api.sh` | Validate public API baselines | Fails on accidental API drift unless baseline update is explicit |
| `./eng/public-docs.sh [version]` | Validate public documentation layer | Checks required files, snippets, and policy references |
| `./eng/release-check.sh <version>` | Full release-readiness gate | Runs check + public-api + public-docs + pack + package-smoke + hygiene checks |
| `./eng/publish.sh` | Publish packaged artifacts | Requires `NUGET_API_KEY`; pushes `artifacts/nuget/*.nupkg` with `--skip-duplicate` |

## Canonical Completion Gate

`./eng/check.sh` is the canonical completion gate.

Agents and CI workflows must call `./eng/check.sh` before declaring work complete.

## Rules

- Benchmarks must not run inside `eng/test.sh`.
- Long-running tests must not run inside `eng/test.sh` by default.
- Long-running integration tests require `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1`.
- Top-level `eng/*.sh` scripts use POSIX `sh`.
- Formatting validation is intentionally whitespace-only (see `docs/decisions/0003-format-command-whitespace-only.md`).
- `eng/test.sh` exports `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=0` unless explicitly set, making default long-running exclusion visible at the script boundary.
- `eng/benchmark.sh` is intentionally build-only; workflows or operators run the benchmark executable separately when benchmark execution is required (see `docs/decisions/0002-benchmark-command-build-only.md`).
- Optional commands (`eng/benchmark.sh`, `eng/package.sh`, `eng/publish.sh`) exist only when the corresponding capability exists.

## CI-only Helpers

Workflow-specific helpers live under `eng/ci/`.

| Script | Purpose |
| --- | --- |
| `./eng/ci/collect-coverage.sh` | Re-run repository test projects with coverage for standard, long-running, or performance workflows |

## Test Execution Rules

| Category | Included in `eng/test.sh` | How to Enable |
| --- | --- | --- |
| Fast tests | ✅ Yes | Default |
| Long-running integration tests | ❌ No | `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1` |
| Benchmarks | ❌ No | `./eng/benchmark.sh` builds; workflow/operator executes the benchmark runner separately |

## Script Locations

All scripts are located under `eng/` in the repository root.

Scripts must be executable (`chmod +x`).

# Authority

This document is authoritative for:
- the `eng/` script command contract
- test execution classification rules

This document is not authoritative for:
- workflow implementation details (see `docs/WORKFLOWS.md`)
- guardrail policy (see `docs/GUARDRAILS.md`)

# Document Contract

## Related Documents

- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`
- `docs/WORKFLOWS.md`

## Must Be Updated Together

When the command contract changes, review and update:
- top-level scripts under `eng/`
- workflow-specific helpers under `eng/ci/`
- workflow specifications under `docs/workflows/`
- GitHub workflow files under `.github/workflows/`
