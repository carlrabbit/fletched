# Concepts

## Core vs Roslyn

- `Fletched.Core`: runtime, DSL primitives, attributes, query execution types.
- `Fletched.Roslyn`: source generator + analyzers used at build time.

## Facts and Predicates

- `[Fact]` marks fact record structs.
- `[Predicate]` + `[PredicateBody]` define query logic.
- `TerminalVar<T>` represents projected output variables.

## Generated Code Expectations

During build, `Fletched.Roslyn` generates query/result code for predicates.
