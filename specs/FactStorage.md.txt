FactStorage.md


---

1. Overview

Defines the runtime representation, indexing, and access patterns for [Fact] data.
Fact storage is immutable during query execution and accessed through generated code.


---

2. Core Concepts / Data Structures

Fact Table

sealed class FactTable<T>
{
    public T[] Data;
}


---

Indexed Fact Table

sealed class FactTable<T, TKey>
{
    public T[] Data;
    public Dictionary<TKey, int[]> Index;
}

Index maps key → array of indices into Data



---

Engine Context

sealed class EngineContext
{
    public FactTable<User> Users;
    public FactTable<Admin> Admins;
}

One field per [Fact] type

Field name = pluralized fact type name



---

Data Source Representation (Execution Plan)

abstract record PlanDataSource;

record FullScanSource(Type FactType) : PlanDataSource;

record IndexedSource(
    Type FactType,
    MemberInfo KeyMember,
    int KeySlot
) : PlanDataSource;


---

3. Rules and Invariants

Fact tables are read-only during execution

Data arrays are non-null and densely packed

Index keys are derived from a single field or property

Index values reference valid positions in Data

Indexed access is used only when the key slot is bound

Full scan is used when no applicable index exists

Fact instances are copied into state fields on iteration

No mutation of fact instances occurs during execution

Fact storage is external to execution state



---

4. Execution / Behavior

Full Scan Access

var data = ctx.Users.Data;

for (int i = 0; i < data.Length; i++)
{
    var value = data[i];

    state.user = value;
    state.user_bound = true;

    // continue execution
}


---

Indexed Access

if (!state.login_bound)
    goto Fail;

var key = state.login;

if (!ctx.UsersByLogin.Index.TryGetValue(key, out var indices))
    goto Fail;

for (int i = 0; i < indices.Length; i++)
{
    var value = ctx.Users.Data[indices[i]];

    state.user = value;
    state.user_bound = true;

    // continue execution
}


---

Choice Point Integration

int index = 0;

L_Check:
if (index >= data.Length)
    goto Fail;

cps.Push(new ChoicePoint
{
    LabelId = L_Next,
    TrailTop = state.TrailTop
});

state.user = data[index];
state.user_bound = true;

goto Continue;

L_Next:
state.Trail.UnwindTo(ref state, cp.TrailTop);
index++;
goto L_Check;


---

5. Examples

DSL

With<User>((user) => user.Login == name)


---

Execution Plan (simplified)

Loop(
  Slot = user,
  Source = Indexed(User, Login, name),
  Body = Unify(Field(user, Login), name)
)


---

Generated Code (Indexed)

if (!state.name_bound)
    goto Fail;

if (!ctx.UsersByLogin.Index.TryGetValue(state.name, out var indices))
    goto Fail;

for (int i = 0; i < indices.Length; i++)
{
    var user = ctx.Users.Data[indices[i]];

    state.user = user;
    state.user_bound = true;

    // unify succeeds by construction
}


---

Generated Code (Full Scan)

var data = ctx.Users.Data;

for (int i = 0; i < data.Length; i++)
{
    var user = data[i];

    state.user = user;
    state.user_bound = true;

    if (user.Login != state.name)
        goto Next;

    // success path

Next:
    continue;
}