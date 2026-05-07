EngineContext.md

---

1. Overview

Defines the runtime container that exposes fact tables to generated predicates.
`EngineContext` is a hand-written partial type in `Fletched.Core.Runtime`; the source generator extends it with one fact-table property per `[Fact]` type.

---

2. Core Concepts / Data Structures

2.1 Base Declaration

public partial class EngineContext
{
}

The base declaration contains no hand-written members beyond the partial type itself.

---

2.2 Generated Fact Property

public FactTable<User> Users { get; set; }
    = new FactTable<User>();

- One property is generated for each `[Fact]` type.
- The generated property type is `FactTable<TFact>`.
- Properties are public and settable so callers can provide runtime data.
- Each property is initialized to an empty `FactTable<TFact>`.

---

2.3 Property Naming

Given a fact type named `User`, the generated property name is `Users`.

If a fact type's final character is `s`, the generated property keeps the same name.
The rule is character-based; no additional `s` is appended in that case.

The current rule is a simple suffix check performed by the generator:

- if the type name ends with `s`, use it as-is
- otherwise append `s`

---

2.4 Runtime Consumption

Generated predicate entry points accept an `EngineContext` instance explicitly:

bool <Predicate>_MoveNext(
    ref <Predicate>_State state,
    ref <Predicate>_Frame frame,
    EngineContext ctx
);

The context provides access to fact data but does not store per-query execution state.

---

3. Rules and Invariants

- `EngineContext` MUST remain a partial class in the `Fletched.Core.Runtime` namespace.
- Generated fact properties MUST also be emitted in the `Fletched.Core.Runtime` namespace so they extend the same type.
- Each generated fact property MUST default to a non-null `FactTable<TFact>` instance.
- Predicates MUST receive `EngineContext` as an explicit parameter; no ambient/global context is used.
- `EngineContext` holds shared runtime fact storage only; bindings, frames, and trail state live in generated execution state.

---

4. Execution / Behavior

4.1 Context Construction

var ctx = new EngineContext();

ctx.Users = new FactTable<User>(new[]
{
    new User("alice"),
    new User("bob"),
});

An empty `EngineContext` is valid because generated properties are initialized to empty fact tables.

---

4.2 Full Fact Scan

foreach (var user in ctx.Users.Data)
{
    // continue execution
}

Generated code reads fact rows through the relevant `FactTable<TFact>` property.

---

4.3 Indexed Fact Lookup

if (!ctx.Users.TryGetIndex("Login", state.name, out var indices))
    goto Fail;

for (int i = 0; i < indices.Length; i++)
{
    var user = ctx.Users.Data[indices[i]];
    // continue execution
}

Indexes are owned by `FactTable<TFact>`; `EngineContext` only supplies access to the table instance.

---

5. Examples

5.1 Base Runtime Type

namespace Fletched.Core.Runtime;

public partial class EngineContext
{
}

---

5.2 Generated Extension

namespace Fletched.Core.Runtime;

public partial class EngineContext
{
    public FactTable<User> Users { get; set; }
        = new FactTable<User>();
}

---

5.3 Usage from Tests / Callers

var ctx = new EngineContext();
ctx.Users = new FactTable<User>(new[]
{
    new User("alice"),
    new User("bob"),
});

var results = default(UserNames).Execute(ctx);

---
