# Guardrails

## Purpose

Guardrails define project-wide constraints that prevent agents and contributors from taking actions
that would degrade quality, performance, or repository consistency.

Guardrails are not optional. They apply to all contributors, agents, and CI workflows.

## Available Guardrail Documents

| Document | Purpose |
| --- | --- |
| [`guardrails/testing.md`](guardrails/testing.md) | Testing execution constraints and test classification rules |
| [`guardrails/implementation.md`](guardrails/implementation.md) | Implementation constraints for source code and documentation changes |
| [`guardrails/languages/dotnet.md`](guardrails/languages/dotnet.md) | .NET-specific language and toolchain guardrails |

## Guardrail Summary

### Testing

- `eng/test.sh` runs fast tests only.
- Benchmarks must not run during default test execution.
- Long-running tests must not run during default test execution.
- Agents must not call benchmarks or long-running test suites without explicit instructions.

### Implementation

- Prefer minimal, targeted changes over broad rewrites.
- Do not introduce new dependencies without security advisory review.
- Do not break existing tests.
- Do not remove or edit unrelated tests.

### .NET

- Use file-scoped namespaces.
- Use nullable reference types.
- Target C# 14 and .NET 10.
- Use `dotnet run` for Microsoft.Testing.Platform test projects.

# Authority

This document is authoritative for:
- the guardrail document index under `docs/guardrails/`
- guardrail policy routing

This document is not authoritative for:
- engineering command contracts (see `docs/ENGINEERING.md`)
- behavioral specifications (see `docs/SPECS.md`)

# Document Contract

## Related Documents

- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`
- `AGENTS.md`

## Must Be Updated Together

When guardrail policy changes, review and update:
- `AGENTS.md`
- `.github/copilot-instructions.md`
- `docs/ENGINEERING.md` if command contracts are affected
