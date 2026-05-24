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
| `./eng/benchmark.sh` | Build benchmarks | Optional; documented repository deviation from benchmark-execution default |

## Canonical Completion Gate

`./eng/check.sh` is the canonical completion gate.

Agents and CI workflows must call `./eng/check.sh` before declaring work complete.

## Rules

- Benchmarks must not run inside `eng/test.sh`.
- Long-running tests must not run inside `eng/test.sh` by default.
- Long-running integration tests require `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1`.
- Top-level `eng/*.sh` scripts use POSIX `sh`.
- `eng/benchmark.sh` is intentionally build-only; workflows or operators run the benchmark executable separately when benchmark execution is required.
- Optional commands (`eng/benchmark.sh`) exist only when the corresponding capability exists.

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
