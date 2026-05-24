# Explainable Planning and Diagnostics

## Purpose

Define the internal/test-facing explanation contract for compiler planning, optimization, code-emission decisions, and phase-aware diagnostics.

## Contracts

- The planner can produce a deterministic `PlanExplanation` per predicate body.
- `PlanExplanation` includes:
  - query identity;
  - semantic summary;
  - normalized IR summary;
  - planned IR summary (blocks, instructions, slots, access paths);
  - recursive planning summary (tabling, magic-set, recursive access paths);
  - optimization summary sourced from optimization trace metadata;
  - code-emission summary;
  - diagnostic explanations.
- `DiagnosticExplanation` includes:
  - diagnostic id and severity;
  - compiler phase;
  - invariant/reason text;
  - source location where available;
  - related symbols when available;
  - suggested fix entries when meaningful.
- Renderers support deterministic:
  - plain text;
  - Markdown;
  - JSON.

## Determinism rules

- Identifier ordering is stable for equivalent semantic input.
- Rendering order does not depend on dictionary/hash-set iteration order.
- JSON uses stable property names and deterministic array ordering.

## API scope

- Explanation entry points are internal/test-facing.
- Explanation generation must not alter generated predicate source output unless explicitly requested by tests.
