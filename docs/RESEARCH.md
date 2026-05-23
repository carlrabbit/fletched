# Research

## Purpose

Research documents preserve exploratory thinking, setup rationale, and non-authoritative reference material.

Research documents are intentionally less authoritative than specs, architecture, decisions, and TBPs.
Their durable conclusions must be promoted into authoritative documents before they govern behavior.

## Available Research Documents

| Document | Purpose |
| --- | --- |
| [`research/project-setup-guide-v4.md`](research/project-setup-guide-v4.md) | Project Setup Guide V4 — repository governance model and V4 upgrade requirements |
| [`research/engineering-guide-v3.md`](research/engineering-guide-v3.md) | Engineering Guide V3 — command contract, test classification, and guardrail requirements |
| [`research/documentation-structure-adoption.md`](research/documentation-structure-adoption.md) | Documentation structure adoption rationale |
| [`research/Project Setup V3.md`](research/Project%20Setup%20V3.md) | Project Setup Guide V3 (superseded by V4) |
| [`research/project-setup-guide-v2.md`](research/project-setup-guide-v2.md) | Project Setup Guide V2 (superseded by V3 and V4) |

## Non-Authoritativeness Rule

Research documents must not be treated as authoritative for:
- behavioral specifications
- architecture decisions
- workflow implementation
- engineering command contracts

When research conclusions are confirmed as durable, they must be promoted into the appropriate authoritative document.

# Authority

This document is authoritative for:
- the research document index for `docs/research/`
- the boundary between research and more authoritative document types

This document is not authoritative for:
- behavioral specifications
- architecture decisions
- workflow implementation

# Document Contract

## Related Documents

- `README.md`
- `docs/SPECS.md`
- `docs/TBPS.md`
- `docs/ENGINEERING.md`
- `docs/GUARDRAILS.md`

## Must Be Updated Together

When the research document set or research/document boundaries change, review and update:
- `README.md`
- related authoritative documents that supersede the research findings
