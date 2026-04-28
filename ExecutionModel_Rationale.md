# Design Decisions & Trade-offs

## Overview

This document captures the design decisions and trade-offs of the execution model.

---

## Logical Model

### Decision
- Use microKanren-style semantics

### Trade-offs
- Pros:
  - Simple compositional model
  - Clean logical foundation
- Cons:
  - Requires compilation for performance

---

## Execution Strategy

### Decision
- Compile DSL to C# instead of interpreting

### Trade-offs
- Pros:
  - High performance
  - JIT optimizations
- Cons:
  - Increased code generation complexity
  - Larger generated code size

---

## Runtime State

### Decision
- Use generated typed state structs instead of slot arrays

### Trade-offs
- Pros:
  - No boxing
  - No casting
  - Better cache locality
- Cons:
  - More code generation
  - Less dynamic flexibility

---

## Variable Representation

### Decision
- Map variables to fields in state struct

### Trade-offs
- Pros:
  - Direct access
  - Compile-time type safety
- Cons:
  - Requires compile-time knowledge of all variables

---

## Unification

### Decision
- Inline, type-specialized unification

### Trade-offs
- Pros:
  - Eliminates virtual calls
  - Enables inlining
- Cons:
  - Code duplication across types

---

## Field Access

### Decision
- Generate direct property access via proxies

### Trade-offs
- Pros:
  - No reflection at runtime
  - JIT-friendly
- Cons:
  - Requires source generation of proxies

---

## Backtracking

### Decision
- Explicit trail + choice point stack

### Trade-offs
- Pros:
  - Fine-grained control
  - Efficient undo
- Cons:
  - More complex control flow

---

## Disjunction

### Decision
- Compile into explicit branching with labels

### Trade-offs
- Pros:
  - No runtime abstraction overhead
- Cons:
  - Complex generated code

---

## Predicate Invocation

### Decision
- Use resumable frame structs

### Trade-offs
- Pros:
  - Supports backtracking across calls
  - No heap allocation required
- Cons:
  - More complex code generation

---

## Fact Storage

### Decision
- Store facts in typed arrays with optional indexes

### Trade-offs
- Pros:
  - Fast iteration
  - Efficient indexing
- Cons:
  - Requires upfront indexing structures

---

## Indexing

### Decision
- Use dictionary-based indexes for selective fields

### Trade-offs
- Pros:
  - Reduces search space
- Cons:
  - Additional memory usage
  - Maintenance cost

---

## Variable Aliasing

### Decision
- Optional union-find structure

### Trade-offs
- Pros:
  - Avoids value copying
- Cons:
  - Adds complexity
  - Limited benefit with typed state

---

## Result Projection

### Decision
- Use record projection

### Trade-offs
- Pros:
  - Strong typing
  - Clean API
- Cons:
  - Less flexible than dynamic projection

---

## Query Interface

### Decision
- Return IEnumerable results

### Trade-offs
- Pros:
  - Simple integration
- Cons:
  - No built-in async support (future extension)

---

## Parallelism

### Decision
- Defer parallel execution

### Trade-offs
- Pros:
  - Simpler initial design
- Cons:
  - Missed early performance opportunities

---

## Summary

The system favors:
- Compile-time specialization
- Strong typing
- Direct execution

At the cost of:
- Increased generator complexity
- Larger generated code
