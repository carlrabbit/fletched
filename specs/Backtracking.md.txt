Backtracking.md


---

1. Overview

Backtracking is the mechanism for exploring alternative execution paths by restoring prior state and resuming execution at recorded continuation points. It is driven by choice points and the trail.


---

2. Core Concepts / Data Structures

ChoicePoint

struct ChoicePoint
{
    public int LabelId;
    public int TrailTop;
}

LabelId: target block label for resumption

TrailTop: trail position to restore state



---

ChoicePoint Stack

Stack<ChoicePoint> ChoicePoints;

LIFO structure

stores pending alternatives



---

Trail

struct Trail
{
    TrailEntry[] Entries;
    int Top;
}


---

TrailEntry

struct TrailEntry
{
    public SlotId Slot;
    public bool WasBound;
}


---

SlotId

enum SlotId
{
    // generated per predicate
}


---

Execution Labels

int LabelId;

corresponds to generated code labels (L_x)



---

3. Rules and Invariants

Choice points are pushed before entering a non-deterministic branch.

The trail records every binding change after the last choice point.

TrailTop in a choice point must correspond to a valid trail position.

Backtracking restores state strictly to the recorded TrailTop.

Choice points are popped exactly once during backtracking.

Execution resumes at the LabelId of the popped choice point.

No state mutation occurs outside tracked bindings.

All variable bindings must be trailed before modification if they may be undone.

Unbound variables must have _bound == false.



---

4. Execution / Behavior

Fail

goto Resume;


---

Resume

---

Cross-Predicate Backtracking

- Backtracking across an invocation boundary resumes the caller choice point first.
- Caller may resume a live callee frame to obtain additional results.
- Callee exhaustion returns control to caller fail handling without mutating caller-local bindings.

---

Invocation Choice Points

- Predicate calls act as resumable choice points from the caller perspective.
- Resume labels for calls must target the invocation continuation path.
- Call continuation and callee frame state must remain deterministic.

---

Trail Ownership Across Calls

- Caller trail entries are unwound only by caller backtracking.
- Callee trail entries are unwound only during callee execution/backtracking.
- Invocation boundaries do not merge caller and callee trails.

Resume:
if (!ChoicePoints.TryPop(out var cp))
    goto End;

Trail.UnwindTo(ref state, cp.TrailTop);
goto Label(cp.LabelId);


---

Trail Unwind

void UnwindTo(ref State state, int targetTop)
{
    while (Trail.Top > targetTop)
    {
        var entry = Trail.Entries[--Trail.Top];

        switch (entry.Slot)
        {
            case SlotId.Name:
                state.name_bound = entry.WasBound;
                break;

            case SlotId.User:
                state.user_bound = entry.WasBound;
                break;
        }
    }
}


---

Binding with Trailing

if (!state.name_bound)
{
    Trail.Push(new TrailEntry
    {
        Slot = SlotId.Name,
        WasBound = false
    });

    state.name = value;
    state.name_bound = true;
}
else if (state.name != value)
{
    goto Fail;
}


---

Choice Point Creation

ChoicePoints.Push(new ChoicePoint
{
    LabelId = L_Alternative,
    TrailTop = Trail.Top
});


---

5. Examples


---

Disjunction

DSL

A || B


---

Execution Plan (simplified)

Choice → L_B
A
Succeed
L_B:
B
Succeed


---

Generated Code

ChoicePoints.Push(new ChoicePoint
{
    LabelId = L_B,
    TrailTop = Trail.Top
});

// A
if (!EvalA(ref state))
    goto Resume;

goto Success;

L_B:
Trail.UnwindTo(ref state, cp.TrailTop);

// B
if (!EvalB(ref state))
    goto Resume;

goto Success;


---

Fact Loop

L_Check:
if (index >= users.Length)
    goto Resume;

ChoicePoints.Push(new ChoicePoint
{
    LabelId = L_Next,
    TrailTop = Trail.Top
});

state.user = users[index];
state.user_bound = true;

goto Body;

L_Next:
index++;
goto L_Check;


---

End of Execution

End:
yield break;
