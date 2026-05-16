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

## Observer Callbacks

`IExecutionObserver` includes recursion-aware callbacks:

- `OnRecursiveInvocation(string predicateName, int depth)`
- `OnRecursiveDepthExceeded(string predicateName, int depth, int maxDepth, bool insideNegation)`

## Guard Diagnostics Visibility

Recursion guard violations are observable through:

- `RecursiveDepthExceededException`
- observer callback `OnRecursiveDepthExceeded(...)`

Violations inside negation are reported as operational failures and remain observable.
