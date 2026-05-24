# Project Setup Guide V4

## Status

Authoritative project-setup guide.

## Purpose

This guide defines how a repository is structured as a documentation-first, AI-assisted engineering system.

The guide is intentionally not stack-specific. It defines the repository knowledge model, documentation conventions, index rules, task best practices, specifications, milestones, workflows, guardrails, issue templates, and agent routing.

For concrete build, test, formatting, benchmark, packaging, .NET, Bun, Biome, Blazor, Playwright, samples, and GitHub Pages setup, use:

- `docs/engineering/dotnet.md`
- `docs/ENGINEERING.md`

The engineering guide is the stack profile. This project setup guide is the repository governance model.

## Core model

The repository separates the following responsibilities.

| Layer | Responsibility |
|---|---|
| `README.md` | Human entry point and basic repository navigation. |
| `docs/TERMINOLOGY.md` | Canonical vocabulary. |
| `docs/ARCHITECTURE.md` | Index for structural system design. |
| `docs/DECISIONS.md` | Index for decision records and rationale. |
| `docs/SPECS.md` | Index for behavioral truth and invariants. |
| `docs/MILESTONES.md` | Index for controlled implementation phases. |
| `docs/TBPS.md` | Index for reusable operational methodology. |
| `docs/WORKFLOWS.md` | Index for operational workflow specifications. |
| `docs/GUARDRAILS.md` | Index for cross-cutting implementation and testing constraints. |
| `docs/ENGINEERING.md` | Index for concrete engineering substrate and stack profiles. |
| `docs/RESEARCH.md` | Index for non-authoritative research and rationale. |
| `AGENTS.md` | Concise agent routing and repository synchronization rules. |
| `.github/copilot-instructions.md` | Concise GitHub Copilot routing rules. |
| `.github/ISSUE_TEMPLATE/*.md` | Lightweight issue templates that route work to the correct documents and TBPs. |

The governing rule is:

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
```

## Non-goals

This guide does not define:

- application architecture for a concrete product;
- domain model semantics;
- language-specific implementation rules;
- package versions;
- CI YAML details;
- concrete .NET, TypeScript, Blazor, or Playwright setup.

Those belong in the engineering guide, guardrails, specs, workflows, architecture documents, or decisions.

---

# 1. Repository structure

## Required base structure

```text
/
├─ README.md
├─ AGENTS.md
│
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ ARCHITECTURE.md
│  ├─ DECISIONS.md
│  ├─ SPECS.md
│  ├─ MILESTONES.md
│  ├─ TBPS.md
│  ├─ WORKFLOWS.md
│  ├─ GUARDRAILS.md
│  ├─ ENGINEERING.md
│  ├─ RESEARCH.md
│  │
│  ├─ architecture/
│  ├─ decisions/
│  ├─ specs/
│  ├─ milestones/
│  ├─ tbps/
│  ├─ workflows/
│  ├─ guardrails/
│  │  ├─ implementation.md
│  │  ├─ testing.md
│  │  └─ languages/
│  ├─ engineering/
│  │  └─ dotnet.md
│  └─ research/
│     ├─ project-setup-guide-v4.md
│     └─ engineering-guide-v3.md
│
└─ .github/
   ├─ ISSUE_TEMPLATE/
   │  ├─ bug.md
   │  ├─ documentation.md
   │  ├─ milestone-implementation.md
   │  └─ release.md
   ├─ workflows/
   ├─ instructions/
   └─ copilot-instructions.md
```

Concrete implementation repositories may add language and stack folders, for example:

```text
eng/
src/
tests/
benchmarks/
samples/
site/
tools/
artifacts/
packages/
.config/
```

Those folders are governed by `docs/ENGINEERING.md`, `docs/engineering/dotnet.md`, and the relevant guardrails.

## README rule

Only the root-level repository `README.md` is allowed.

Do not create additional `README.md` files anywhere else in the repository.

This includes:

```text
docs/**/README.md
eng/README.md
samples/README.md
tools/**/README.md
site/README.md
```

Use named Markdown documents instead:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/engineering/tools.md
docs/engineering/site.md
```

Rationale:

- One global README avoids conflicting local entry points.
- ALLCAPS index documents make documentation navigation predictable.
- Agents can be instructed to read stable named files.
- Folder-local README files often become stale duplicates.

---

# 2. Index document convention

Every documentation folder may have exactly one index document.

The index document is:

```text
docs/<FOLDER>.md
```

where `<FOLDER>` is the folder name written in uppercase.

Examples:

