# Fletched

Typed relational engine for .NET with a compiled DSL, source-generated execution code, and explicit runtime fact storage.

## Purpose

Fletched provides a strongly typed logic layer for .NET applications.

The repository contains:

- `src/Fletched.Core` — runtime, DSL, and fact storage primitives
- `src/Fletched.Roslyn` — source generator and planning pipeline
- `tests/` — core, integration, feature, performance, and sample tests
- `samples/` — runnable example applications
- `specs/` — supplementary design notes; authoritative specifications live under `docs/specs/`
- `docs/` — authoritative engineering documentation

## Authoritative documentation

Read these documents first:

| Document | Purpose |
| --- | --- |
| [`AGENTS.md`](AGENTS.md) | Repository routing rules and synchronization requirements |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Authoritative GitHub Copilot routing instructions |
| [`docs/TERMINOLOGY.md`](docs/TERMINOLOGY.md) | Canonical project vocabulary |
| [`docs/SPECS.md`](docs/SPECS.md) | Specification index and authoring rules |
| [`docs/WORKFLOWS.md`](docs/WORKFLOWS.md) | Workflow documentation index |
| [`docs/TBPS.md`](docs/TBPS.md) | Task best practice index |
| [`docs/ENGINEERING.md`](docs/ENGINEERING.md) | Engineering command contracts and toolchain setup |
| [`docs/GUARDRAILS.md`](docs/GUARDRAILS.md) | Project-wide constraints and guardrail policy |
| [`docs/architecture/system-overview.md`](docs/architecture/system-overview.md) | Current architecture overview |
| [`docs/agent-context/project-context.md`](docs/agent-context/project-context.md) | High-signal repository context |

## Documentation structure

| Path | Responsibility |
| --- | --- |
| `docs/architecture/` | Current architecture, subsystem boundaries, and constraints |
| `docs/decisions/` | Architectural decisions and rationale |
| `docs/engineering/` | Engineering command contracts and toolchain documents |
| `docs/guardrails/` | Project-wide constraint policy documents |
| `docs/milestones/` | Delivery planning and staged scope |
| `docs/research/` | Exploratory findings and concise conclusions |
| `docs/specs/` | Authoritative behavioral specifications |
| `docs/workflows/` | Workflow intent independent from GitHub Actions YAML |
| `docs/tbps/` | Reusable project-specific execution guidance |
| `docs/agent-context/` | Compressed repository context for humans and agents |

## Supplementary documentation

| Document | Purpose |
| --- | --- |
| [`docs/engineering/samples.md`](docs/engineering/samples.md) | Sample overview and execution commands |
| [`docs/engineering/codespaces.md`](docs/engineering/codespaces.md) | Codespaces and dev container guidance |
| [`docs/engineering/packaging.md`](docs/engineering/packaging.md) | Packaging and release document routing |
| [`specs/`](specs/) | Detailed design notes that support architecture and implementation work |

## Synchronization summary

- Workflow intent belongs in `docs/workflows/` before `.github/workflows/` changes.
- New terminology belongs in `docs/TERMINOLOGY.md`.
- New or revised specifications belong in `docs/specs/` and must stay indexed in `docs/SPECS.md`.
- Repeated operational patterns belong in `docs/tbps/`.
- Architectural changes belong in `docs/architecture/` and `docs/decisions/`.
- Concrete recurring issue intake belongs in `.github/ISSUE_TEMPLATE/`.
- Engineering commands are defined in `docs/ENGINEERING.md`; use `./eng/check.sh` as the canonical completion gate.
