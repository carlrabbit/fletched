# Fact Storage Refinement Specification

## Authority

This spec defines fact-storage refinements needed by recursive and magic-set workloads.

It extends `FactSourcesAndIndexes.md`.

## Rules

### FSR-001 — Stable Fact Identity

Fact storage uses stable fact-array indices for indexed access and duplicate suppression.

### FSR-002 — Index Stores Fact References or Indices

Indexes store stable fact indices rather than duplicated fact values.

### FSR-003 — Generated Index Accessors

When a fact member is known during generation, optimized indexes should use generated accessors rather than runtime reflection.

### FSR-004 — Transient Magic Sources

Magic-set planning may require transient query-scoped magic sources.

### FSR-005 — Table and Magic Source Separation

Answer tables and magic sources are separate query-scoped concepts.

### FSR-006 — Null Key Semantics

`null` is a valid key bucket when the indexed member type permits null.

### FSR-007 — Diagnostics

The planner may report when a recursive workload would benefit from a missing index.

## Required Tests

- index stores fact indices
- generated accessor index works
- transient magic source is query-scoped
- answer table and magic source do not share storage
- null key behavior is tested
- missing recursive index diagnostic can be emitted conservatively
