# Intermediate Representation

It’s the structured, language-agnostic model of a Prolog program and query that sits between:

Host integration (C# API / DSL)
and
Execution primitives (operator/runtime layer)

IR is:

> A fully explicit, normalized description of logic that is easy to transform and execute.

Instead of dealing with:

ancestor(X, Y) :- parent(X, Z), ancestor(Z, Y).

The IR looks more like:

Predicate(
  name: "ancestor",
  clauses: [
    Clause(
      head: Structure("ancestor", [X, Y]),
      body: [
        Structure("parent", [X, Z]),
        Structure("ancestor", [Z, Y])
      ]
    )
  ]
)


---

Why IR is critical for the design?

It’s the control point of the whole system.

It allows to:
- Decouple host API from execution
- Normalize semantics (no syntactic sugar leaks down)
- Enable transformations (join ordering, optimization later)
- Enforce constraints (via analyzers)

In one sentence:

> IR is the canonical, structured form of the logic program that everything else operates on.
