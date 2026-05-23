# Task Best Practices

## Purpose

Task Best Practices define reusable operational methodology for classes of work in this repository.

TBPs are not prompts.
TBPs are not task descriptions.
TBPs are not specifications.

They define how work should be approached.

## TBP Scope Rules

TBPs define:
- operational methodology
- repository process
- synchronization expectations
- validation expectations
- required reading patterns

TBPs do not define:
- feature semantics
- concrete implementation details
- one-off tasks

Feature semantics belong in specs.
Concrete work belongs in milestones and issues.
Architectural choices belong in decisions and architecture documents.

## TBP Layers

| Layer | Purpose |
| --- | --- |
| Foundational TBPs | Meta-structure and documentation lifecycle |
| Governance TBPs | Terminology, specs, milestones, and documentation consistency |
| Implementation TBPs | Feature work, bug work, and refactoring |
| Operational TBPs | Workflow changes, release preparation, and other recurring operations |

## Available TBPs

| TBP | Purpose |
| --- | --- |
| [`tbps/add-tbp.md`](tbps/add-tbp.md) | Add or revise a Task Best Practice |
| [`tbps/create-spec.md`](tbps/create-spec.md) | Create a new specification |
| [`tbps/create-milestone.md`](tbps/create-milestone.md) | Define a new milestone |
| [`tbps/start-milestone.md`](tbps/start-milestone.md) | Prepare a milestone for implementation |
| [`tbps/finish-milestone.md`](tbps/finish-milestone.md) | Close and consolidate a milestone |
| [`tbps/documentation-review.md`](tbps/documentation-review.md) | Review documentation consistency |
| [`tbps/terminology-management.md`](tbps/terminology-management.md) | Add or revise terminology |
| [`tbps/feature-implementation.md`](tbps/feature-implementation.md) | Implement feature work |
| [`tbps/bug-investigation.md`](tbps/bug-investigation.md) | Investigate and fix defects |
| [`tbps/refactor-planning.md`](tbps/refactor-planning.md) | Plan safe refactors |
| [`documentation-changes.md`](tbps/documentation-changes.md) | Keep authoritative docs aligned with repository changes |
| [`workflow-changes.md`](tbps/workflow-changes.md) | Change workflow intent and workflow implementation together |
| [`release-preparation.md`](tbps/release-preparation.md) | Prepare packaging and release changes without drifting from workflow intent |

# Authority

This document is authoritative for:
- TBP scope rules
- TBP categorization
- the index of available TBPs

This document is not authoritative for:
- feature behavior
- milestone scope
- workflow implementation details

# Document Contract

## Related Documents

- `docs/TERMINOLOGY.md`
- `.github/ISSUE_TEMPLATE/`

## Must Be Updated Together

When TBP scope rules or the TBP catalog change, review and update:
- affected documents under `docs/tbps/`
- related issue templates under `.github/ISSUE_TEMPLATE/`
