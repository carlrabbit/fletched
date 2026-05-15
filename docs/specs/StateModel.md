StateModel.md

1. Overview

Defines the runtime state representation used during predicate execution.
The state is a generated, strongly typed structure with explicit binding and backtracking support.

---

2. Core Concepts / Data Structures

2.1 State Structure

ref struct <PredicateName>_State
{
    // Variable storage
    public T1 var1;
    public T2 var2;
    ...

    // Binding flags
    public bool var1_bound;
    public bool var2_bound;
    ...

    // Backtracking support
    public Trail Trail;
}

---

2.2 Slot Identity

enum SlotId
{
    Var1,
    Var2,
    ...
}

- Each logical variable is assigned exactly one "SlotId"
- Mapping between variables and state fields is fixed at compile time

---

2.3 Trail

struct TrailEntry
{
    public SlotId Slot;
    public bool WasBound;
}

struct Trail
{
    private Span<TrailEntry> entries;
    private int top;

    public int Top => top;

    public void Push(SlotId slot, bool wasBound)
    {
        entries[top++] = new TrailEntry { Slot = slot, WasBound = wasBound };
    }

    public void UnwindTo(ref <PredicateName>_State state, int targetTop)
    {
        while (top > targetTop)
        {
            var entry = entries[--top];

            switch (entry.Slot)
            {
                case SlotId.Var1:
                    state.var1_bound = entry.WasBound;
                    break;
                case SlotId.Var2:
                    state.var2_bound = entry.WasBound;
                    break;
            }
        }
    }
}

---

2.4 Choice Point (State Interaction)

struct ChoicePoint
{
    public int LabelId;
    public int TrailTop;
}

- "TrailTop" references "Trail.Top"

---

3. Rules and Invariants

- Each variable corresponds to exactly one:
  - state field
  - "_bound" flag
  - "SlotId"
- A variable is unbound iff its "_bound" flag is "false"
- A variable is bound iff its "_bound" flag is "true"
- State fields contain valid values only when "_bound == true"
- All bindings must be recorded in the "Trail" before mutation
- "Trail.Top" monotonically increases during forward execution
- "Trail.UnwindTo" restores all "_bound" flags to previous states
- State structure is stack-allocated ("ref struct")
- No boxing or "object"-based storage is permitted
- Slot identity is compile-time fixed and immutable

---

4. Execution / Behavior

4.1 Binding

if (!state.var1_bound)
{
    state.Trail.Push(SlotId.Var1, false);
    state.var1 = value;
    state.var1_bound = true;
}
else if (state.var1 != value)
{
    goto Fail;
}

---

4.2 Unbinding (via backtracking)

state.Trail.UnwindTo(ref state, targetTop);

---

4.3 Choice Point Interaction

cps.Push(new ChoicePoint
{
    LabelId = L_next,
    TrailTop = state.Trail.Top
});

---

4.4 State Initialization

var state = new <PredicateName>_State
{
    var1_bound = false,
    var2_bound = false,
    ...
};

---

4.5 Invocation Boundary State Rules

- Caller state and callee state are distinct state instances.
- Copy-in transfers mapped argument values and bound flags from caller to callee.
- Copy-out transfers mapped terminal outputs from callee to caller only on callee success.
- Callee-local temporary bindings are not visible in caller state.

---

4.6 Negation Isolation

- Negation evaluation must not leak temporary bindings into outer scope.
- Trail checkpoints taken before negation must be restored after negation evaluation.

---

5. Examples

5.1 DSL

user.Name == name

---

5.2 Generated State

ref struct Example_State
{
    public User user;
    public string name;

    public bool user_bound;
    public bool name_bound;

    public Trail Trail;
}

---

5.3 Generated Binding Code

if (!state.name_bound)
{
    state.Trail.Push(SlotId.Name, false);
    state.name = state.user.Name;
    state.name_bound = true;
}
else if (state.name != state.user.Name)
{
    goto Fail;
}

---

5.4 Backtracking

state.Trail.UnwindTo(ref state, cp.TrailTop);

---
