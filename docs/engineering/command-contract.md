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
| `./eng/format.sh` | Apply code formatting | Modifies source files |
| `./eng/check.sh` | Canonical completion gate | Calls restore + build + test |
| `./eng/benchmark.sh` | Build benchmarks | Optional; does not run benchmarks |

## Canonical Completion Gate

`./eng/check.sh` is the canonical completion gate.

Agents and CI workflows must call `./eng/check.sh` before declaring work complete.

## Rules

- Benchmarks must not run inside `eng/test.sh`.
- Long-running tests must not run inside `eng/test.sh` by default.
- Long-running integration tests require `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1`.
- Optional commands (`eng/benchmark.sh`) exist only when the corresponding capability exists.

## Test Execution Rules

| Category | Included in `eng/test.sh` | How to Enable |
| --- | --- | --- |
| Fast tests | ✅ Yes | Default |
| Long-running integration tests | ❌ No | `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1` |
| Benchmarks | ❌ No | `./eng/benchmark.sh` |

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
