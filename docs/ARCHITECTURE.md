# Architecture

## Purpose

Architecture documents describe current architectural structure, subsystem boundaries, and constraints.

Architecture documents are authoritative for structural decisions and subsystem responsibilities.
They are not authoritative for behavioral specifications, which belong in `docs/SPECS.md`.

## Available Architecture Documents

| Document | Purpose |
| --- | --- |
| [`architecture/system-overview.md`](architecture/system-overview.md) | Current architecture overview |
| [`architecture/source-generation-pipeline.md`](architecture/source-generation-pipeline.md) | Source generation pipeline architecture |
| [`architecture/execution-model.md`](architecture/execution-model.md) | Execution model architecture |
| [`architecture/fact-storage-and-indexing.md`](architecture/fact-storage-and-indexing.md) | Fact storage and indexing architecture |

# Authority

This document is authoritative for:
- the architecture document index for `docs/architecture/`
- routing readers to the current architecture set

This document is not authoritative for:
- architecture decisions (see `docs/DECISIONS.md`)
- behavioral specifications (see `docs/SPECS.md`)
- workflow behavior (see `docs/WORKFLOWS.md`)

# Document Contract

## Related Documents

- `README.md`
- `docs/DECISIONS.md`
- `docs/SPECS.md`
- `docs/architecture/system-overview.md`

## Must Be Updated Together

When the architecture document set changes, review and update:
- `README.md`
- `docs/architecture/system-overview.md`
- related decisions under `docs/decisions/`
