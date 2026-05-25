# Project Setup Guide V5

## Status

Authoritative project-setup guide.

## Purpose

This guide defines how a repository is structured as a documentation-first, AI-assisted engineering system.

Version 5 extends Version 4 with a first-class public documentation layer for repositories that publish NuGet packages, public APIs, source generators, command-line tools, websites, or other externally consumed artifacts.

The guide remains stack-independent. It defines the repository knowledge model, documentation conventions, public documentation model, index rules, task best practices, specifications, milestones, workflows, guardrails, issue templates, and agent routing.

For concrete build, test, formatting, benchmark, package, release, .NET, Bun, Biome, Blazor, Playwright, samples, public documentation validation, and GitHub Pages setup, use:

- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`

## Core model

The repository separates the following responsibilities.

| Layer | Responsibility |
|---|---|
| `README.md` | First-contact public and contributor entry point. |
| `docs/TERMINOLOGY.md` | Canonical vocabulary. |
| `docs/ARCHITECTURE.md` | Index for structural system design. |
| `docs/DECISIONS.md` | Index for decision records and rationale. |
| `docs/SPECS.md` | Index for behavioral truth and invariants. |
| `docs/MILESTONES.md` | Index for controlled implementation phases. |
| `docs/TBPS.md` | Index for reusable operational methodology. |
| `docs/WORKFLOWS.md` | Index for operational workflow specifications. |
| `docs/GUARDRAILS.md` | Index for cross-cutting implementation and testing constraints. |
| `docs/ENGINEERING.md` | Index for concrete engineering substrate and stack profiles. |
| `docs/PUBLIC-DOCS.md` | Index and synchronization contract for public user-facing documentation. |
| `docs/RESEARCH.md` | Index for non-authoritative research and rationale. |
| `public-docs/` | Source for externally consumable documentation. |
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
Public docs explain supported usage to consumers.
Issues define concrete work.
Workflows define operations.
```

## Internal documentation vs public documentation

The repository has two documentation bases.

```text
docs/
  internal authoritative engineering and semantic documentation

public-docs/
  external consumer-facing documentation source
```

`docs/` is the internal knowledge system. It contains specs, architecture, decisions, TBPs, milestones, guardrails, engineering instructions, workflow specs, and research.

`public-docs/` is the public documentation source. It contains user-facing material such as installation guides, getting-started guides, package docs, diagnostics references, API documentation, sample walkthroughs, versioning policy, release notes, and website source content.

The public documentation layer is core to the repository. It is not a separate guide and not an afterthought. Public documentation must be kept synchronized with specs, public API, package metadata, diagnostics, samples, release behavior, and website publication.

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
│  ├─ PUBLIC-DOCS.md
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
│  └─ research/
│     ├─ project-setup-guide-v5.md
│     └─ engineering-guide-v4.md
│
├─ public-docs/
│  ├─ getting-started.md
│  ├─ installation.md
│  ├─ concepts.md
│  ├─ packages.md
│  ├─ samples.md
│  ├─ diagnostics.md
│  ├─ versioning.md
│  ├─ release-notes.md
│  ├─ guides/
│  ├─ api/
│  ├─ diagnostics/
│  ├─ nuget/
│  ├─ samples/
│  └─ website/
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

These folders are governed by `docs/ENGINEERING.md`, `docs/engineering/*`, and the relevant guardrails.

## README rule

Only the root-level repository `README.md` is allowed.

Do not create additional `README.md` files anywhere else in the repository.

This includes:

```text
docs/**/README.md
public-docs/**/README.md
eng/README.md
samples/README.md
tools/**/README.md
site/README.md
```

Use named Markdown documents instead:

```text
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/engineering/command-contract.md
public-docs/getting-started.md
public-docs/nuget/package-readme.md
public-docs/samples/getting-started.md
```

Rationale:

- one global README avoids conflicting local entry points;
- ALLCAPS index documents make internal documentation navigation predictable;
- `public-docs/` has its own explicit source structure;
- agents can be instructed to read stable named files;
- folder-local README files often become stale duplicates.

---

# 2. Index document convention

Every documentation folder under `docs/` may have exactly one index document.

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

`public-docs/` is different. It is a root-level public documentation source folder. Its internal governance is defined by:

```text
docs/PUBLIC-DOCS.md
```

Do not create:

```text
public-docs/README.md
```

## Index document responsibilities

An index document must:

- state the purpose of the indexed area;
- define what belongs in the area;
- define what does not belong in the area;
- list available documents;
- define authority for that documentation area;
- reference relevant TBPs, specs, workflows, guardrails, engineering documents, or public documentation surfaces.

