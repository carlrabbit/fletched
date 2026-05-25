# Testing Guardrails

## Purpose

This document defines constraints on test execution in the Fletched repository.

These guardrails prevent agents and contributors from running expensive operations by default.

## Test Classification

| Category | Description | Default Execution |
| --- | --- | --- |
| Fast tests | Core, features, and integration tests (short-running) | ✅ Always |
| Benchmarks | BenchmarkDotNet benchmark projects | ❌ Never in `eng/test.sh` |

## Rules

1. **`eng/test.sh` runs fast tests only.** It must not run benchmarks.
2. **Benchmarks have a dedicated script.** Use `eng/benchmark.sh` to build them.
3. **Agents must not run benchmarks** unless explicitly instructed to do so.
4. **`eng/check.sh` is the canonical completion gate.** It calls `eng/test.sh` (fast tests only).

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
