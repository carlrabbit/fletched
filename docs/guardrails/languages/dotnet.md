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
`./eng/format.sh` uses `dotnet format whitespace`.
Do not submit formatting-only changes unless the task specifically requires it.

## Analyzer and Style Enforcement

- Root `.editorconfig` is authoritative for C# style.
- Analyzer severities must be configured in `.editorconfig`.
- Build must enable analyzer execution through `Directory.Build.props`.
- Code style must be enforced in build.
- Production and test projects treat compiler warnings as errors; analyzer and style diagnostics run in build at configured severity without being promoted to errors repository-wide.
- NuGet package versions must be centrally managed through `Directory.Packages.props`.
- `CS1591` is suppressed centrally until repository-wide public API XML documentation is standardized.
- `src/Fletched.Roslyn` suppresses `CS1570`, `CS8604`, and `RS2008` as documented transitional exceptions while analyzer release tracking and nullable cleanup remain out of scope for this repository-standards milestone.
- Test and benchmark projects suppress generated-code warnings `CS0436`, `CS0649`, `CS8602` and recursive-planning diagnostics `FLM3002`/`FLM3004` as documented transitional exceptions for source-generated validation assets.

# Authority

This document is authoritative for:
- .NET language conventions in Fletched
- .NET toolchain guardrails

This document is not authoritative for:
- generic implementation guardrails (see `docs/guardrails/implementation.md`)
- engineering commands (see `docs/engineering/dotnet.md`)

# Document Contract

## Related Documents

- `docs/GUARDRAILS.md`
- `docs/engineering/dotnet.md`
- `.editorconfig`
- `Directory.Build.props`
- `Directory.Packages.props`

## Must Be Updated Together

When .NET guardrails change, review and update:
- `docs/engineering/dotnet.md`
- root build/style configuration files