An index document must not duplicate the contents of the documents it indexes.

---

# 3. Documentation authority

Every durable document should declare what it is authoritative for.

Use this section when the document defines rules, behavior, process, structure, or publication responsibility.

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
- public documentation structure
- public documentation synchronization rules
- public documentation publication surfaces

This document is not authoritative for:
- implementation behavior
- architecture decisions
- build command contracts
```

Authority sections prevent semantic drift and clarify which document wins when two documents overlap.

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

Document contracts are especially important for:

- workflow specs and GitHub workflow YAML;
- testing guardrails and engineering commands;
- public API documentation rules and language guardrails;
- package metadata and public NuGet documentation;
- diagnostics behavior and diagnostics reference pages;
- milestone lifecycle TBPs and issue templates;
- public documentation and release notes;
- engineering command contracts and agent instructions.

---

# 5. Root files

## `README.md`

Purpose: first-contact entry point for users and contributors.

The README must be user-first when the repository publishes a package, public API, CLI, source generator, website, or other externally consumed artifact.

Minimum content for public NuGet libraries:

```md
# Project Name

## Install

Package installation snippet.

## Quick Start

Minimal working example.

## Packages

Package split and installation rules.

## Core Concepts

Short user-facing concepts.

## Samples

Links to samples and public docs.

## Documentation

- public-docs/getting-started.md
- public-docs/installation.md
- public-docs/concepts.md
- public-docs/diagnostics.md

## Contributor Documentation

- docs/TERMINOLOGY.md
- docs/SPECS.md
- docs/TBPS.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
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
6. docs/PUBLIC-DOCS.md when public behavior, package contents, samples, diagnostics, public API, website, or release behavior changes
7. Relevant architecture, specs, decisions, milestones, workflows, TBPs, guardrails, engineering documents, and public documentation

## Repository Rules

- Use canonical terminology.
- Do not introduce new terminology without updating docs/TERMINOLOGY.md.
- Follow docs/guardrails/testing.md before creating or running tests.
- Follow docs/guardrails/implementation.md before implementation.
- Follow language-specific guardrails when applicable.
- Follow docs/ENGINEERING.md for command contracts.
- Follow docs/PUBLIC-DOCS.md when public documentation may be affected.
- Do not invent build, test, format, benchmark, packaging, release, or public documentation validation commands.
- Do not define feature behavior in TBPs.
- Do not define permanent behavior in milestones.
- Keep specs authoritative for behavior.
- Keep public docs synchronized with public behavior.
- Keep workflow specs synchronized with workflow implementations.
- Keep engineering command contracts synchronized with scripts.
- Do not create README files outside the repository root.

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
- docs/PUBLIC-DOCS.md when public-facing behavior changes
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
- Update public-docs/ when public behavior, packages, diagnostics, samples, or release behavior changes.
```

---

# 6. Terminology

## `docs/TERMINOLOGY.md`

Purpose: canonical vocabulary.

Add the following public-documentation terms when public docs are used:

```md
### Public Documentation
Documentation intended for external users, package consumers, API users, website readers, and release consumers.

### Consumer
A developer or system using the released package, API, tool, generated output, or website.

### Public Documentation Surface
A publication target or consumer-facing documentation form, such as README content, NuGet package README, website pages, API documentation, diagnostics reference, samples, or release notes.

### Package README
Markdown content included in a NuGet package and displayed on the package page.

### Diagnostics Reference
Public documentation for diagnostic IDs, severities, messages, causes, invalid examples, fixed examples, and stability expectations.

### Public API Baseline
Tracked representation of the supported public API surface used to detect accidental API changes.
```

---

# 7. Public documentation

## `docs/PUBLIC-DOCS.md`

Purpose: internal authority and synchronization contract for public documentation.

Template:

```md
# Public Documentation

## Purpose

This document indexes and governs public user-facing documentation.

Public documentation is documentation intended for package consumers, API users, website readers, and release consumers.

Public documentation lives under:

```text
public-docs/
```

## Authority

This document is authoritative for:
- public documentation structure
- public documentation synchronization rules
- public documentation publication surfaces
- public documentation ownership

This document is not authoritative for:
- implementation behavior
- architecture
- internal specifications
- task methodology
- build command contracts

## Public Documentation Surfaces

| Surface | Source |
|---|---|
| Root README user sections | README.md |
| Getting started docs | public-docs/getting-started.md |
| Installation docs | public-docs/installation.md |
| Package docs | public-docs/nuget/ |
| NuGet package README content | public-docs/nuget/package-readme.md |
| Public API documentation | public-docs/api/ |
| Diagnostics reference | public-docs/diagnostics/ |
| Samples documentation | public-docs/samples/ |
| Website source content | public-docs/website/ |
| Release notes | public-docs/release-notes.md |
| Versioning policy | public-docs/versioning.md |

