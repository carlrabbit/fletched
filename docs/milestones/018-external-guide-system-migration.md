# Milestone 18: External Guide-System Migration

## Goal

Migrate the Fletched repository from the old repository-local copied guide model to the external guide-system model.

After this milestone, Fletched must contain only localized project truth and machine-readable guide-selection metadata. Copied setup, engineering, meta, profile, migration, or template guides from the external guide system must not be present in the target repository as operational authority.

The external guide repository is planning-time authority only:

```text
carlrabbit/agentic-project-guides
```

Implementation agents working in Fletched must use the localized Fletched documentation listed in this milestone and must not be required to read the external guide repository.

## Repository Role and Maturity Assumptions

### Repository role

```text
mixed-dogfood
```

Fletched is primarily a reusable capability provider because it builds a typed relational runtime, DSL, source generator, analyzer/generator pipeline, packages, and validation infrastructure. It also contains bounded consumer-style samples, so the repository role is `mixed-dogfood` rather than pure `capability-provider`.

Provider boundary:

```text
Fletched must validate that its runtime, DSL, source generator, packages, and samples work.
Fletched must not structure itself as if it were primarily an application consuming another capability.
Dogfood samples remain bounded under samples/ and tests.
```

### Maturity stage

```text
public-preview
```

The repository already has public package concerns, public API baselines, public docs, package smoke testing, and release-readiness commands, but it is still before stable `1.0.0` compatibility.

### Applicable guide profile

No `.guide-profile.json` was present at planning time.

Provisional inferred profile:

```text
base
dotnet-library
source-generator
public-package
mixed-dogfood
```

Only `.guide-profile.json` records this guide-system traceability. Repository-local operational docs must not require implementation agents to read the external guide repository.

## Execution Mode

```text
engineering-migration
```

Implementation autonomy:

```text
ai-executed-human-reviewed
```

This is a repository-authority migration. The implementation can be AI-executed because the required end state is explicit, but human review is required for authority/routing changes before completion.

## Task Mode

```text
engineering migration
```

This is not normal feature implementation, not release readiness, and not broad documentation synchronization.

## Scope

Apply a focused migration from copied guide authority to external guide-system traceability.

The implementation must:

```text
- add `.guide-profile.json` if it is not already present;
- remove copied setup/engineering guide documents from the target repository;
- remove operational references to copied guide documents from Fletched docs;
- ensure implementation agents are routed only to project-local authority documents;
- preserve Fletched project truth in localized documents;
- classify migration changes as required, conditional, deprecated, manual-review, or no-op;
- update milestone navigation for this milestone;
- validate the repository with concrete local commands.
```

## Non-Goals

Do not implement:

```text
- source changes;
- test changes;
- generated code changes;
- build-script changes unless validation reveals an existing guide-hygiene command already requires metadata updates;
- workflow YAML changes;
- broad public documentation polish;
- release notes;
- issue templates;
- TBPs;
- copied external guide documents;
- local copies of setup guides;
- local copies of engineering guides;
- local copies of meta guides;
- local copies of profile guides;
- local copies of migration guides;
- local copies of templates from `carlrabbit/agentic-project-guides`.
```

Do not convert this milestone into a general documentation synchronization pass.

## Required Authority Documents for Implementation

Implementation agents must read only these localized Fletched documents:

```text
README.md
AGENTS.md
.github/copilot-instructions.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/MILESTONES.md
docs/RESEARCH.md
docs/PUBLIC-DOCS.md
docs/GUARDRAILS.md
docs/agent-context/project-context.md
docs/milestones/018-external-guide-system-migration.md
.guide-profile.json
```

If `.guide-profile.json` does not yet exist before implementation, create it from the milestone package before proceeding.

The implementation agent must not read the external guide repository.

## External Guide-System Planning Inputs

The following external-guide concepts were already applied during planning and are embedded into this milestone:

```text
- repositories contain project truth only;
- copied guides are not operational repository documentation;
- `.guide-profile.json` records guide-system traceability;
- repository roles distinguish provider, consumer, and mixed dogfood repositories;
- maturity stage controls validation/release obligations;
- migrations classify changes as required, conditional, deprecated, manual-review, or no-op;
- validation is tiered;
- implementation agents use localized docs only.
```

These planning inputs are included here so that the later implementation agent does not need to read `carlrabbit/agentic-project-guides`.

## Focus Areas

### Focus Area 1 — Guide profile metadata

Add or update:

```text
.guide-profile.json
```

Required properties:

