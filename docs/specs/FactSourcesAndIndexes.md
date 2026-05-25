# Fact Sources and Indexes Specification

## Authority

This spec defines planner-visible fact sources and index-aware source selection.

## Rules

### FSI-001 — Full Scan Remains Available

A fact source may always fall back to a full scan.

### FSI-002 — Indexed Source Selection

A planner may select indexed fact access when it can prove a declared equality or range key shape.

Declared fact indexes are explicit through `[FactIndex(...)]` attributes on `[Fact]` types.

- Equality indexes may target one or more members.
- Range indexes target exactly one comparable member in this milestone.
- Full scans remain the required fallback whenever the selected key inputs are not available at lookup time.

### FSI-003 — Recursive Access Paths

Recursive and magic-set plans may use generated indexes and transient magic sources.

Execution plans must identify the selected access-path kind explicitly.

### FSI-004 — Deterministic Source Choice

Given the same lowered plan and binding information, fact-source choice must be deterministic.

### FSI-005 — Composite Equality Selection

Composite equality indexes require all declared members to participate in the selected lookup key.

### FSI-006 — Range Selection

Range indexes may be selected for `>`, `>=`, `<`, and `<=` comparisons when at least one bound is available.

## Related Documents

- `IndexedSources.md`
- `RecursiveAccessPaths.md`
- `FactStorageRefinement.md`
