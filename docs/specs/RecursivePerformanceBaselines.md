# Recursive Performance Baselines Specification

## Authority

This spec defines benchmark scenarios and baseline reporting requirements for recursive predicates.

It does not define optimization behavior.

## Benchmark Categories

### RPB-001 — Linear Ancestor Traversal

Measures direct recursion over a linear parent chain.

Datasets:

- 10 edges
- 100 edges
- 1,000 edges

Metrics:

- total results
- elapsed time
- allocations
- predicate invocations
- recursive depth observations
- backtracks

### RPB-002 — Branching Tree Traversal

Measures recursive branching behavior over tree-like facts.

Datasets:

- branching factor 2, depth 5
- branching factor 3, depth 5
- branching factor 4, depth 4

Metrics:

- results/sec
- invocation count
- backtrack count
- recursive depth observations
- allocations/result

### RPB-003 — No-Result Recursive Query

Measures failed recursive search against an unreachable target.

### RPB-004 — Mutual Recursion Baseline

Measures mutually recursive predicates without tabling.

### RPB-005 — Tabled vs Untabled Recursion Comparison

Measures equivalent recursive workloads with tabling disabled and enabled for supported predicates.

Metrics:

- total results (set equivalence)
- elapsed time
- allocations
- predicate invocation count
- recursive invocation count
- answer-table insertions (when available)

## Baseline Rules

### RPB-010 — Baselines Are Comparative

Baselines are captured in BenchmarkDotNet artifacts suitable for later comparison.

### RPB-011 — No Hard Performance Gate Initially

Initial baselines do not fail CI by threshold. They establish measurement history.

### RPB-012 — Metrics Correlation

Benchmarks correlate with runtime metrics where enabled:

- predicate invocations
- recursive invocations
- backtracks
- choice points
- recursive depth
- index hits/scans

## Required Tests

- benchmark project compiles
- benchmark scenarios produce expected result counts
- recursive metrics are emitted when metrics instrumentation is enabled
- recursive depth metric is populated
- tabled and untabled recursive benchmark outputs are comparable by scenario identifier

## Must Be Updated Together

- performance documentation
- workflow docs if benchmark CI behavior changes
- runtime metrics documentation