```text
guideSystem
guideRepository
guideVersion
repositoryRole
maturityStage
profiles
taskModes
executionModes
activeDocumentationLayers
deferredDocumentationLayers
inactiveDocumentationLayers
notes
```

Required semantic content:

```text
guideRepository = carlrabbit/agentic-project-guides
repositoryRole = mixed-dogfood
maturityStage = public-preview
profiles includes base, dotnet-library, source-generator, public-package, mixed-dogfood
engineering-migration is supported
release-readiness is supported for release work only
implementation agents must use localized docs only
external guide docs are planning-time authority, not implementation-time authority
```

The file is project metadata. It must not link ordinary implementation agents to external guide documents as required reading.

### Focus Area 2 — Remove copied guide documents

Remove repository-local copied guide documents from operational documentation.

Required deprecated files:

```text
docs/research/project-setup-guide-v5.md
docs/research/engineering-guide-v4.md
```

Also remove any older copied guide variants if present:

```text
docs/research/project-setup-guide-v1.md
docs/research/project-setup-guide-v2.md
docs/research/project-setup-guide-v3.md
docs/research/project-setup-guide-v4.md
docs/research/engineering-guide-v1.md
docs/research/engineering-guide-v2.md
docs/research/engineering-guide-v3.md
```

Required rule:

```text
No setup guide, engineering guide, meta guide, profile guide, migration guide, or template copied from `carlrabbit/agentic-project-guides` may remain as operational documentation in the target repository.
```

If a copied guide contains project-specific truth that is not present elsewhere, migrate only the project-specific truth into the appropriate Fletched-local authority document before deleting the copied guide.

### Focus Area 3 — Route agents to localized authority only

Update repository routing documents so agents read project-local authority documents only.

Likely affected files:

```text
AGENTS.md
.github/copilot-instructions.md
README.md
docs/RESEARCH.md
```

Required routing rule:

```text
Implementation agents must not be instructed to read copied guide documents under docs/research/.
Implementation agents must not be instructed to read `carlrabbit/agentic-project-guides`.
Implementation agents must be routed to localized Fletched authority documents.
```

`AGENTS.md` and `.github/copilot-instructions.md` must remove:

```text
docs/research/project-setup-guide-v5.md
docs/research/engineering-guide-v4.md
```

as read-first or primary operational documents.

They may mention `.guide-profile.json` only as metadata for planning/documentation-sync agents, not as ordinary implementation authority.

### Focus Area 4 — Research index cleanup

Update:

```text
docs/RESEARCH.md
```

Required classification:

```text
Deprecated:
  copied setup/engineering guide documents

Required:
  remove copied-guide entries from the active research index
  state that copied external guide documents are not stored in this repository
  keep research index limited to actual Fletched exploratory/project-truth research
```

If no active research documents remain, `docs/RESEARCH.md` may state that there are currently no active research documents.

Do not delete `docs/RESEARCH.md` unless existing repository policy explicitly allows it.

### Focus Area 5 — Milestone navigation

Update:

```text
docs/MILESTONES.md
```

Required change:

```text
Add this milestone to the milestone index.
```

Do not perform broad milestone renumbering or historical milestone cleanup.

### Focus Area 6 — Command and validation alignment

Use existing command contracts.