## Rules

- Public documentation must be user-first.
- Public documentation must not expose internal implementation structure unless relevant to users.
- Public documentation must link to public concepts before internal specs.
- Public documentation must use canonical terminology from docs/TERMINOLOGY.md.
- Public documentation must be updated when specs, public API, diagnostics, package metadata, samples, or release behavior changes.
- Public documentation must not duplicate internal specs verbatim.
- Public documentation must explain supported usage, not internal rationale.

## Document Contract

### Related Documents

- docs/TERMINOLOGY.md
- docs/SPECS.md
- docs/ENGINEERING.md
- docs/GUARDRAILS.md
- docs/WORKFLOWS.md
- docs/tbps/public-documentation-update.md
- docs/tbps/documentation-review.md
- docs/tbps/release.md

### Must Be Updated Together

When public behavior changes, review and update:
- relevant specs
- README.md
- public-docs/getting-started.md
- public-docs/concepts.md
- public-docs/api/
- public-docs/samples/

When package metadata or package contents change, review and update:
- docs/engineering/packaging.md
- public-docs/nuget/
- public-docs/nuget/package-readme.md
- README.md
- public-docs/release-notes.md

When diagnostics change, review and update:
- relevant specs
- public-docs/diagnostics.md
- public-docs/diagnostics/
- tests that verify diagnostics
- public-docs/release-notes.md

When samples change, review and update:
- public-docs/samples.md
- public-docs/samples/
- docs/engineering/samples.md
- sample validation workflow

When release behavior changes, review and update:
- docs/workflows/release.md
- docs/engineering/packaging.md
- public-docs/versioning.md
- public-docs/release-notes.md
```

## Recommended public documentation structure for NuGet libraries

```text
public-docs/
├─ getting-started.md
├─ installation.md
├─ concepts.md
├─ packages.md
├─ samples.md
├─ diagnostics.md
├─ versioning.md
├─ release-notes.md
│
├─ guides/
│  ├─ first-query.md
│  ├─ facts-and-predicates.md
│  ├─ modules.md
│  ├─ indexes.md
│  ├─ recursion.md
│  ├─ negation.md
│  └─ metrics.md
│
├─ api/
│  ├─ public-api.md
│  ├─ compatibility.md
│  └─ xml-docs.md
│
├─ diagnostics/
│  ├─ FLTCH001.md
│  ├─ FLT2004.md
│  ├─ FLM3004.md
│  └─ FLL5003.md
│
├─ nuget/
│  ├─ Fletched.Core.md
│  ├─ Fletched.Roslyn.md
│  └─ package-readme.md
│
├─ samples/
│  ├─ getting-started.md
│  ├─ facts-and-predicates.md
│  ├─ modules.md
│  └─ work-assignment.md
│
└─ website/
   ├─ index.md
   ├─ navigation.md
   └─ publishing.md
```

## Diagnostics page template

```md
# <Diagnostic ID>

## Severity

<Error | Warning | Info>

## Message

Diagnostic message.

## Cause

Why this diagnostic occurs.

## Invalid Example

Example that triggers the diagnostic.

## Fixed Example

Corrected version.

## Related Docs

- public-docs/diagnostics.md
- relevant guide
```

## Package README source

Use:

```text
public-docs/nuget/package-readme.md
```

as the source for package README content, unless package-specific README files are required.

For multiple packages, use:

```text
public-docs/nuget/<PackageId>.md
```

and document the mapping in `docs/PUBLIC-DOCS.md`.

---

# 8. Guardrails

## `docs/GUARDRAILS.md`

Purpose: index project-wide constraints.

Include public documentation rules in the index:

```md
# Guardrails

## Available Guardrails

| Guardrail | Purpose |
|---|---|
| guardrails/testing.md | Test classification and execution rules |
| guardrails/implementation.md | General implementation rules |
| guardrails/languages/dotnet.md | .NET-specific rules |
| guardrails/languages/typescript.md | TypeScript-specific rules |

## Related Public Documentation

- docs/PUBLIC-DOCS.md
- public-docs/
```

## Public API documentation rule

Add to `docs/guardrails/implementation.md`:

```md
# Public API Documentation

Public APIs must document intent, contract, constraints, and failure behavior.

Public API documentation must not merely restate implementation mechanics.