```text
docs/ARCHITECTURE.md indexes docs/architecture/
docs/DECISIONS.md indexes docs/decisions/
docs/SPECS.md indexes docs/specs/
docs/MILESTONES.md indexes docs/milestones/
docs/TBPS.md indexes docs/tbps/
docs/WORKFLOWS.md indexes docs/workflows/
docs/GUARDRAILS.md indexes docs/guardrails/
docs/ENGINEERING.md indexes docs/engineering/
docs/RESEARCH.md indexes docs/research/
```

## Index document responsibilities

An index document must:

- state the purpose of the indexed folder;
- define what belongs in the folder;
- define what does not belong in the folder;
- list available documents;
- define authority for that documentation area;
- reference relevant TBPs, specs, workflows, guardrails, or engineering documents.

An index document must not duplicate the contents of the documents it indexes.

## Folder rule

Folders under `docs/` must not contain local `README.md` or other competing meta documents.

Use the ALLCAPS index document only.

---

# 3. Documentation authority

Every durable document should declare what it is authoritative for.

Use this section when the document defines rules, behavior, process, or structure.

```md
# Authority

This document is authoritative for:
- <area>
- <constraint>
- <naming>
- <behavior>

This document is not authoritative for:
- <excluded area>
```

Example:

```md
# Authority

This document is authoritative for:
- short-running and long-running test classification
- default agent test execution limits
- test validation expectations

This document is not authoritative for:
- product feature behavior
- application architecture
- package versions
```

Authority sections prevent accidental semantic drift and make it clear which document wins when two documents seem to overlap.

---

# 4. Document contracts

Documents that are part of a synchronization chain should define a document contract.

```md
# Document Contract

## Related Documents

- docs/TERMINOLOGY.md
- docs/TBPS.md
- docs/GUARDRAILS.md

## Must Be Updated Together

When this document changes, review and update:
- <related document>
- <related workflow>
- <related TBP>
```

Examples:

```md
# Document Contract

## Related Documents

- docs/GUARDRAILS.md
- docs/WORKFLOWS.md
- docs/engineering/dotnet.md

## Must Be Updated Together

When testing rules change, review and update:
- docs/guardrails/testing.md
- docs/workflows/test-short.md
- docs/workflows/test-long.md
- docs/engineering/dotnet.md
- AGENTS.md
- .github/copilot-instructions.md
```

Document contracts are especially important for:

- workflow specs and GitHub workflow YAML;
- testing guardrails and engineering commands;
- public API documentation rules and language guardrails;
- milestone lifecycle TBPs and issue templates;
- engineering command contracts and agent instructions.

---

# 5. Core documents

## `README.md`

Purpose: human-facing entry point.

Minimum content:

```md
# Project Name

## Goal

Short project summary.

## Documentation Entry Points

- docs/TERMINOLOGY.md
- docs/ARCHITECTURE.md
- docs/SPECS.md
- docs/MILESTONES.md
- docs/TBPS.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/WORKFLOWS.md

## Engineering Commands

See docs/ENGINEERING.md.

## Development

Basic setup instructions.
```

The root README may include quick-start commands, but it must not become the full engineering guide.

## `AGENTS.md`

Purpose: concise AI-agent entry point.

Template:

```md
# Agent Instructions

## Required Reading

Before non-trivial work, read:

1. docs/TERMINOLOGY.md
2. docs/GUARDRAILS.md
3. docs/TBPS.md
4. docs/SPECS.md
5. docs/ENGINEERING.md
6. Relevant architecture, specs, decisions, milestones, workflows, TBPs, guardrails, and engineering documents

## Repository Rules

- Use canonical terminology.
- Do not introduce new terminology without updating docs/TERMINOLOGY.md.
- Follow docs/guardrails/testing.md before creating or running tests.
- Follow docs/guardrails/implementation.md before implementation.
- Follow language-specific guardrails when applicable.
- Follow docs/ENGINEERING.md for command contracts.
- Do not invent build, test, format, benchmark, packaging, or release commands.
- Do not define feature behavior in TBPs.
- Do not define permanent behavior in milestones.
- Keep specs authoritative for behavior.
- Keep workflow specs synchronized with workflow implementations.
- Keep engineering command contracts synchronized with scripts.

## Validation

Use the minimal relevant validation from docs/ENGINEERING.md and docs/guardrails/testing.md.

Do not run long-running tests unless explicitly requested.
```

## `.github/copilot-instructions.md`

Purpose: concise GitHub Copilot routing file.

Template:

```md
# GitHub Copilot Instructions

Read:

- AGENTS.md
- docs/TERMINOLOGY.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/TBPS.md
- relevant specs
- relevant architecture documents
- relevant language guardrails

Rules:

- Keep changes scoped.
- Prefer documented behavior over inferred behavior.
- Validate against specs.
- Use canonical engineering commands from docs/ENGINEERING.md.
- Use short-running tests by default.
- Do not run long-running tests unless explicitly requested.
- Document public API intent, not implementation mechanics.
```