Known commands:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify-no-changes
./eng/check.sh
./eng/public-docs.sh [version]
./eng/release-check.sh <version>
```

For this milestone, do not require release validation.

Required validation tier:

```text
Tier 2 — standard local validation
Tier 5 — human review validation
```

Tier 1 focused checks are also required before Tier 2.

### Focus Area 7 — Manual authority review

Human review is required for:

```text
- removal of copied guide documents;
- AGENTS.md routing changes;
- `.github/copilot-instructions.md` routing changes;
- `.guide-profile.json` profile and maturity selection;
- confirmation that no Fletched project truth was lost when copied guides were removed.
```

## Migration Change Classification

### Required

```text
- Add `.guide-profile.json`.
- Remove operational read-first references to copied guide documents.
- Remove copied guide documents from `docs/research/`.
- Update `docs/RESEARCH.md`.
- Update `docs/MILESTONES.md`.
- Ensure implementation agents use localized Fletched docs only.
- Run Tier 1 and Tier 2 validation.
- Obtain Tier 5 human review for authority changes.
```

### Conditional

```text
- Update `README.md` only if it references copied guides or presents `docs/research/` guide copies as current operational authority.
- Update `docs/ENGINEERING.md` only if it references copied guides or external guide documents as operational authority.
- Update `docs/PUBLIC-DOCS.md` only if it references copied guides or external guide documents as public authority.
- Migrate project-specific facts from copied guides only if those facts are not already represented in localized Fletched docs.
```

### Deprecated

```text
- Repository-local copied setup guides.
- Repository-local copied engineering guides.
- Repository-local copied meta/profile/migration/template guides.
- Instructions telling implementation agents to read copied guides.
- Instructions telling ordinary implementation agents to read the external guide repository.
```

### Manual Review

```text
- Final `.guide-profile.json` role, maturity, and selected profiles.
- Whether any removed copied guide content contained project truth that should be localized.
- Whether AGENTS/Copilot routing is sufficiently specific for implementation agents.
- Whether the milestone package was added without smuggling in broad documentation sync.
```

### No-Op

```text
- Source code behavior.
- Runtime behavior.
- Source generator behavior.
- NuGet package behavior.
- Public API behavior.
- Release workflow behavior unless existing release hygiene checks already fail because of copied guide references.
```

## Implementation Constraints

```text
- Do not copy external guide documents into this repository.
- Do not link external guide documents as required implementation authority.
- Do not make ordinary implementation agents read `carlrabbit/agentic-project-guides`.
- Do not remove localized Fletched project truth.
- Do not convert exploratory guide text into authoritative behavior unless it is Fletched-specific and belongs in an existing local authority document.
- Do not perform broad documentation synchronization beyond the files required by this migration.
- Do not add TBPs.
- Do not add issue templates.
- Do not update workflows unless validation exposes a direct blocker.
- Do not require release validation.
```

## Files or Areas Likely Affected

Expected files:

```text
.guide-profile.json
AGENTS.md
.github/copilot-instructions.md
docs/RESEARCH.md
docs/MILESTONES.md
docs/milestones/018-external-guide-system-migration.md
```

Expected deletions:

```text
docs/research/project-setup-guide-v5.md
docs/research/engineering-guide-v4.md
```

Possible deletions if present:

```text
docs/research/project-setup-guide-v1.md
docs/research/project-setup-guide-v2.md
docs/research/project-setup-guide-v3.md
docs/research/project-setup-guide-v4.md
docs/research/engineering-guide-v1.md
docs/research/engineering-guide-v2.md
docs/research/engineering-guide-v3.md
```

Conditional files:

```text
README.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/TBPS.md
docs/WORKFLOWS.md
```

Only edit conditional files when they directly reference copied guide documents or present external guide-system material as operational authority.

## Validation Tiers and Commands

### Tier 0 — edit sanity

Required checks:

```text
- package contains only repository-relative Markdown and metadata files;
- no copied external guide documents were added;
- no implementation source files were added by this migration package;
- `.guide-profile.json` parses as JSON;
- milestone document is under `docs/milestones/`;
- no README.md files are added outside repository root.
```

Concrete commands:

```text
python -m json.tool .guide-profile.json >/dev/null
find . -path './.git' -prune -o -name 'README.md' -print
```

The `find` command must show only:

```text
./README.md
```

unless the repository has an explicitly documented exception.

### Tier 1 — focused validation

Required focused checks:

```text
- no active docs route agents to copied setup/engineering guides;
- no copied guide files remain under docs/research/;
- research index no longer lists copied guide documents as current;
- milestone index includes this milestone;
- external guide repository is not required reading for implementation agents.
```

Concrete commands:

```text
grep -R "project-setup-guide" README.md AGENTS.md .github docs -n || true
grep -R "engineering-guide-v" README.md AGENTS.md .github docs -n || true
find docs/research -maxdepth 1 -type f \( -name '*setup-guide*' -o -name '*engineering-guide*' \) -print
grep -n "018-external-guide-system-migration" docs/MILESTONES.md
```

Expected focused results:

```text
No operational read-first reference to copied setup/engineering guides remains.
No copied setup/engineering guide file remains under docs/research/.
This milestone appears in docs/MILESTONES.md.
```

References to this milestone's historical migration rationale are allowed only inside:

```text
docs/milestones/018-external-guide-system-migration.md
```

### Tier 2 — standard local validation

Run the canonical local completion gate:

```text
./eng/check.sh
```

If `./eng/check.sh` fails for reasons unrelated to this migration, record the failure and isolate whether the migration introduced it.

### Tier 3 — PR integration validation

Required after opening a PR:

```text
Repository CI / PR checks
```

Do not require local reproduction of every PR integration workflow unless CI fails.

### Tier 4 — release validation

Not required.

Do not run:

```text
./eng/release-check.sh <version>
```

for this milestone unless a maintainer explicitly promotes the migration to release-readiness work.

### Tier 5 — human review validation

Required.

Human reviewer must confirm:

```text
- copied guide authority was removed;
- localized project truth remains intact;
- guide profile metadata is acceptable;
- agent routing is clear;
- no broad documentation sync was smuggled into the migration.
```

## Acceptance Criteria

```text
- `.guide-profile.json` exists.
- `.guide-profile.json` identifies `carlrabbit/agentic-project-guides` as the guide repository.
- `.guide-profile.json` records repository role as `mixed-dogfood`.
- `.guide-profile.json` records maturity stage as `public-preview`.
- `.guide-profile.json` records applicable profiles.
- Ordinary implementation agents are not required to read the external guide repository.
- `AGENTS.md` no longer lists copied setup/engineering guide documents as read-first operational authority.
- `.github/copilot-instructions.md` no longer lists copied setup/engineering guide documents as primary operational authority.
- `docs/RESEARCH.md` no longer lists copied setup/engineering guide documents as current active research authority.
- Copied setup guide documents are removed from `docs/research/`.
- Copied engineering guide documents are removed from `docs/research/`.
- No external guide documents are copied into the target repository.
- Any Fletched-specific project truth from removed copied guides is preserved in localized Fletched authority documents if needed.
- `docs/MILESTONES.md` indexes this milestone.
- Tier 0 edit sanity passes.
- Tier 1 focused validation passes.
- Tier 2 `./eng/check.sh` passes or any failure is proven unrelated to this migration.
- Tier 5 human review is complete.
```

## Direct Documentation Impact

Required direct documentation changes:

```text
.guide-profile.json
AGENTS.md
.github/copilot-instructions.md
docs/RESEARCH.md
docs/MILESTONES.md
docs/milestones/018-external-guide-system-migration.md
```

Conditional direct documentation changes:

```text
README.md
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/TBPS.md
docs/WORKFLOWS.md
```

Only update conditional files if they directly contain copied-guide authority, copied-guide routing, or stale guide-system references.

## Deferred Documentation Synchronization Hints

Defer broad cleanup to a separate documentation-sync milestone or PR.

Deferred work may include:

```text
- normalizing all document contract sections;
- replacing older TBP references if the repository later decides to retire local TBPs;
- broad README user-flow polish;
- cross-reference normalization across all docs;
- public docs polish;
- release notes;
- issue template modernization;
- workflow documentation cleanup unrelated to guide authority.
```

These are not part of this milestone unless directly required to remove copied guide authority.

## Human Review Requirements

Human review is mandatory before completion.

Reviewer checklist:

```text
- The final repository does not contain copied guide documents as operational authority.
- `.guide-profile.json` accurately reflects Fletched.
- The repository remains usable by an implementation agent reading only localized docs.
- No project-specific Fletched truth was lost.
- The migration did not become broad documentation synchronization.
```

## Out-of-Scope Guide Migration Work

Out of scope:

```text
- importing external guide templates;
- recreating external guide docs locally;
- replacing all existing Fletched docs with template-derived docs;
- changing source layout;
- changing package layout;
- changing validation scripts unless a direct guide-authority blocker exists;
- modifying CI workflows;
- removing all docs/research content merely because it is research;
- removing local TBPs merely because the external guide system has templates;
- release readiness.
```

## Implementation Sequence

```text
1. Add `.guide-profile.json` from the milestone package.
2. Add this milestone document under `docs/milestones/`.
3. Inspect AGENTS.md, .github/copilot-instructions.md, README.md, docs/RESEARCH.md, docs/ENGINEERING.md, docs/PUBLIC-DOCS.md, and docs/MILESTONES.md for copied-guide authority.
4. Remove read-first and primary-doc references to copied setup/engineering guides.
5. Delete copied setup/engineering guide files from docs/research/.
6. Preserve any Fletched-specific project truth from removed guides in localized authority docs only if missing elsewhere.
7. Update docs/RESEARCH.md.
8. Update docs/MILESTONES.md.
9. Run Tier 0 validation.
10. Run Tier 1 focused validation.
11. Run ./eng/check.sh.
12. Request human review for authority migration.
```

## Completion Rule

This milestone is complete only when Fletched has migrated away from repository-local copied guide authority, `.guide-profile.json` records external guide-system traceability, implementation agents are routed only to localized Fletched project truth, copied guide documents are removed, focused and standard validation pass, and human review confirms that the repository is ready for future implementation work under the external guide-system model.
