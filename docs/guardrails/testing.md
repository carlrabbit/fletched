# Testing Guardrails

## Purpose

This document defines constraints on test execution in the Fletched repository.

These guardrails prevent agents and contributors from running expensive operations by default.

## Test Classification

| Category | Description | Default Execution |
| --- | --- | --- |
| Fast tests | Core, features, and integration tests (short-running) | ✅ Always |
| Long-running integration tests | Tagged `Category("LongRunning")` | ❌ Opt-in only |
| Benchmarks | BenchmarkDotNet benchmark projects | ❌ Never in `eng/test.sh` |

## Rules

1. **`eng/test.sh` runs fast tests only.** It must not run benchmarks or long-running tests.
2. **Long-running tests require explicit activation.** Set `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1`.
   - `eng/test.sh` sets `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=0` unless explicitly provided by the caller.
3. **Benchmarks have a dedicated script.** Use `eng/benchmark.sh` to build them.
4. **Agents must not run benchmarks** unless explicitly instructed to do so.
5. **Agents must not enable long-running tests** unless the task specifically requires them.
6. **`eng/check.sh` is the canonical completion gate.** It calls `eng/test.sh` (fast tests only).

## Long-Running Test Activation

Long-running integration tests are controlled by:

```sh
FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1
```

When not set:
- Tests tagged `Category("LongRunning")` are automatically skipped.
- The default `eng/test.sh` and `eng/check.sh` will not execute them.

When set:
- Long-running tests execute in addition to fast tests.
- Only use this in the dedicated `build-and-test-long-running` workflow.

## Coverage and Reporting

Coverage collection is optional and controlled by workflow YAML.
It must not block test execution if coverage tooling is unavailable.

# Authority

This document is authoritative for:
- test classification rules
- test execution constraints for `eng/` scripts
- agent behavior during test execution

This document is not authoritative for:
- test authoring conventions (see individual test projects)
- CI workflow implementation (see `docs/WORKFLOWS.md`)