Path-specific Copilot instructions may live under:

```text
.github/instructions/
```

These files must remain concise and must not duplicate full guide content.

---

# 6. Terminology

## `docs/TERMINOLOGY.md`

Purpose: canonical vocabulary.

Template:

```md
# Terminology

## Rules

- One sentence per term.
- One canonical meaning per term.
- Avoid aliases unless explicitly declared.
- Add new domain terms before broad use.
- Use terminology consistently across documentation, issues, and code.

## Terms

### Task Best Practice
Reusable operational guidance for a class of repository work.

### Specification
Authoritative behavioral description of a system, component, feature, or process.

### Milestone
Controlled implementation phase with explicit scope, deliverables, and exit criteria.

### Guardrail
Project-wide constraint that limits implementation, testing, documentation, or operational behavior.

### Engineering Guide
Stack-specific definition of repository commands, tooling, validation, and optional engineering modules.

### Command Contract
Stable set of repository commands used by humans, CI, and agents.

### Short-Running Test
Test intended to be safe for local development and agent execution.

### Long-Running Test
Test intended only for explicit local execution or GitHub workflow execution.

### Document Authority
Declaration of what a document is allowed to define.

### Document Contract
Declaration of related documents and synchronization obligations.
```

---

# 7. Guardrails

## `docs/GUARDRAILS.md`

Purpose: index project-wide constraints.

Template:

```md
# Guardrails

## Purpose

Guardrails define project-wide constraints that apply across tasks.

Guardrails are not specs.
Guardrails are not TBPs.
Guardrails constrain how implementation, testing, and documentation are performed.

## Available Guardrails

| Guardrail | Purpose |
|---|---|
| guardrails/testing.md | Test classification and execution rules |
| guardrails/implementation.md | General implementation rules |
| guardrails/languages/dotnet.md | .NET-specific rules |
| guardrails/languages/typescript.md | TypeScript-specific rules |

## Rules

- General guardrails apply to all work.
- Language guardrails apply when touching that language or runtime.
- More specific guardrails may refine general guardrails.
- More specific guardrails must not silently contradict general guardrails.

## Related Engineering Documents

- docs/ENGINEERING.md
- docs/engineering/dotnet.md
```

## `docs/guardrails/testing.md`

Purpose: prevent uncontrolled test generation and expensive test execution by AI agents.

```md
# Testing Guardrails

# Goal

Keep tests useful, deterministic, and safe to run during local and agent-assisted development.

# Test Categories

## Short-Running Tests

Short-running tests are safe for:
- local development
- AI agent validation
- pre-commit validation
- pull request validation

Short-running tests should:
- complete quickly;
- avoid external dependencies unless explicitly isolated;
- avoid sleeps and timing assumptions;
- avoid large datasets;
- avoid network dependencies;
- avoid full-system benchmark behavior.

## Long-Running Tests

Long-running tests are reserved for:
- explicit developer execution;
- scheduled GitHub workflows;
- release validation;
- benchmark validation;
- expensive integration scenarios.

Long-running tests include:
- benchmarks;
- large dataset tests;
- stress tests;
- endurance tests;
- full browser suites;
- full database matrix tests;
- cross-platform matrix tests;
- performance regression tests.

# Agent Rules

AI agents must:
- create short-running tests by default;
- run only the minimal relevant short-running test set;
- not run long-running tests unless explicitly instructed;
- not create expensive tests without marking them as long-running;
- not use arbitrary sleeps to stabilize tests;
- not expand test scope beyond the task;
- prefer deterministic test synchronization over timing delays.

# Minimal Validation Rule

For implementation work, the agent should run the smallest relevant validation set that can catch local regressions.

# Authority

This document is authoritative for:
- test classification;
- agent test execution limits;
- default validation expectations.

# Document Contract

When test execution policy changes, review and update:
- AGENTS.md
- .github/copilot-instructions.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/engineering/dotnet.md
- docs/workflows/test-short.md
- docs/workflows/test-long.md
```

## `docs/guardrails/implementation.md`

Purpose: language-independent implementation constraints.

