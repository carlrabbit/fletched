# Engineering

## Purpose

This document indexes engineering contracts and canonical `eng/` commands.

## Available Engineering Documents

| Document | Purpose |
| --- | --- |
| [`engineering/command-contract.md`](engineering/command-contract.md) | Canonical `eng/` script contracts |
| [`engineering/dotnet.md`](engineering/dotnet.md) | .NET toolchain setup and conventions |
| [`engineering/building-blocks.md`](engineering/building-blocks.md) | Core toolchain building blocks |
| [`engineering/codespaces.md`](engineering/codespaces.md) | Codespaces and dev-container guidance |
| [`engineering/optional-modules.md`](engineering/optional-modules.md) | Optional capabilities and activation rules |
| [`engineering/packaging.md`](engineering/packaging.md) | Packaging and release routing |
| [`engineering/samples.md`](engineering/samples.md) | Sample overview and execution guidance |

## Engineering Command Summary

| Script | Purpose |
| --- | --- |
| `./eng/restore.sh` | Restore dependencies |
| `./eng/build.sh` | Release build |
| `./eng/test.sh` | Fast test execution |
| `./eng/format.sh` | Apply/verify whitespace formatting |
| `./eng/check.sh` | Canonical completion gate |
| `./eng/benchmark.sh` | Benchmark build-only command |
| `./eng/package.sh <version>` | Pack maintained NuGet projects |
| `./eng/package-smoke.sh [version]` | Validate local package consumption from packed artifacts |
| `./eng/public-api.sh` | Validate public API baselines |
| `./eng/public-docs.sh [version]` | Validate consumer-facing public docs |
| `./eng/release-check.sh <version>` | Full release-readiness gate |
| `./eng/publish.sh` | Explicit publish command (guarded by `NUGET_API_KEY`) |

`./eng/check.sh` remains the canonical completion gate for normal development and CI.