When public API documentation changes, review:
- public-docs/api/
- public-docs/getting-started.md
- public-docs/nuget/
- public-docs/release-notes.md
```

---

# 9. Engineering

## `docs/ENGINEERING.md`

Purpose: index concrete engineering substrate.

Add public documentation validation to the engineering index:

```md
# Engineering

## Available Engineering Documents

| Document | Purpose |
|---|---|
| engineering/dotnet.md | Opinionated .NET engineering profile |
| engineering/command-contract.md | Canonical repository command contract |
| engineering/building-blocks.md | Building block summary and selection rules |
| engineering/packaging.md | NuGet packaging and publishing |
| engineering/public-documentation.md | Public documentation validation and publication |

## Public Documentation Commands

| Command | Purpose |
|---|---|
| ./eng/public-docs.sh | Validate public documentation consistency. |
| ./eng/release-check.sh <version> | Run release-oriented validation. |
```

Engineering Guide V4 defines BB19 Public Documentation and BB20 Release Readiness.

---

# 10. Specs

Specs remain internal behavioral authority.

Public documentation must not replace specs.

Rules:

- Specs define what must be true.
- Public docs explain how consumers use what is true.
- Public docs must be updated when externally visible spec-defined behavior changes.
- Specs may link to public docs, but public docs must not become internal implementation authority.

---

# 11. Milestones

Milestones that affect public release readiness must include public documentation impact.

Add to milestone template:

```md
# Public Documentation Impact

List affected public documentation surfaces:

- README.md
- public-docs/getting-started.md
- public-docs/installation.md
- public-docs/nuget/
- public-docs/api/
- public-docs/diagnostics/
- public-docs/samples/
- public-docs/release-notes.md
```

For version 1.0 milestones, public documentation is required, not optional.

---

# 12. TBPs

## `docs/TBPS.md`

Add:

```md
| tbps/public-documentation-update.md | Update public documentation after public behavior, package, API, diagnostics, sample, or release changes |
```

## `docs/tbps/public-documentation-update.md`

```md
# Public Documentation Update

# Goal

Keep user-facing documentation synchronized with repository behavior, packages, diagnostics, samples, public API, and release state.

# Constraints

- Public documentation must be user-first.
- Public documentation must not duplicate internal specs verbatim.
- Public documentation must use canonical terminology.
- Public documentation must reflect released or intentionally documented behavior.
- Public documentation must be updated before release when public behavior changes.

# Non-Goals

- Defining implementation behavior
- Recording architecture rationale
- Replacing specs
- Replacing engineering command contracts

# Required Reading

- docs/TERMINOLOGY.md
- docs/PUBLIC-DOCS.md
- docs/SPECS.md
- docs/ENGINEERING.md
- relevant specs
- relevant package docs
- relevant diagnostics docs
- relevant samples docs

# Process

1. Identify which public documentation surfaces are affected.
2. Identify whether behavior, API, diagnostics, samples, package metadata, or release behavior changed.
3. Update public docs using user-facing language.
4. Update README.md if first-contact user experience changed.
5. Update NuGet package README source if package usage changed.
6. Update diagnostics reference if diagnostics changed.
7. Update samples documentation if examples changed.
8. Update release notes if the change is externally visible.
9. Verify public docs do not contradict specs.
10. Verify public docs do not expose internal-only concepts as supported user API.

# Validation

- Public documentation matches implemented behavior.
- Public documentation uses canonical terminology.
- Related specs remain the internal authority.
- Package README content is current.
- Diagnostics reference is current.
- Samples build and match documented usage.
```

---

# 13. Workflows

Add public documentation and release-readiness workflow specs:

```text
docs/workflows/public-docs.md
docs/workflows/release-check.md
```

## `docs/workflows/public-docs.md`

```md
# Public Documentation Workflow

# Goal

Validate public documentation before release or public-facing changes.

# Constraints

- Must not require secrets.
- Must not publish by default.
- Must check links, required files, diagnostics pages, NuGet README source, and sample documentation references where practical.

# Inputs

- public-docs/
- README.md
- docs/PUBLIC-DOCS.md
- docs/TERMINOLOGY.md

# Outputs

- validation result

# Authority

This document is authoritative for:
- public documentation validation workflow intent

# Document Contract

When this workflow changes, review:
- docs/PUBLIC-DOCS.md
- docs/ENGINEERING.md
- eng/public-docs.sh
- .github/workflows/public-docs.yml
```

---

# 14. Issue templates

Use simple Markdown issue templates.

## `.github/ISSUE_TEMPLATE/documentation.md`

Add:

```md
# Documentation Improvement

## Required Reading

