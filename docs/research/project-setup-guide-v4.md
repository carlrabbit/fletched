# Project Setup Guide V4

> **Research input.** This document is non-authoritative. Its durable rules must be promoted into the
> appropriate authoritative documents (`docs/ENGINEERING.md`, `docs/GUARDRAILS.md`, `AGENTS.md`,
> `docs/WORKFLOWS.md`, etc.) rather than referenced directly as behavior.

---

## Goal

Create a repository structure that acts as a **Semantic Engineering System** optimized for:

- humans
- AI agents
- long-lived maintainability
- operational clarity
- deterministic repository semantics

---

## Repository Governance Model

```text
Terminology defines words.
Architecture defines structure.
Specs define truth.
Decisions define rationale.
Milestones define sequencing.
TBPs define methodology.
Guardrails define project-wide constraints.
Engineering defines command contracts and toolchain setup.
Issues define concrete work.
Workflows define operations.
Research preserves non-authoritative rationale.
```

---

## Index Convention

Each documentation folder is indexed by exactly one ALLCAPS document under `docs/`:

| Folder | Required Index |
| --- | --- |
| `docs/architecture/` | `docs/ARCHITECTURE.md` |
| `docs/decisions/` | `docs/DECISIONS.md` |
| `docs/specs/` | `docs/SPECS.md` |
| `docs/milestones/` | `docs/MILESTONES.md` |
| `docs/tbps/` | `docs/TBPS.md` |
| `docs/workflows/` | `docs/WORKFLOWS.md` |
| `docs/guardrails/` | `docs/GUARDRAILS.md` |
| `docs/engineering/` | `docs/ENGINEERING.md` |
| `docs/research/` | `docs/RESEARCH.md` |

---

## README Rule

Only the root `README.md` is allowed.

Do not leave local README files under:

```text
docs/**
eng/
samples/
tools/**
site/
```

---

## Engineering Command Contract

The repository exposes canonical commands through `eng/` scripts:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
./eng/benchmark.sh
```

Optional commands exist only if the corresponding capability exists.

CI and agents use these scripts instead of duplicating command logic.

---

## Required Documentation Areas

```text
docs/ENGINEERING.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
docs/engineering/building-blocks.md
docs/engineering/optional-modules.md
docs/GUARDRAILS.md
docs/guardrails/testing.md
docs/guardrails/implementation.md
docs/guardrails/languages/dotnet.md
docs/RESEARCH.md
```

---

## Agent and Copilot Routing

`AGENTS.md` must require reading:

- `docs/GUARDRAILS.md`
- `docs/ENGINEERING.md`
- relevant `docs/engineering/*` documents

`.github/copilot-instructions.md` routes through `AGENTS.md`, `docs/GUARDRAILS.md`, and `docs/ENGINEERING.md`.

---

## Issue Template Model

Issue templates reference the documentation areas relevant to the issue type:

- `bug` → specs, guardrails, engineering
- `documentation` → relevant doc folders, TBPS
- `milestone-implementation` → MILESTONES.md, SPECS.md, TBPS.md
- `release` → WORKFLOWS.md, workflow specs

---

## V4 Upgrade from V3

Changes from V3 to V4:

- Add `docs/GUARDRAILS.md` and `docs/guardrails/` family.
- Add `docs/ENGINEERING.md` and `docs/engineering/` family.
- Add `docs/RESEARCH.md`.
- Add engineering command contract via `eng/` scripts.
- Add `docs/MILESTONES.md` top-level index.
- Add `docs/ARCHITECTURE.md`, `docs/DECISIONS.md`, `docs/SPECS.md` top-level indexes.
- Remove all `docs/**/README.md` files.
- Update agent and Copilot routing.
- Update issue templates.
