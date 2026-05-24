# Engineering

## Purpose

This document indexes the engineering substrate for the Fletched repository.

Engineering documents define:
- canonical command contracts
- toolchain setup
- build and test execution rules
- optional module activation rules

## Available Engineering Documents

| Document | Purpose |
| --- | --- |
| [`engineering/command-contract.md`](engineering/command-contract.md) | Canonical `eng/` script contracts and usage rules |
| [`engineering/dotnet.md`](engineering/dotnet.md) | .NET toolchain setup and project conventions |
| [`engineering/building-blocks.md`](engineering/building-blocks.md) | Core toolchain building blocks |
| [`engineering/codespaces.md`](engineering/codespaces.md) | Codespaces and dev-container guidance |
| [`engineering/optional-modules.md`](engineering/optional-modules.md) | Optional capabilities and activation conditions |
| [`engineering/packaging.md`](engineering/packaging.md) | Packaging and release document routing |
| [`engineering/samples.md`](engineering/samples.md) | Sample overview and execution guidance |

## Engineering Command Summary

| Script | Purpose |
| --- | --- |
| `./eng/restore.sh` | Restore dependencies |
| `./eng/build.sh` | Release build |
| `./eng/test.sh` | Fast test execution (no benchmarks, no long-running tests) |
| `./eng/format.sh` | Apply or verify whitespace formatting (`dotnet format whitespace`) |
| `./eng/check.sh` | Canonical completion gate (restore + build + fast tests + format verification) |
| `./eng/benchmark.sh` | Build benchmarks only (optional; see decision `0002`) |
| `./eng/package.sh` | Pack maintained NuGet projects to `artifacts/nuget` |
| `./eng/publish.sh` | Publish packaged artifacts in `artifacts/nuget` to nuget.org |

`./eng/check.sh` is the canonical completion gate for CI and agents.

# Authority

This document is authoritative for:
- the engineering document index under `docs/engineering/`
- the canonical command contract summary
- engineering document routing

This document is not authoritative for:
- feature behavioral specifications (see `docs/SPECS.md`)
- workflow implementation details (see `docs/WORKFLOWS.md`)
- guardrail policy (see `docs/GUARDRAILS.md`)

# Document Contract

## Related Documents

- `docs/GUARDRAILS.md`
- `docs/WORKFLOWS.md`
- `eng/check.sh`

## Must Be Updated Together

When engineering commands or toolchain setup change, review and update:
- `docs/engineering/command-contract.md`
- `docs/engineering/dotnet.md`
- relevant `eng/` scripts
- `docs/WORKFLOWS.md` and `.github/workflows/` if CI commands change
- `AGENTS.md` and `.github/copilot-instructions.md`
