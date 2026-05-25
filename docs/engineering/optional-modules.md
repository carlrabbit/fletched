# Optional Modules

## Purpose

This document lists optional capabilities in the Fletched repository and their activation conditions.

Optional modules exist only when the corresponding tooling is present.

## Optional Capabilities

| Capability | Status | Activation Condition |
| --- | --- | --- |
| Benchmarks | Active | `benchmarks/` directory exists; `eng/benchmark.sh` available |
| Performance testing workflow | Active | Separate workflow; not part of `eng/check.sh` |
| Blazor/Web UI | Not present | Not applicable |
| TypeScript tooling | Not present | Not applicable |
| Playwright | Not present | Not applicable |

## Rules

- Optional capabilities must not be included in `eng/check.sh` unless they are always available.
- Optional capabilities that exist (benchmarks) have a dedicated `eng/` script.
- Optional capabilities not yet present (Blazor, TypeScript, Playwright) have no `eng/` script.

# Authority

This document is authoritative for:
- listing optional module activation conditions
- clarifying what is and is not included in `eng/check.sh`

This document is not authoritative for:
- required capabilities (see `docs/engineering/building-blocks.md`)
- workflow implementation details (see `docs/WORKFLOWS.md`)
