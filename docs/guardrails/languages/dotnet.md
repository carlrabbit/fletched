# .NET Language Guardrails

## Purpose

This document defines .NET-specific language and toolchain guardrails for the Fletched repository.

## Language Conventions

| Convention | Rule |
| --- | --- |
| Namespace style | File-scoped namespaces (`namespace Foo;`) |
| Nullability | Nullable reference types enabled (`<Nullable>enable</Nullable>`) |
| Target language | C# 14 |
| Target framework | .NET 10 |
| Async | Use async APIs for I/O-bound work |

## Test Framework

| Convention | Rule |
| --- | --- |
| Framework | TUnit (Microsoft.Testing.Platform) |
| Test runner | `dotnet run` (not `dotnet test`) |
| Assertion style | Await all assertions |

## Source Generation

| Convention | Rule |
| --- | --- |
| Generator type | Roslyn Incremental Source Generators |
| Generator entry | `Fletched.Roslyn` project |
| Output | Compile-time generated C# code |

## Package Management

- Do not add NuGet packages without checking the GitHub advisory database for vulnerabilities.
- Prefer the lowest compatible version that satisfies requirements.
- Do not upgrade package versions unless required for correctness, security, or command-contract compliance.

## Build Configuration

- Always build in `Release` configuration for tests and benchmarks.
- Use `--no-restore` after a successful restore step.
- Use `Fletched.slnx` as the solution entry point.

## Formatting

Run `./eng/format.sh` before pushing if code formatting was modified.
Do not submit formatting-only changes unless the task specifically requires it.

# Authority

This document is authoritative for:
- .NET language conventions in Fletched
- .NET toolchain guardrails

This document is not authoritative for:
- generic implementation guardrails (see `docs/guardrails/implementation.md`)
- engineering commands (see `docs/engineering/dotnet.md`)
