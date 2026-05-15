ResultProjection.md


---

1. Overview

Defines how bound variables (TerminalVar<T>) are materialized into strongly typed result values and returned from predicate execution.


---

2. Core Concepts / Data Structures

2.1 Terminal Variables

readonly struct TerminalVar<T>;

Represents a required bound output variable

Each instance maps to a slot in the state model



---

2.2 Projection Descriptor

record Projection(
    IReadOnlyList<ProjectionField> Fields
);

record ProjectionField(
    string Name,
    int Slot,
    Type Type
);

Defines mapping from state slots → result fields



---

2.3 Generated Result Type

For each predicate:

readonly record struct <PredicateName>Result(
    T1 Field1,
    T2 Field2,
    ...
);

Field order matches projection definition

Field types match TerminalVar<T>



---

2.4 Execution Signature

IAsyncEnumerable<<PredicateName>Result> ExecuteAsync(...);


---

3. Rules and Invariants

Every TerminalVar<T> in predicate signature MUST:

map to exactly one slot

appear exactly once in the projection


All projected slots MUST be bound before result emission

Projection field order is deterministic:

defined by parameter order in [PredicateBody]


Field names:

derived from parameter names


Field types:

must exactly match TerminalVar<T>


No unbound variable may be projected

No implicit projection of non-terminal variables

Projection is immutable and side-effect free

Result type is unique per predicate



---

4. Execution / Behavior

4.1 Success Path

On reaching a success state:

yield return new <PredicateName>Result(
    state.field1,
    state.field2,
    ...
);


---

4.2 Binding Requirement

Before projection:

if (!state.field_bound) goto Fail;

Must be enforced for each projected slot



---

4.3 Integration with Backtracking

Projection occurs only on successful states

After yield return, execution resumes via backtracking

No state mutation occurs during projection



---

5. Examples


---

5.1 DSL

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


---

5.2 Projection

Projection
{
    Fields = [
        ProjectionField("name", nameSlot, typeof(string))
    ]
}


---

5.3 Generated Result Type

readonly record struct AdminUsersResult(
    string name
);

Predicates with multiple "[PredicateBody]" overloads use arity-specific result types:

readonly record struct PersonLookupArity1Result(
    string name
);

readonly record struct PersonLookupArity2Result(
    string login,
    string name
);


---

5.4 Generated Execution Snippet

Success:
    if (!state.name_bound) goto Fail;

    yield return new AdminUsersResult(
        state.name
    );

    goto Resume;

The generated predicate exposes two public methods:
- IEnumerable<AdminUsersResult> Execute(EngineContext ctx, IExecutionObserver? observer = null)
  A goto-based synchronous iterator. Each block is a goto label; choice points and backtracking
  use goto Resume/Fail/Success labels.

- async IAsyncEnumerable<AdminUsersResult> ExecuteAsync(EngineContext ctx, IExecutionObserver? observer = null, CancellationToken cancellationToken = default)
  A native async iterator with an explicit program-counter variable (_pc) and a
  while (true) { switch (_pc) { ... } } loop. Each block becomes a numbered case.
  Control-flow jumps (_pc = N; break;) replace all goto labels, making the method
  compatible with async/await and IAsyncEnumerable<T>.


---

5.5 Multiple Terminal Variables

DSL:

Body(TerminalVar<string> name, TerminalVar<string> login)

Generated:

readonly record struct Result(
    string name,
    string login
);

Projection:

yield return new Result(
    state.name,
    state.login
);
