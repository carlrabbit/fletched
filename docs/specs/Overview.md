# Overview

## 1. Purpose

Defines the system scope, design principles, and high-level architecture of Fletched.

Fletched is a compiled, statically typed logic-programming library for .NET. Developers author
predicates as ordinary C# types using a fluent DSL. A Roslyn source generator translates those
types into efficient, fully-typed C# state machines before compilation completes. No
interpretation occurs at runtime.

---

## 2. Design Principles

- **Compiled** — predicates are source-generated into C# before assembly emission. There is no
  runtime interpreter or expression evaluator.
- **Statically typed** — fact and predicate types are plain .NET record structs. All unification,
  iteration, and projection is type-checked at compile time.
- **.NET interop** — generated predicates expose standard `IEnumerable<T>` and
  `IAsyncEnumerable<T>` interfaces. Callers need no knowledge of the logic engine internals.
- **Backtracking** — the execution model supports full Prolog-style backtracking through
  choice points, trails, and resume labels encoded directly in the generated state machine.
- **Observability** — the engine exposes metrics and observer hooks for predicate invocations,
  fact scans, choice points, and recursive guards.

---

## 3. Architecture

The pipeline has five stages.

### 3.1 DSL Layer

Developers define:
- `[Fact]` record structs — ground fact tuples stored in an `IFactSource<T>`.
- `[Predicate]` record structs with a `[PredicateBody]` method — logical rules expressed as
  C# expressions using `LogicExpr<bool>`, `Logic.With<T>`, unification (`==`), conjunction
  (`&&`), disjunction (`||`), negation (`Logic.Not`), and predicate calls.
- `TerminalVar<T>` variables — projected into the query result.
- `With<T>` variables — scoped fact iteration variables.

### 3.2 Semantic Analysis

The Roslyn `SemanticAnalyzer` traverses the syntax tree of each `[PredicateBody]` method and
produces a `PredicateModel` — a typed semantic expression tree (`SemanticExpr` hierarchy).

`VariableSymbol` nodes track variable kind (`Terminal`, `Source`, `Fresh`) and type.
`CallExpr` nodes represent calls to other predicates.

### 3.3 Lowering

`IrLowerer` transforms a `PredicateModel` into a `PlanProgram` — a flat, block-based execution
plan in the Fletched Plan IR.

The plan IR consists of:
- `PlanBlock` — a labelled sequence of `PlanInstruction` values with a `PlanTerminator`.
- Instructions — `UnifyInstr`, `ConstraintInstr`, `AssignInstr`, `CompInstr`,
  `LoopBindInstr`/`IndexInitInstr`/`IndexIncrInstr` (fact loops), `CallInstr` (predicate call),
  `NotInstr` (negation-as-failure).
- Terminators — `SucceedTerm`, `FailTerm`, `GotoTerm`, `ChoiceTerm`, `LoopCheckTerm`.

After lowering, `RecursivePlanningAnnotator` computes `RecursivePlanMetadata`:
adornment patterns, magic-set rewriting artifacts (for eligible recursive predicates), and
access-path metadata.

### 3.4 Optimization

`OptimizationPipeline` applies a sequence of `IPlanOptimization` passes to the `PlanProgram`:

**Currently implemented passes** (run in order):
1. `NormalizeSequence` — merge linear single-predecessor blocks.
2. `RemoveRedundantUnify` — constant-fold and eliminate trivially redundant unifications.
3. `DependencyAnalysis` — compute per-instruction read/write sets (used by subsequent passes).
4. `ReorderConjunction` — reorder independent instructions (constraints first, loops last).
5. `IndexSelection` — promote loop key-filter checks as early as dependency ordering allows.
6. `ConstraintHoisting` — hoist constraint instructions before their producer loops when safe.
7. `DeadCodeElimination` — remove instructions after provable failure and unreachable blocks.
8. `LoopSpecialization` — detect loop-invariant bodies (analysis only; no transformation yet).
9. `TempHoisting` — deduplicate repeated field reads with temporary assignment slots.
10. `PredicateCallInlining` — inline eligible non-recursive, non-tabled, deterministic callee
    plans at their call sites (see `docs/specs/Optimization.md` for eligibility rules).

### 3.5 Code Generation

`PredicateEmitter` (sync) and `PredicateEmitterAsync` (async) translate a `PlanProgram` into a
C# source file. Each predicate becomes:
- A `SlotId` enum — named integer identifiers for each slot.
- A `State` struct — holds slot values and `_bound` flags.
- A `Result` record — the projected output type.
- An `Execute` / `ExecuteAsync` method — a switch-loop state machine implementing backtracking,
  loop iteration, and predicate-call dispatch.

### 3.6 Runtime Execution

The generated state machines operate over:
- `IFactSource<T>` — provides fact sequences (full scan or indexed lookup).
- `EngineContext` — carries the recursion guard, query-scoped table store, and observer.
- `RecursionGuard` — enforces max-depth limits for recursive invocations.
- `QueryTableStore` — provides memoization tables for `[Tabled]` predicates.
- `IExecutionObserver` — optional hook for metrics and tracing.

---

## 4. Key Concepts

| Concept | Description |
| --- | --- |
| Fact | A ground tuple stored in an `IFactSource<T>`. Declared with `[Fact]`. |
| Predicate | A logical rule that queries facts and other predicates. Declared with `[Predicate]`. |
| PredicateBody | The `[PredicateBody]` method defining the predicate's logic as a DSL expression. |
| LogicExpr&lt;T&gt; | The DSL expression type. Unification, conjunction, disjunction, and calls all produce `LogicExpr<bool>`. |
| TerminalVar&lt;T&gt; | A query output variable. Its value is included in the result projection. |
| Unification | Binding a slot to a value or another slot. Written as `==` in the DSL. |
| Choice point | A backtracking point. Created by disjunction or fact loops; resumed on failure. |
| Slot | An integer index into the predicate's `State` struct. Assigned during lowering. |
| Tabling | Memoization of recursive predicate results using `[Tabled]`. |
| Adornment | The bound/free pattern of a predicate call, used by magic-set rewriting. |

---

## 5. Related Documents

- `docs/SPECS.md` — spec index
- `docs/ARCHITECTURE.md` — architectural decision record index
- `docs/specs/DSL.md` — DSL surface and semantics
- `docs/specs/IR.md` — Plan IR specification
- `docs/specs/ExecutionPlan.md` — execution plan structure
- `docs/specs/LoweringRules.md` — lowering rules
- `docs/specs/Optimization.md` — optimization pass specifications
- `docs/specs/CodeGeneration.md` — code generation specification
- `docs/specs/Backtracking.md` — backtracking semantics
- `docs/specs/RecursivePredicates.md` — recursive predicate behavior
- `docs/specs/Tabling.md` — tabled predicate semantics

---

## 6. Authority

This document is authoritative for:
- system scope and design principles
- high-level architecture and pipeline stage descriptions
- core concept definitions at the overview level

This document is not authoritative for:
- detailed behavioral contracts (see individual specs in `docs/specs/`)
- milestone sequencing (see `docs/MILESTONES.md`)
- architectural decisions (see `docs/ARCHITECTURE.md`)