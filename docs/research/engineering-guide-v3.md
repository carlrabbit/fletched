# Engineering Guide V3

> **Research input.** This document is non-authoritative. Its durable rules must be promoted into the
> appropriate authoritative documents (`docs/ENGINEERING.md`, `docs/engineering/*`, `docs/GUARDRAILS.md`,
> etc.) rather than referenced directly as behavior.

---

## Goal

Define a canonical engineering substrate for the repository that:

- exposes reproducible command contracts via `eng/` scripts
- separates command intent from CI implementation details
- classifies test execution into fast, long-running, integration, benchmark, and optional categories
- establishes guardrails that prevent agents and contributors from running expensive operations by default

---

## Engineering Command Contract

### Canonical Scripts

```text
./eng/restore.sh     — restore dependencies
./eng/build.sh       — release build
./eng/test.sh        — fast test execution (no benchmarks, no long-running)
./eng/format.sh      — apply code formatting
./eng/check.sh       — canonical completion gate (restore + build + test)
./eng/benchmark.sh   — benchmark execution (optional, only if benchmarks exist)
```

### Rules

- `eng/check.sh` is the canonical completion gate.
- CI and agents use these scripts instead of duplicating command logic.
- Benchmarks must not run inside `eng/test.sh`.
- Long-running tests must not run inside `eng/test.sh` by default.
- `eng/benchmark.sh` is optional and exists only if benchmark capability exists.

---

## Test Classification

| Category | When Run | How Enabled |
| --- | --- | --- |
| Fast tests | Always (default) | No flag required |
| Long-running tests | Explicit workflow only | `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1` |
| Benchmarks | Explicit `eng/benchmark.sh` | Separate script |

---

## Building Blocks

### Toolchain

- .NET SDK (version locked via `global.json`)
- C# source generation via Roslyn
- TUnit test framework (Microsoft.Testing.Platform)

### Project Structure

```text
src/          — library and generator source
tests/        — test projects
benchmarks/   — benchmark projects (optional)
samples/      — runnable example applications
eng/          — canonical command scripts
docs/         — authoritative documentation
```

---

## Optional Modules

Optional capabilities exist only when the corresponding tooling is present in the repository:

| Capability | Activation Condition |
| --- | --- |
| Benchmarks | `benchmarks/` directory exists |
| Long-running tests | `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS` environment variable |
| Performance testing | Separate workflow; not part of `eng/check.sh` |

---

## .NET Engineering Specifics

### Restore

```sh
dotnet restore Fletched.slnx
```

### Build

```sh
dotnet build Fletched.slnx -c Release --no-restore
```

### Test (fast)

Run all test projects except benchmarks and long-running integration tests:

```sh
dotnet run --no-build -c Release --project tests/Fletched.Core.Tests/...
dotnet run --no-build -c Release --project tests/Fletched.Features.Tests/...
dotnet run --no-build -c Release --project tests/Fletched.Integration.Tests/...
```

Long-running integration tests are excluded unless `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1`.

### Format

```sh
dotnet format Fletched.slnx
```

### Benchmark

```sh
dotnet build benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj -c Release
```

---

## Guardrails

Engineering guardrails prevent:

- running benchmarks during default test execution
- running long-running tests outside of explicit long-running workflows
- agents duplicating command logic that belongs in `eng/` scripts
- CI workflow drift from the canonical command contract

---

## CI Integration

CI workflows call canonical `eng/` scripts instead of raw dotnet commands:

```yaml
- name: Check
  run: ./eng/check.sh
```

This ensures CI and local development use the same commands.
