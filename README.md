# Fletched 
## A Typed Relational Engine for .NET

This project implements a high-performance relational logic engine embedded directly into C#. It draws inspiration from Prolog and microKanren, but deliberately departs from traditional designs to leverage modern .NET capabilities such as source generation, strong typing, and JIT optimization.

The goal is not to recreate a standalone Prolog system, but to provide a focused, composable logic layer that integrates seamlessly with existing .NET applications.

---

## Core Concepts

Embedded DSL via Source Generators

Predicates and facts are defined using standard C# constructs and attributes. A source generator rewrites these definitions into optimized execution code at compile time.

```csharp
[Fact]
partial record struct User(string Login, string Name);

[Predicate]
partial record struct AdminUsers
{
    [PredicateBody]
    LogicExpr<bool> Body(TerminalVar<string> name) =>
        With<User, Admin>((user, admin) =>
            user.Name == name &&
            user.Login == admin.Login
        );
}
```

This code looks like regular C#, but is interpreted as a typed logical expression. 

---

## Strongly Typed Logical Model

The system uses a typed DSL based on:

- "LogicExpr<T>" — symbolic expressions
- "TerminalVar<T>" — query boundaries
- generated proxies for facts — enabling typed field access

Operators such as "==" and "&&" are reinterpreted as:

- unification
- logical conjunction

This allows compile-time validation while preserving a natural syntax.

---

## Compiled Execution (No Interpreter)

Instead of interpreting logic at runtime, the system:

1. Builds a typed intermediate representation (IR)
2. Compiles it into specialized C# code
3. Executes it using an efficient, imperative runtime

Key characteristics:

- No reflection at runtime
- No expression tree interpretation
- Fully inlined execution paths
- JIT-optimized code

---

## Execution Model

The engine combines:

- microKanren-style semantics (pure logical model)
- WAM-inspired optimizations (efficient execution)

Core runtime features include:

- slot-based variable storage (compiled to typed fields)
- backtracking via choice points
- trail-based state restoration
- synchronous result enumeration (`IEnumerable<T>` via `Execute`, goto-based iterator)
- native async result enumeration (`IAsyncEnumerable<T>` via `ExecuteAsync`, switch-loop state machine) with `CancellationToken` support

---

## Fact Storage and Querying

Facts are stored in strongly typed tables:

> FactTable<User>

Queries are compiled into efficient loops and joins over these tables, with optional indexing for performance.

### List ergonomics

Logical lists support concise DSL and runtime construction:

```csharp
[Fact]
partial record struct NumberSequence(string Name, LogicList<int> Numbers);

[Predicate]
partial record struct PairSequence
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<NumberSequence>(ns =>
            ns.Name == name &&
            ns.Numbers == Logic.List(1, 2)
        );
}

var facts = new[]
{
    new NumberSequence("empty", LogicList<int>.Create()),
    new NumberSequence("pair",  LogicList<int>.Create(1, 2)),
};
```

---

## Predicate Composition

Predicates can call other predicates and participate in backtracking. Calls are compiled into state machines or inlined logic, enabling:

- composability
- reuse
- efficient multi-solution enumeration

Predicates may also declare multiple `[PredicateBody]` methods on the same record as long as each body has a distinct arity.

```csharp
[Predicate]
partial record struct PersonLookup
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Person>(person => person.Name == name);

    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login, TerminalVar<string> name) =>
        Logic.With<Person>(person =>
            person.Login == login &&
            person.Name == name);
}
```

Single-body predicates keep the existing `Execute(...)` / `*Result` surface. Overloaded predicates generate arity-specific entry points such as `ExecuteArity1(...)`, `ExecuteArity2(...)`, and matching result types.

---

## Design Goals

- Performance-first: generated code rivals hand-written imperative logic
- Type safety: full integration with the C# type system
- Minimal runtime overhead: no interpreter, no dynamic dispatch
- Composable logic: small, reusable predicates
- .NET interop: seamless integration with existing code

---

## Samples

The repository includes runnable samples under `samples/`.

The first sample is `samples/WorkAssignment`, a console application that models a
7-day schedule with early/late shifts, worker availability constraints, and fair
assignment search expressed in the Fletched DSL.

See [Samples.md](Samples.md) for an overview of the sample and the exact commands
to run it with generated input or CSV input.
For a ready-to-use GitHub Codespaces and VS Code setup, see
[Codespace.md](Codespace.md).

---

## Non-Goals

- Full ISO Prolog compatibility
- Dynamic runtime evaluation of arbitrary code
- General-purpose programming language replacement

---

# Summary

This project explores a different point in the design space:

> A compiled, strongly typed relational DSL for .NET, combining the clarity of logic programming with the performance characteristics of generated imperative code.

---

More detailed design documents (IR, execution model, trade-offs) are available in the repository under specs/.

# Name origin - Claude 

https://claude.ai/share/ef4597bd-40dc-475f-9dff-42bd052d49b7

From Zeno to **fletched** – from the "arrow paradox" (a fletched arrow)
