# Project Setup Guide V3

## Goal

Create a repository structure that acts as a:

```text
Semantic Engineering System
```

optimized for:

* humans
* AI agents
* long-lived maintainability
* operational clarity
* deterministic repository semantics

The repository should:

* minimize ambiguity
* minimize duplicated authority
* minimize process drift
* maximize semantic consistency

---

# Core Structural Principles

## 1. Precision over prose

Documentation should be:

* concise
* structured
* reference-oriented
* terminology-consistent
* low ambiguity

Avoid:

* conversational explanations
* duplicated semantics
* implementation leakage into specifications

---

## 2. Repository semantics over tool semantics

The repository should not depend conceptually on:

* Codex
* Copilot
* ChatGPT
* Claude
* prompt systems

All operational guidance should remain valid independently of tooling.

---

## 3. Explicit document authority

Every important document should explicitly define:

* what it is authoritative for
* what it is not authoritative for

---

## 4. Explicit synchronization contracts

Documents must define:

* related documents
* synchronization obligations
* update relationships

---

# Repository Structure

```text
/
├─ README.md
├─ AGENTS.md
├─ copilot-instructions.md
│
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ TBPS.md
│  ├─ SPECS.md
│  ├─ WORKFLOWS.md
│  ├─ TESTING.md
│  │
│  ├─ architecture/
│  ├─ decisions/
│  ├─ milestones/
│  ├─ research/
│  ├─ specs/
│  ├─ tbps/
│  └─ workflows/
│
└─ .github/
   ├─ ISSUE_TEMPLATE/
   └─ workflows/
```

---

# Folder and Index Rules

This is now fully standardized.

## Rule

Every important documentation folder:

```text
/docs/<folder>/
```

has exactly one index document:

```text
/docs/ALLCAPS_NAME.md
```

The index document:

* is named after the folder
* uses ALLCAPS
* exists outside the folder
* acts as the authoritative navigation/index document

---

# Examples

| Folder          | Index             |
| --------------- | ----------------- |
| docs/specs/     | docs/SPECS.md     |
| docs/tbps/      | docs/TBPS.md      |
| docs/workflows/ | docs/WORKFLOWS.md |

---

# Forbidden Patterns

## Forbidden

```text
docs/specs/README.md
docs/specs/SPECS.md
docs/SPECS/
```

## Forbidden

Simultaneously having:

```text
docs/TBPS.md
docs/tbps/README.md
```

---

# Folder Semantics

Folders contain:

* concrete documents
* implementations of the indexed concept

Index documents contain:

* navigation
* scope rules
* lifecycle rules
* structural rules
* categorization

---

# Important Consequence

This eliminates ambiguity between:

* folder metadata
* folder contents
* navigational authority
* implementation artifacts

This is a major improvement for both:

* humans
* AI agents

---

# TBP Model

## Purpose

TBPs define:

```text
Reusable operational methodology
```

They are:

* process-oriented
* reusable
* repository-specific
* implementation-aware
* not task-specific

---

# TBP Scope Rules

TBPs define:

* operational methodology
* synchronization expectations
* validation expectations
* repository process

TBPs do NOT define:

* concrete tasks
* feature semantics
* architecture decisions
* milestone sequencing

---

# TBP Granularity

Correct granularity:

```text
feature-implementation
bug-investigation
create-spec
create-milestone
finish-milestone
workflow-changes
documentation-review
```

Incorrect granularity:

```text
implement-node-cache
fix-render-bug
add-api-endpoint
```

Those belong in:

* specs
* milestones
* issues

---

# Foundational TBPs

Foundational TBPs are explicitly encouraged.

They capture:

* repository governance
* meta-structure lifecycle
* documentation lifecycle
* operational consistency

Examples:

```text
docs/tbps/
    add-tbp.md
    create-spec.md
    create-milestone.md
    start-milestone.md
    finish-milestone.md
    terminology-management.md
    documentation-review.md
```

These are intentionally:

* slow-changing
* repository-wide
* highly reusable

---

# Specs

## Purpose

Specs define:

```text
Behavioral truth
```

Specs are authoritative for:

* invariants
* contracts
* behavior
* failure semantics
* observable guarantees

Specs are NOT:

* implementation plans
* architecture overviews
* milestone plans

---

# Milestones

## Purpose

Milestones define:

```text
Controlled implementation phases
```

Milestones:

* sequence work
* constrain scope
* define deliverables
* define exit criteria

Milestones are NOT authoritative for:

* long-term behavior
* architecture
* permanent semantics

---

# Workflow Model

## Principle

Workflow intent must exist before workflow implementation.

Therefore:

```text
docs/workflows/
```

contains:

* workflow semantics
* workflow constraints
* workflow intent

while:

```text
.github/workflows/
```

contains:

* CI implementation artifacts

---

# Workflow Specification Structure

```md
# Goal

# Constraints

# Non-Goals

# Triggers

# Inputs

# Outputs

# Validation

# Authority

# Document Contract
```

---

# Lightweight Issue Templates

V3 intentionally simplifies issue templates.

Issue templates are NOT:

* process engines
* structured workflow systems
* semantic authorities

They are only:

* lightweight entrypoints
* routing mechanisms
* issue structure helpers

---

# Issue Template Rules

