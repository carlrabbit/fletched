# Observability Specification

## Overview

Observability defines runtime measurement and callback surfaces for execution diagnostics and performance tracking.

## Runtime Metrics

Runtime metrics include predicate and recursion counters/histograms in `EngineMetrics`, including:

- `predicate_invocations`
- `predicate_invocation_resumes`
- `predicate_invocation_exhaustions`
- `predicate_invocation_failures`
- `recursive_invocations`
- `recursive_depth`

Query-scoped runtime metrics are available through:

- `QueryExecutionOptions.Metrics`
- `QueryMetrics`
- `QueryMetricsSnapshot`
- `QueryMetricsDerived`

These counters are mutable per query execution, are not global, and are not thread-safe.

Advanced fact indexing additionally distinguishes:

- `EqualityIndexLookups`
- `CompositeIndexLookups`
- `RangeIndexLookups`
- `IndexRowsReturned`

Implementations may also expose residual-constraint counters when index filtering is followed by additional predicate constraints.

## Observer Callbacks

`IExecutionObserver` includes recursion-aware callbacks:

- `OnRecursiveInvocation(string predicateName, int depth)`
- `OnRecursiveDepthExceeded(string predicateName, int depth, int maxDepth, bool insideNegation)`

## Guard Diagnostics Visibility

Recursion guard violations are observable through:

- `RecursiveDepthExceededException`
- observer callback `OnRecursiveDepthExceeded(...)`

Violations inside negation are reported as operational failures and remain observable.
