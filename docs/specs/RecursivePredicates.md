# Recursive Predicates Specification

## Overview

Recursive predicates are predicates whose invocation call graph contains a path from a predicate identity back to itself.

Recursive predicates preserve source-order depth-first execution and existing copy-in/copy-out invocation semantics.

## Tabled Recursion

Recursive predicates may opt into tabled execution as defined by `Tabling.md`.

Untabled recursion preserves baseline depth-first behavior.

Tabled recursion may alter operational ordering but must preserve the result set for supported positive recursive predicates.

## Operational Safety

Recursive predicates do not guarantee termination.

Operational safety limits, including recursion depth guards, are defined by `RecursiveSafety.md`.

A recursion guard violation is operational failure, not logical failure.