```md
# Implementation Guardrails

# Goal

Keep implementation work scoped, readable, maintainable, and aligned with repository documentation.

# General Rules

- Prefer documented behavior over inferred behavior.
- Keep changes scoped to the task.
- Avoid opportunistic refactoring.
- Avoid introducing new abstractions without documented need.
- Prefer simple explicit control flow over cleverness.
- Preserve existing public contracts unless the spec changes.
- Do not silently change behavior.
- Do not introduce terminology that is absent from docs/TERMINOLOGY.md.
- Update specs when behavior changes.
- Update architecture documents when structure changes.
- Update decisions when meaningful rationale is introduced.

# Public API Documentation

Public API surface must be documented.

Documentation should describe intent, contract, and usage constraints.

Documentation should not merely restate what the code does.

Good:

```text
Creates a stable projection identifier that can be reused across incremental updates.
```

Bad:

```text
Returns a string created from the input value.
```

# Comments

Use comments for:
- intent;
- invariants;
- non-obvious constraints;
- external contracts;
- safety assumptions.

Avoid comments that:
- narrate obvious code;
- repeat names;
- explain syntax;
- compensate for unclear structure.

# Validation

Implementation is valid when:
- relevant specs remain satisfied;
- relevant guardrails are followed;
- minimal relevant tests pass;
- public API documentation reflects intent;
- documentation synchronization obligations are satisfied.

# Authority

This document is authoritative for:
- general implementation constraints;
- public API documentation rules;
- code comment intent rules.

# Document Contract

When implementation rules change, review and update:
- docs/GUARDRAILS.md
- AGENTS.md
- .github/copilot-instructions.md
- relevant language guardrails
```

---

# 8. Engineering

## `docs/ENGINEERING.md`

Purpose: index concrete engineering substrate.

Template:

```md
# Engineering

## Purpose

Engineering documents define repository commands, toolchain setup, validation commands, optional modules, and stack-specific setup rules.

Engineering documents are authoritative for:
- command contracts;
- build, restore, test, format, benchmark, package, and release commands;
- toolchain setup;
- stack-specific building blocks;
- optional engineering modules.

Engineering documents are not authoritative for:
- domain behavior;
- application architecture;
- milestone scope;
- long-term product semantics.

## Available Engineering Documents

| Document | Purpose |
|---|---|
| engineering/dotnet.md | Opinionated .NET 10 + MTP + TUnit + BenchmarkDotNet + Bun/Biome engineering profile |
| engineering/command-contract.md | Canonical repository command contract |
| engineering/building-blocks.md | Building block summary and selection rules |
| engineering/optional-modules.md | Optional engineering modules |

## Rules

- Humans, agents, and CI must use canonical engineering commands.
- CI must call engineering scripts instead of duplicating logic.
- Optional modules are absent by default.
- Tooling must be pinned or explicit.
- Command changes must update this index and affected engineering documents.

## Related Documents

- docs/GUARDRAILS.md
- docs/guardrails/testing.md
- docs/guardrails/implementation.md
- docs/WORKFLOWS.md
```

The concrete content for `docs/engineering/dotnet.md` is defined in **Engineering Guide V3**.

---

# 9. Specs

## `docs/SPECS.md`

Purpose: spec index and rules.

```md
# Specifications

## Purpose

Specs define behavioral truth.

Specs are authoritative for:
- behavior;
- invariants;
- contracts;
- inputs and outputs;
- failure semantics;
- validation expectations.

## Rules

- Specs must use canonical terminology.
- Specs must define invariants explicitly.
- Specs must avoid implementation plans.
- Specs should exist before implementation whenever practical.

## Available Specs

| Spec | Purpose |
|---|---|
| specs/example.md | Example specification structure |
```

## Spec template

```md
# <Spec Name>

# Goal

# Scope

# Non-Goals

# Terminology

# Invariants

# Behavioral Rules

# Inputs

# Outputs

# Failure Semantics

# Validation

# Related Architecture

# Related Decisions

# Authority

This document is authoritative for:
- <behavior>
- <invariants>
- <contracts>

# Document Contract

## Related Documents

- docs/TERMINOLOGY.md

## Must Be Updated Together

When this spec changes, review:
- related tests
- related architecture
- related decisions
- related milestones
```

---

# 10. Milestones

## `docs/MILESTONES.md`

Purpose: milestone index.

```md
# Milestones

## Purpose

Milestones sequence implementation work.

Milestones define:
- scope;
- deliverables;
- dependencies;
- risks;
- exit criteria.

Milestones do not define permanent behavioral truth.

## Available Milestones

| Milestone | Status | Purpose |
|---|---|---|
| milestones/example.md | Draft | Example milestone structure |
```

## Milestone template

```md
# <Milestone Name>

# Goal

# Scope

# Non-Goals

# Required Specs

# Required Decisions

# Deliverables

# Risks

# Execution Notes

# Exit Criteria

# Authority

This document is authoritative for:
- milestone scope
- milestone deliverables
- milestone exit criteria

This document is not authoritative for:
- permanent feature behavior
- architecture decisions

# Document Contract

## Related Documents

- docs
