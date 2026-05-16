# Fact Sources and Indexes Specification

## Authority

This spec defines planner-visible fact sources and index-aware source selection.

## Rules

### FSI-001 — Full Scan Remains Available

A fact source may always fall back to a full scan.

### FSI-002 — Indexed Source Selection

A planner may select indexed fact access when it can prove an equality-constrained key.

### FSI-003 — Recursive Access Paths

Recursive and magic-set plans may use generated indexes and transient magic sources.

Execution plans must identify the selected access-path kind explicitly.

### FSI-004 — Deterministic Source Choice

Given the same lowered plan and binding information, fact-source choice must be deterministic.

## Related Documents

- `IndexedSources.md`
- `RecursiveAccessPaths.md`
- `FactStorageRefinement.md`
