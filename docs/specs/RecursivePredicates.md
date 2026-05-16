# Recursive Predicates Specification

## Overview

Recursive predicates are predicates whose invocation call graph contains a path from a predicate identity back to itself.

Recursive predicates preserve source-order depth-first execution and existing copy-in/copy-out invocation semantics.

## Operational Safety

Recursive predicates do not guarantee termination.

Operational safety limits, including recursion depth guards, are defined by `RecursiveSafety.md`.

A recursion guard violation is operational failure, not logical failure.
