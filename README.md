# Fletched

Typed relational engine for .NET with a compiled DSL, source-generated execution code, and explicit runtime fact storage.

## Purpose

Fletched provides a strongly typed logic layer for .NET applications.

The repository contains:

- `src/Fletched.Core` — runtime, DSL, and fact storage primitives
- `src/Fletched.Roslyn` — source generator and planning pipeline
- `tests/` — core, integration, feature, performance, and sample tests
- `samples/` — runnable example applications
- `specs/` — detailed design notes for selected runtime and generator topics
- `docs/` — authoritative engineering documentation

## Authoritative documentation

Read these documents first:

| Document | Purpose |
| --- | --- |
| [`AGENTS.md`](AGENTS.md) | Repository routing rules and synchronization requirements |
| [`copilot-instructions.md`](copilot-instructions.md) | Concise Copilot-facing repository instructions kept in sync with GitHub configuration |
| [`docs/TERMINOLOGY.md`](docs/TERMINOLOGY.md) | Canonical project vocabulary |
| [`docs/SPECS.md`](docs/SPECS.md) | Specification index and authoring rules |
| [`docs/WORKFLOWS.md`](docs/WORKFLOWS.md) | Workflow documentation index |
| [`docs/TBPS.md`](docs/TBPS.md) | Task best practice index |
| [`docs/architecture/system-overview.md`](docs/architecture/system-overview.md) | Current architecture overview |
| [`docs/agent-context/project-context.md`](docs/agent-context/project-context.md) | High-signal repository context |

## Documentation structure

| Path | Responsibility |
| --- | --- |
| `docs/architecture/` | Current architecture, subsystem boundaries, and constraints |
| `docs/decisions/` | Architectural decisions and rationale |
| `docs/milestones/` | Delivery planning and staged scope |
| `docs/research/` | Exploratory findings and concise conclusions |
| `docs/specs/` | Authoritative behavioral specifications |
| `docs/workflows/` | Workflow intent independent from GitHub Actions YAML |
| `docs/tbps/` | Reusable project-specific execution guidance |
| `docs/agent-context/` | Compressed repository context for humans and agents |

## Supplementary documentation

| Document | Purpose |
| --- | --- |
| [`Samples.md`](Samples.md) | Sample overview and execution commands |
| [`Codespace.md`](Codespace.md) | Codespaces and dev container guidance |
| [`Nuget.md`](Nuget.md) | Short link document for NuGet packaging and release docs |
| [`specs/`](specs/) | Detailed design notes that support architecture and implementation work |

## Synchronization summary

- Workflow intent belongs in `docs/workflows/` before `.github/workflows/` changes.
- New terminology belongs in `docs/TERMINOLOGY.md`.
- New or revised specifications belong in `docs/specs/` and must stay indexed in `docs/SPECS.md`.
- Repeated operational patterns belong in `docs/tbps/`.
- Architectural changes belong in `docs/architecture/` and `docs/decisions/`.
- Concrete recurring issue intake belongs in `.github/ISSUE_TEMPLATE/`.
