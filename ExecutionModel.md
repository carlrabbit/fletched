# Execution Model

## Overview

The execution model compiles a typed logical DSL into imperative C# code operating over a mutable runtime state.

---

## Core Concepts

- Logical semantics: microKanren-style
- Execution: compiled, not interpreted
- State: mutable, stack-allocated where possible
- Backtracking: explicit via choice points and trail
- Data access: direct, reflection-free

---

## Runtime State (Typed)

```csharp
ref struct State
{
    public string name;
    public User user;
    public Admin admin;

    public bool name_bound;
    public bool user_bound;
    public bool admin_bound;

    public Trail Trail;
}
```

---

## Variable Mapping

| Variable | Type | Field |
|----------|------|-------|
| name     | string | state.name |
| user     | User   | state.user |
| admin    | Admin  | state.admin |

---

## Unification (Typed)

```csharp
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
```

---

## Field Access

DSL:

```csharp
user.Name
```

Generated:

```csharp
state.user.Name
```

---

## Conjunction

DSL:

```csharp
A && B && C
```

Generated:

```csharp
if (!A) goto Fail;
if (!B) goto Fail;
if (!C) goto Fail;
```

---

## Disjunction

DSL:

```csharp
A || B
```

Generated:

```csharp
cps.Push(new ChoicePoint { LabelId = L_B, TrailTop = state.TrailTop });

// A branch
...
goto Success;

L_B:
state.Trail.UnwindTo(ref state, cp.TrailTop);

// B branch
...
```

---

## Choice Points

```csharp
struct ChoicePoint
{
    public int LabelId;
    public int TrailTop;
}
```

---

## Trail

```csharp
struct Trail
{
    public void Push(SlotId slot);
    public void UnwindTo(ref State state, int target);
}
```

---

## Fact Iteration

```csharp
foreach (var user in context.Users.Data)
{
    state.user = user;
    state.user_bound = true;

    ...
}
```

---

## Indexed Access

```csharp
if (state.name_bound)
{
    if (!ctx.Users.ByLogin.TryGetValue(state.name, out var matches))
        goto Fail;

    foreach (var user in matches)
    {
        ...
    }
}
```

---

## Predicate Invocation

```csharp
var frame = new AdminUsers_Frame(...);

L_Call:
if (!frame.MoveNext(ref state))
    goto Fail;

goto Continue;

L_CallResume:
goto L_Call;
```

---

## Frame

```csharp
struct AdminUsers_Frame
{
    int state;

    public bool MoveNext(ref State state)
    {
        switch (state)
        {
            case 0:
                ...
            case 1:
                ...
        }
    }
}
```

---

## Backtracking

```csharp
if (!Backtrack(ref state, cps))
    yield break;
```

---

## Result Projection

```csharp
yield return new AdminUsersResult(
    state.name
);
```

---

## Execution Loop

```csharp
while (true)
{
    if (Run(ref state))
    {
        yield return Project(state);
    }

    if (!Backtrack(ref state, cps))
        yield break;
}
```

---

## Summary

- Typed state replaces slot arrays
- Unification is inlined and specialized
- Field access is direct
- Backtracking via trail and choice points
- Facts accessed via iteration or indexes
- Predicate calls compiled as resumable frames
