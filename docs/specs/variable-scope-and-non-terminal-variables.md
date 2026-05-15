# Variable Scope and Non-Terminal Variables

# Goal

Define the observable behavior of predicate-local variable scopes and non-terminal variables.

# Scope

This spec applies to variables introduced by predicate body parameters and `With<T...>` scopes.

This spec defines:
- terminal variable behavior
- non-terminal variable behavior
- local scope boundaries
- fact-source variable behavior
- variable visibility and result materialization rules
- grounding interactions for scoped variables

# Non-Goals

- Recursive predicate semantics
- Query planner behavior
- Index selection behavior
- Generated code structure
- Runtime storage layout
- Predicate invocation ABI details

# Terminology

Uses terms from `docs/TERMINOLOGY.md`.

Additional terms used by this spec:

- Terminal variable: query-boundary variable declared as a `TerminalVar<T>` predicate body parameter.
- Non-terminal variable: predicate-local logical variable that is not part of the query result boundary.
- Source variable: scoped variable introduced by `With<T>` where `T` is a fact type and candidates are read from the corresponding fact table.
- Fresh variable: scoped variable introduced by `With<T>` where `T` is not a fact type and no fact source is enumerated.

# Invariants

- Each logical variable has exactly one declaration scope.
- A variable introduced by `With<T...>` is visible only inside the corresponding lambda body.
- A non-terminal variable is never materialized as a query result unless future explicit projection semantics require it.
- A terminal variable must be bound before result materialization.
- A scoped variable must not escape the lambda that declares it.
- A fact-source variable and a fresh variable are both local variables.
- A fact-source variable is bound by fact-source enumeration before the `With` body is evaluated.
- A fresh variable is initially unbound when the `With` body begins evaluation.
- Groundness analysis must account for local scope and execution order.

# Behavioral Rules

## Terminal Variables

- `TerminalVar<T>` parameters define the query boundary.
- A terminal variable may be initially bound by query input.
- A terminal variable may be bound by unification during predicate execution.
- A terminal variable is included in generated result materialization.
- Result materialization fails for a solution if any terminal variable remains unbound.

## Non-Terminal Variables

- A non-terminal variable is a predicate-local logical variable.
- A non-terminal variable may participate in unification, constraints, predicate calls, disjunction, and negation validation.
- A non-terminal variable is not part of the generated result shape.
- A non-terminal variable may become ground during evaluation.
- A non-terminal variable is restored by normal backtracking rules.

## `With<T>` Resolution

`With<T>` has two valid behaviors:

1. If `T` is a fact type, `With<T>` introduces a source variable.
2. If `T` is not a fact type, `With<T>` introduces a fresh variable.

The behavior must be deterministic from the resolved type symbol.

## Multiple `With<T...>` Variables

For `With<T1, ..., TN>`:

- each generic type parameter declares one local variable
- each variable is resolved independently as source or fresh
- variables are visible only inside the lambda body
- declaration order determines local variable ordering for diagnostics and generated result stability, not result materialization

## Source Variables

- A source variable enumerates values from the fact table associated with its fact type.
- A source variable is bound before its lambda body is evaluated.
- If the fact table has no candidates, the `With` scope yields no solutions.

## Fresh Variables

- A fresh variable starts unbound.
- A fresh variable can only become bound through unification or copy-out from predicate invocation.
- A fresh variable does not enumerate values by itself.
- A fresh variable that remains unbound at the end of predicate execution is allowed if it is not required for terminal result materialization or grounding.

## Scope Boundaries

- A variable declared in a `With` lambda must not be referenced outside that lambda.
- Nested `With` scopes may reference variables from outer scopes.
- Inner variables may shadow names only if the host language permits it and semantic analysis resolves the symbols unambiguously.

## Negation and Groundness

- A scoped variable referenced by `Not(goal)` must be ground at the negation evaluation point if it is outward-visible to the negated goal.
- A fresh variable declared inside `Not(goal)` must not escape the negated goal.
- Bindings produced during negation evaluation must not become visible outside the negated goal.

# Inputs

The spec applies to predicate bodies using:

```csharp
TerminalVar<T>
With<T>(...)
With<T1, ..., TN>(...)
```

# Outputs

Observable outputs are generated predicate results containing only terminal variables.

Non-terminal variables do not produce direct output.

# Failure Semantics

- Invalid variable escape is a compile-time diagnostic.
- Unsupported or ambiguous `With<T>` resolution is a compile-time diagnostic.
- Result materialization fails for unbound terminal variables.
- Ungrounded negation usage is a compile-time diagnostic when detectable before code generation.
- A source variable with no candidates yields no solutions.
- A fresh variable with no binding source does not fail by itself.

# Validation

Validation must include:

- unit tests for terminal and non-terminal variable classification
- semantic analysis tests for `With<T>` source/fresh resolution
- integration tests for fresh variable unification
- integration tests proving fresh variables are not projected
- diagnostics tests for variable escape
- diagnostics tests for ungrounded negation involving scoped variables
- backtracking tests for scoped variable restoration

# Examples

## Fresh Variable

```csharp
[Predicate]
partial record struct SameLogin
{
    [PredicateBody]
    LogicExpr<bool> Body(TerminalVar<string> left, TerminalVar<string> right) =>
        With<string>(login =>
            left == login &&
            right == login);
}
```

`login` is a fresh non-terminal variable.

## Source Variable

```csharp
[Fact]
partial record struct User(string Login, string Name);

[Predicate]
partial record struct UsersByLogin
{
    [PredicateBody]
    LogicExpr<bool> Body(TerminalVar<string> login) =>
        With<User>(user =>
            user.Login == login);
}
```

`user` is a source variable.

## Nested Scope

```csharp
With<string>(login =>
    With<User>(user =>
        user.Login == login));
```

The inner `With<User>` scope may reference `login` from the outer scope.

# Related Architecture

- `docs/specs/DSL.md`
- `docs/specs/SemanticModel.md`
- `docs/specs/IR.md`
- `docs/specs/LoweringRules.md`
- `docs/specs/StateModel.md`
- `docs/specs/Diagnostics.md`

# Related Decisions

None yet.

# Authority

This document is authoritative for:
- terminal variable behavior
- non-terminal variable behavior
- `With<T>` source/fresh behavioral distinction
- local variable visibility rules
- variable result materialization rules
- scoped-variable groundness behavior

This document is not authoritative for:
- generated code layout
- execution-plan structure
- fact storage layout
- recursion semantics
- query planning decisions

# Document Contract

## Related Documents

- `docs/specs/README.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/DSL.md`
- `docs/specs/SemanticModel.md`
- `docs/specs/LoweringRules.md`
- `docs/specs/StateModel.md`

## Must Be Updated Together

When variable scope or non-terminal variable behavior changes, review and update:
- `docs/specs/README.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/DSL.md`
- `docs/specs/SemanticModel.md`
- `docs/specs/IR.md`
- `docs/specs/LoweringRules.md`
- `docs/specs/StateModel.md`
- `docs/specs/Diagnostics.md`