Issue templates should:

* reference relevant TBPs
* reference relevant documentation
* provide minimal structure
* avoid duplicated semantics

Issue templates should NOT:

* duplicate TBP content
* duplicate specs
* duplicate process logic

---

# Recommended Issue Templates

```text
.github/ISSUE_TEMPLATE/
    bug.md
    documentation.md
    implementation.md
    release.md
    create-spec.md
    create-milestone.md
    add-tbp.md
```

---

# Example: Bug Template

```md
# Required Reading

- docs/TERMINOLOGY.md
- docs/TBPS.md
- docs/tbps/bug-investigation.md

# Observed Behavior

# Expected Behavior

# Reproduction

# Related Specs

# Related Milestones
```

---

# Example: Create Spec Template

```md
# Required Reading

- docs/SPECS.md
- docs/TBPS.md
- docs/tbps/create-spec.md

# Goal

# Scope

# Non-Goals

# Related Documents
```

---

# Example: Release Template

```md
# Required Reading

- docs/WORKFLOWS.md
- docs/workflows/release.md
- docs/TBPS.md

# Release Goal

# Included Milestones

# Validation Status

# Open Risks

# Follow-Up Work
```

---

# Testing Guardrails

This is a new foundational operational area in V3.

Very important.

---

# Purpose

Prevent AI agents from:

* creating pathological tests
* running excessively expensive suites
* repeatedly executing long-running validations
* degrading development iteration speed

---

# Testing Philosophy

Tests are divided into:

| Category           | Purpose                          |
| ------------------ | -------------------------------- |
| Fast Tests         | Default execution path           |
| Long-Running Tests | Explicit workflow-only execution |

---

# Fast Tests

Fast tests:

* are deterministic
* run quickly
* are safe for local iteration
* are safe for AI execution
* are allowed during normal agent workflows

Fast tests should:

* complete quickly
* avoid huge datasets
* avoid long waits
* avoid expensive integration environments

---

# Long-Running Tests

Long-running tests:

* are intentionally expensive
* are NOT run by agents
* are NOT default local execution
* are only run via workflows or explicit human action

Examples:

* massive dataset tests
* endurance tests
* stress tests
* scalability tests
* very large integration environments
* extended browser automation
* fuzzing
* benchmarking

---

# Important Rule

Agents should never:

* automatically create long-running tests without explicit approval
* automatically execute long-running tests
* repeatedly invoke expensive suites

This rule alone can prevent massive repository cost explosions.

---

# Recommended Test Classification

## Fast Tests

```text
Unit
Small integration
Deterministic behavior
Contract validation
Small snapshot tests
```

## Long-Running Tests

```text
Performance
Stress
Scalability
Massive dataset
Long browser automation
Endurance
Fuzzing
Benchmarking
```

---

# TESTING.md

Add:

```text
docs/TESTING.md
```

---

# Example Structure

```md
# Testing Strategy

## Purpose

Define repository testing categories and execution rules.

## Test Categories

### Fast Tests

Allowed:
- local execution
- AI execution
- default workflows

Requirements:
- deterministic
- short runtime
- isolated

### Long-Running Tests

Allowed:
- explicit workflow execution
- explicit human invocation

Not allowed:
- default agent execution
- automatic local execution

## Agent Rules

Agents must:
- prefer fast tests
- avoid repeated full-suite execution
- avoid creating expensive tests without justification
- avoid creating hidden scalability explosions

## Workflow Rules

Long-running tests are triggered only through dedicated workflows.
```

---

# AGENTS.md

V3 should now explicitly include testing rules.

Example:

```md
# Agent Instructions

## Required Reading

1. docs/TERMINOLOGY.md
2. docs/TBPS.md
3. docs/SPECS.md
4. docs/WORKFLOWS.md
5. docs/TESTING.md

## Repository Rules

- Use canonical terminology.
- Keep specs authoritative for behavior.
- Keep TBPs methodology-oriented.
- Keep workflow specs synchronized with workflow implementations.

## Testing Rules

- Prefer fast tests.
- Do not execute long-running tests automatically.
- Do not create expensive tests without explicit justification.
- Long-running tests are workflow-triggered only.
```

---

# Upgrade Guide from V2

## Structural Changes

### Standardized Index Model

Replace:

```text
docs/specs/README.md
docs/tbps/README.md
```

with:

```text
docs/SPECS.md
docs/TBPS.md
```

and remove folder READMEs.

---

### Simplified Issue Templates

Replace:

* complex YAML issue forms
* structured workflow-heavy issue systems

with:

* lightweight markdown templates
* TBP references
* minimal structure

---

### Added TESTING.md

Introduce:

* fast test semantics
* long-running test semantics
* agent testing guardrails

---

### Clarified Folder Semantics

Folders now contain:

* concrete documents only

Index documents now contain:

* structure
* lifecycle rules
* categorization
* navigation

---

# Final V3 Philosophy

V3 now converges toward:

```text
Repository Semantic Governance
```

rather than:

```text
AI Prompt Engineering
```

The repository becomes:

* terminology-driven
* lifecycle-aware
* synchronization-aware
* operationally explicit
* resistant to AI context drift
* resistant to documentation ambiguity
* scalable for long-lived AI-assisted development
