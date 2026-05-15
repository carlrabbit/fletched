# Unification.md

## 1. Overview

Unification defines the equality relation between two values within the execution state.  
It operates on slots and values, producing either a successful binding or failure.

---

## 2. Core Concepts / Data Structures

### Slot

```csharp
typedef int SlotId;

Each variable is assigned a unique slot.


---

State (relevant fields)

ref struct State
{
    // per-slot storage
    T_slot_i value_i;
    bool value_i_bound;

    Trail Trail;
}

Each slot has:

a typed value field

a corresponding _bound flag



---

PlanValue

abstract record PlanValue;

record SlotValue(int Slot, Type Type) : PlanValue;
record ConstValue(object? Value, Type Type) : PlanValue;
record FieldValue(PlanValue Target, MemberInfo Member, Type Type) : PlanValue;


---

Trail

struct TrailEntry
{
    SlotId Slot;
    bool WasBound;
}


---

3. Rules and Invariants

General

Unification is symmetric.

Unification is deterministic.

Failure is terminal for the current execution path.



---

Slot State

For each slot s:

s_bound == false → unbound

s_bound == true → bound and s_value is valid



---

Binding

A slot may transition from unbound → bound exactly once per execution branch.

Any binding must be recorded in the trail before mutation.



---

Equality

Equality is type-specific and resolved at code generation time.

No runtime polymorphic equality is permitted.



---

Field Access

Field access is evaluated before unification.

Field access must not modify state.



---

Occurs Check

Occurs check is not performed.



---

4. Execution / Behavior

Slot–Constant

if (!state.s_bound)
{
    state.Trail.Push(s);
    state.s = constant;
    state.s_bound = true;
}
else if (state.s != constant)
{
    goto Fail;
}


---

Slot–Slot

Let a, b be slots:

if (!state.a_bound && !state.b_bound)
{
    // no operation
}
else if (!state.a_bound)
{
    state.Trail.Push(a);
    state.a = state.b;
    state.a_bound = true;
}
else if (!state.b_bound)
{
    state.Trail.Push(b);
    state.b = state.a;
    state.b_bound = true;
}
else if (state.a != state.b)
{
    goto Fail;
}


---

Value–Value

if (left != right)
{
    goto Fail;
}


---

Field–Slot

var value = target.Field;

if (!state.s_bound)
{
    state.Trail.Push(s);
    state.s = value;
    state.s_bound = true;
}
else if (state.s != value)
{
    goto Fail;
}


---

Field–Field

var left = targetA.Field;
var right = targetB.Field;

if (left != right)
{
    goto Fail;
}


---

5. Examples

Example 1 — Slot–Constant

Plan

Unify(Slot(name), Const("Alice"))

Generated

if (!state.name_bound)
{
    state.Trail.Push(SlotId.Name);
    state.name = "Alice";
    state.name_bound = true;
}
else if (state.name != "Alice")
{
    goto Fail;
}


---

Example 2 — Field–Slot

Plan

Unify(Field(Slot(user), Name), Slot(name))

Generated

var value = state.user.Name;

if (!state.name_bound)
{
    state.Trail.Push(SlotId.Name);
    state.name = value;
    state.name_bound = true;
}
else if (state.name != value)
{
    goto Fail;
}


---

Example 3 — Slot–Slot

Plan

Unify(Slot(a), Slot(b))

Generated

if (!state.a_bound && !state.b_bound)
{
}
else if (!state.a_bound)
{
    state.Trail.Push(SlotId.A);
    state.a = state.b;
    state.a_bound = true;
}
else if (!state.b_bound)
{
    state.Trail.Push(SlotId.B);
    state.b = state.a;
    state.b_bound = true;
}
else if (state.a != state.b)
{
    goto Fail;
}


---

Example 4 — Field–Field

Plan

Unify(Field(Slot(user), Login), Field(Slot(admin), Login))

Generated

var left = state.user.Login;
var right = state.admin.Login;

if (left != right)
{
    goto Fail;
}