- docs/TERMINOLOGY.md
- docs/GUARDRAILS.md
- docs/PUBLIC-DOCS.md when public docs are affected
- docs/tbps/documentation-review.md
- docs/tbps/public-documentation-update.md when public docs are affected
- relevant index document

## Documentation Area

Which documents or folders are affected?

## Problem

Describe the documentation issue.

## Expected Improvement

Describe the intended improvement.

## Public Documentation Impact

Does this change affect public-docs/ or README.md?

- [ ] No
- [ ] Yes, update public-docs/
- [ ] Yes, update README.md
- [ ] Yes, update NuGet package README
- [ ] Yes, update diagnostics reference
- [ ] Yes, update samples documentation

## Synchronization

List documents that may need to be updated together.

## Terminology Impact

Does this introduce, change, or remove terminology?
```

## `.github/ISSUE_TEMPLATE/milestone-implementation.md`

Add:

```md
## Public Documentation Impact

List affected public documentation surfaces:

- README.md
- public-docs/getting-started.md
- public-docs/installation.md
- public-docs/nuget/
- public-docs/api/
- public-docs/diagnostics/
- public-docs/samples/
- public-docs/release-notes.md
```

## `.github/ISSUE_TEMPLATE/release.md`

Add:

```md
## Public Documentation Release Checklist

- [ ] README.md is user-first.
- [ ] NuGet package README content is current.
- [ ] Getting started docs are current.
- [ ] Public API docs are current.
- [ ] Diagnostics reference is current.
- [ ] Samples documentation is current.
- [ ] Versioning policy is current.
- [ ] Release notes are current.
```

---

# 15. Research

Store this guide as:

```text
docs/research/project-setup-guide-v5.md
```

Do not make the research copy authoritative. Extract its rules into actual index documents, TBPs, guardrails, public documentation docs, and engineering docs.

---

# 16. Upgrade guide from V4

## 1. Add public documentation index

Create:

```text
docs/PUBLIC-DOCS.md
```

Add it to:

```text
README.md
AGENTS.md
.github/copilot-instructions.md
docs/RESEARCH.md
```

## 2. Add public documentation source folder

Create:

```text
public-docs/
```

Do not create `public-docs/README.md`.

Start with:

```text
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/api/
public-docs/diagnostics/
public-docs/nuget/
public-docs/samples/
public-docs/website/
```

## 3. Update terminology

Add public documentation terms to `docs/TERMINOLOGY.md`.

## 4. Update README.md

Make the root README user-first if the repository publishes packages or public APIs.

Move contributor routing under:

```md
## Contributor Documentation
```

## 5. Add public documentation TBP

Create:

```text
docs/tbps/public-documentation-update.md
```

Update:

```text
docs/TBPS.md
```

## 6. Update guardrails

Update:

```text
docs/guardrails/implementation.md
docs/GUARDRAILS.md
```

Add public API documentation rules and public documentation synchronization references.

## 7. Update engineering guide

Upgrade Engineering Guide V3 to V4.

Add:

```text
BB19 Public Documentation
BB20 Release Readiness
eng/public-docs.sh
eng/package-smoke.sh
eng/public-api.sh
eng/release-check.sh
```

## 8. Update workflows

Create or update:

```text
docs/workflows/public-docs.md
docs/workflows/release-check.md
docs/workflows/release.md
```

## 9. Update issue templates

Add public documentation impact sections to:

```text
.github/ISSUE_TEMPLATE/documentation.md
.github/ISSUE_TEMPLATE/milestone-implementation.md
.github/ISSUE_TEMPLATE/release.md
```

## 10. Update release process

Release readiness must include:

```text
package smoke tests
public API validation
samples validation
public documentation validation
release notes validation
```

## 11. Store V4 as research

Store the previous setup guide at:

```text
docs/research/project-setup-guide-v4.md
```

---

# 17. Final V5 model

V5 explicitly separates:

```text
Project-internal authority
  docs/

Public consumer-facing source
  public-docs/

Publication mechanisms
  README.md
  NuGet package README
  generated API docs
  website
  release notes
```

The repository should now read as:

```text
README.md
  first-contact user and contributor entry point

docs/*.md
  authoritative internal knowledge indexes

docs/<folder>/
  internal documentation content, never README files

public-docs/
  public documentation source, never README files

docs/PUBLIC-DOCS.md
  public documentation authority and synchronization contract

AGENTS.md
  routes agents to authoritative documents

.github/copilot-instructions.md
  routes Copilot to authoritative documents

.github/ISSUE_TEMPLATE/*.md
  routes concrete work to TBPs, specs, milestones, guardrails, engineering commands, and public docs
```
