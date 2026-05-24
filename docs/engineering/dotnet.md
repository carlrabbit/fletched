# .NET Engineering

## Purpose

This document defines .NET toolchain setup and project conventions for the Fletched repository.

## Toolchain

| Requirement | Details |
| --- | --- |
| SDK version | `global.json` pins `10.0.100` with `rollForward: latestFeature` |
| Target framework | .NET 10 |
| Language | C# 14 |
| Test framework | TUnit (Microsoft.Testing.Platform) |

## Project Structure

```text
src/
  Fletched.Core/         — runtime, DSL, and fact storage primitives
  Fletched.Roslyn/       — source generator and planning pipeline
tests/
  Fletched.Core.Tests/
  Fletched.Features.Tests/
  Fletched.Integration.Tests/
  Fletched.Performance.Tests/
  Fletched.Sample.Tests/
benchmarks/
  Fletched.Benchmarks/   — BenchmarkDotNet benchmark project
samples/
  WorkAssignment/        — runnable example application
```

## Solution Entry Point

```text
Fletched.slnx
```

## Commands

### Restore

```sh
./eng/restore.sh
# or directly:
dotnet restore Fletched.slnx
```

### Build

```sh
./eng/build.sh
# or directly:
dotnet build Fletched.slnx -c Release --no-restore
```

### Test

```sh
./eng/test.sh
# or per-project:
dotnet run --no-build -c Release --project tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj
```

### Format

```sh
./eng/format.sh
# or directly:
dotnet format whitespace Fletched.slnx
```

### Benchmark (build only)

```sh
./eng/benchmark.sh
# or directly:
dotnet build benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj -c Release
```

## Shared Repository Configuration

The repository standardizes .NET policy through root configuration files:

| File | Purpose |
| --- | --- |
| `.editorconfig` | Authoritative C# style and analyzer severity configuration |
| `Directory.Build.props` | Shared build, analyzer, deterministic-build, and central-package-management policy |
| `Directory.Packages.props` | Central NuGet package version management |
| `global.json` | .NET SDK pin and Microsoft.Testing.Platform runner configuration |

## Conventions

- Use file-scoped namespaces.
- Use nullable reference types.
- Use async APIs for I/O-bound work.
- Use TUnit for tests; await all assertions.
- Use `dotnet run` (not `dotnet test`) for Microsoft.Testing.Platform-based test projects.

# Authority

This document is authoritative for:
- .NET toolchain setup for Fletched
- project structure conventions
- .NET-specific command usage

This document is not authoritative for:
- canonical command contracts (see `docs/engineering/command-contract.md`)
- optional module activation rules (see `docs/engineering/optional-modules.md`)

# Document Contract

## Related Documents

- `docs/ENGINEERING.md`
- `docs/guardrails/languages/dotnet.md`
- `.editorconfig`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`

## Must Be Updated Together

When .NET toolchain setup changes, review and update:
- `docs/engineering/command-contract.md`
- `docs/guardrails/languages/dotnet.md`
- root toolchain/configuration files
