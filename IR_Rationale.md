# IR_Rationale.md

## Overview

This document captures the design decisions, trade-offs, and guiding principles behind the Intermediate Representation (IR) and execution model.

---

# Core Philosophy

- Favor **compile-time specialization** over runtime interpretation
- Keep IR **minimal and closed**
- Separate **logical model** from **execution strategy**
- Allow incremental evolution from simple to highly optimized

---

# Logic Model Choice

## Decision
Use a microKanren-inspired logical model.

## Trade-offs

### Pros
- Simple semantics
- Compositional
- Easy to represent as expression trees

### Cons
- Does not directly map to high-performance execution
- Requires lowering to optimized form

---

# Execution Model

## Decision
Hybrid approach:
- microKanren semantics
- WAM-style optimizations via code generation

## Trade-offs

### Pros
- Retains declarative clarity
- Enables high-performance compiled execution
- Avoids building a full VM

### Cons
- More complex code generation
- Harder to debug generated code

---

# IR Design

## Decision
Use a small set of node types:
- Var, Const, Field, Unify, Conj, Disj, Constraint

## Trade-offs

### Pros
- Easy to analyze and transform
- Stable foundation for optimizations
- Predictable code generation

### Cons
- Requires encoding higher-level constructs externally

---

# Conjunction Representation

## Decision
Flatten conjunctions into lists

## Trade-offs

### Pros
- Simplifies code generation
- Eliminates recursion
- Enables reordering later

### Cons
- Loses original syntactic grouping

---

# Disjunction Representation

## Decision
Binary tree structure

## Trade-offs

### Pros
- Simple representation
- Maps directly to branching

### Cons
- Requires nesting for multiple branches

---

# Variable Model

## Decision
Map variables to slot indices

## Trade-offs

### Pros
- Simple runtime representation
- Efficient indexing
- Compatible with both dynamic and typed state

### Cons
- Requires mapping layer in generator

---

# Terminal Variables

## Decision
TerminalVar<T> defines query boundary

## Trade-offs

### Pros
- Clear separation between internal and external variables
- Enables result projection

### Cons
- Ambiguity between input/output roles

---

# Result Projection

## Decision
Return all terminal variables (initially)

## Trade-offs

### Pros
- Simple implementation
- No additional syntax

### Cons
- Leaks internal structure
- Cannot express derived results
- Couples API to predicate signature

---

# Future Direction

Explicit projection (e.g., select) to decouple logic and output.

---

# Field Access

## Decision
Capture field access as IR nodes with MemberInfo

## Trade-offs

### Pros
- Enables reflection-free runtime
- Supports direct code generation
- Preserves type information

### Cons
- Requires generator to resolve members

---

# Unification

## Decision
Generate type-specialized unification code

## Trade-offs

### Pros
- Eliminates boxing
- Enables inlining
- Improves performance significantly

### Cons
- Increases generated code size

---

# Variable Aliasing

## Decision
Support union-find style aliasing (initially)

## Trade-offs

### Pros
- Avoids unnecessary copying
- Matches logical semantics

### Cons
- Adds complexity to runtime and trail
- Less important with typed state

---

# Typed State

## Decision
Generate predicate-specific state structs

## Trade-offs

### Pros
- No boxing or casting
- Better cache locality
- JIT-friendly

### Cons
- More generated code
- Less generic runtime

---

# Trail Design

## Decision
Use trail to undo bindings and aliasing

## Trade-offs

### Pros
- Enables backtracking
- Simple undo model

### Cons
- Requires careful tracking of mutations

---

# Choice Points

## Decision
Explicit choice point stack

## Trade-offs

### Pros
- Clear control flow
- Supports backtracking and disjunction

### Cons
- Requires manual state management in generated code

---

# Fact Storage

## Decision
Store facts in typed tables

## Trade-offs

### Pros
- Fast iteration
- Type-safe access
- Easy indexing

### Cons
- Requires separate storage layer

---

# Indexing

## Decision
Add optional indexes for selective queries

## Trade-offs

### Pros
- Avoids full scans
- Improves performance for joins

### Cons
- Additional memory usage
- Requires compile-time detection

---

# Predicate Invocation

## Decision
Compile predicates into callable state machines

## Trade-offs

### Pros
- Supports backtracking across calls
- Enables composition

### Cons
- More complex code generation
- Requires frame/state management

---

# Overall Architecture

## Decision
Compile predicates into specialized programs

## Trade-offs

### Pros
- High performance
- Strong typing at boundaries
- No interpreter overhead

### Cons
- Larger generated codebase
- More complex build-time processing

---

# Future Considerations

- Query planning and join ordering
- Parallel execution
- Recursive predicate optimization
- Advanced indexing strategies

---

# End of Document
