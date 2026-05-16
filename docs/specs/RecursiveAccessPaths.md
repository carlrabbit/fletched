# Recursive Access Paths Specification

## Authority

This spec defines recursive access-path selection after adornment analysis and magic-set planning.

## Rules

### RAP-001 — Bound Arguments Prefer Indexed Access

When adornment proves an argument bound and a fact index exists for the corresponding member, the planner should prefer indexed fact access.

### RAP-002 — Magic Predicates Are Sources

Generated magic predicates are logical sources. Implementations may realize them as query-scoped in-memory sources.

### RAP-003 — Access Path Must Be Explicit

Execution plans must distinguish between:

- full fact scan
- indexed fact lookup
- magic source lookup
- table lookup

### RAP-004 — Conservative Fallback

If no safe indexed or magic access path exists, the planner must fall back to existing full-scan behavior.

### RAP-005 — No Runtime Reflection Requirement

Optimized recursive access paths should use generated accessors instead of runtime reflection where practical.

## Required Tests

- adorned bound argument uses indexed access when available
- all-free recursive call falls back conservatively
- magic source lookup appears in inspectable plan
- table lookup remains explicit for tabled recursion
- optimized access path result set matches fallback behavior
