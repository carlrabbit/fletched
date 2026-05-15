# Terminology

## Purpose

This document defines canonical project terminology.

## Rules

- One sentence per term.
- One canonical meaning per term.
- Avoid aliases unless explicitly listed.
- New domain terms must be added here before broad usage.
- Documentation must use these terms consistently.

## Terms

### Task Best Practice

Reusable operational guidance for a class of repository work.

### Specification

Authoritative behavioral description of a system, component, feature, or process.

### Milestone

Controlled implementation phase with explicit scope, deliverables, and exit criteria.

### Document Authority

Declaration of what a document is allowed to define.

### Document Contract

Declaration of related documents and synchronization obligations.

### Workflow Specification

High-level operational description that a CI workflow implementation must conform to.

## EngineContext

The runtime container that exposes fact tables to generated predicate entry points.

## Fact

A typed record-like domain shape marked with `[Fact]` and stored in a `FactTable<T>`.

## Fact Table

The runtime storage structure that owns dense fact data and optional indexes for a single fact type.

## Indexed Source

An execution-plan data source that replaces a full fact scan with a keyed lookup when a bound or constant equality constraint exists.

## Logic Expression

A typed symbolic expression that represents relational constraints and composition in the DSL.

## Module

A generated scope boundary for facts and predicates in application assemblies that avoids global `EngineContext` collisions.

## Plan

The lowered execution description produced by the generator before C# emission.

## Predicate

A typed logical query shape marked with `[Predicate]` and compiled into executable enumeration code.

## Predicate Body

The method marked with `[PredicateBody]` that defines the logical constraints for a predicate arity.

## Source Generator

The Roslyn component that validates attributed source, builds semantic and planning models, and emits executable code.

## Ground

A logical value whose slot is already bound at an evaluation point.

## Negation-as-Failure

Negation semantics where `Not(goal)` succeeds only when `goal` yields zero solutions.

## Invocation Boundary

The caller/callee separation point where predicate arguments are copied in and results are copied out.

## Caller State

The active predicate state that performs a predicate invocation.

## Callee State

The isolated predicate state owned by the invoked predicate frame.

## Copy-In / Copy-Out

Slot transfer contract that copies argument values and bound flags into callee state, then copies terminal outputs back on each callee success.

## Predicate Frame

A resumable invocation object that stores callee execution position across `MoveNext` calls.

## Predicate Success

A single successful `MoveNext` result that yields one logical solution to the caller.

## Predicate Exhaustion

The terminal `MoveNext == false` state indicating no additional callee solutions remain.

# Authority

This document is authoritative for:
- canonical repository vocabulary
- documentation naming consistency
- cross-document terminology synchronization

This document is not authoritative for:
- behavioral specifications
- architecture decisions
- workflow implementation details

# Document Contract

## Related Documents

- `README.md`
- `docs/SPECS.md`
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`

## Must Be Updated Together

When canonical terminology changes, review and update:
- `README.md`
- `AGENTS.md`
- `copilot-instructions.md`
- `.github/copilot-instructions.md`
- related specs, workflow documents, and TBPs
