PredicateInvocation.md

1. Overview

Defines the structure and semantics of predicate invocation, including argument passing, execution frames, and integration with backtracking.
Predicate calls support multi-result evaluation and resumable execution.

---

2. Core Concepts / Data Structures

Plan Node

record PlanCall(
    Type PredicateType,
    IReadOnlyList<int> ArgumentSlots
) : PlanInstruction;

---

Frame Structure

struct <Predicate>_Frame
{
    public int State;
    public int ResumeLabel;
}

---

Generated Entry Method

bool <Predicate>_MoveNext(
    ref <Predicate>_State state,
    ref <Predicate>_Frame frame,
    EngineContext ctx
);

---

Argument Mapping

struct ArgumentMap
{
    public int CallerSlot;
    public int CalleeSlot;
}

---

3. Rules and Invariants

- Predicate invocation is represented exclusively by "PlanCall".
- Predicate overload resolution is based on predicate name plus arity.
- Each predicate compiles to a resumable execution function ("MoveNext").
- Each invocation instance owns a frame with an explicit state integer.
- Argument passing is slot-based and positional.
- Calls must target exactly one "[PredicateBody]" overload with the same arity.
- Caller and callee states are independent.
- Argument values are copied from caller state into callee state before execution.
- Bound flags are copied together with values.
- Callee execution may yield multiple results via repeated "MoveNext" calls.
- Failure of a predicate call transfers control to caller fail handling.
- Predicate calls must not mutate caller state directly.
- All control flow across predicate boundaries is mediated by frames.
- Frame state is sufficient to resume execution without additional context.

---

4. Execution / Behavior

Invocation Sequence

1. Allocate frame
2. Initialize callee state
3. Copy arguments (value + bound flag)
4. Execute "MoveNext"
5. On success:
   - propagate result (via shared or copied slots)
6. On failure:
   - trigger backtracking

---

Invocation Semantics

- Predicate invocation starts at an invocation boundary between caller state and callee state.
- Caller argument slots are copied into callee argument slots before the first callee step.
- Callee execution is resumable and is driven only through repeated MoveNext calls.
- Each MoveNext success represents exactly one predicate success.
- MoveNext returning false represents predicate exhaustion.

---

Caller/Callee Ownership

- Caller owns caller slots, caller trail, and caller choice points.
- Callee owns callee slots, callee frame state, and callee-local trail operations.
- Callee must never mutate caller-local slots directly.
- Caller observes callee results only through copy-out rules.

---

Copy-In / Copy-Out Rules

- Copy-in transfers value and bound flag from each mapped caller slot to callee slot.
- Copy-out is allowed only for mapped terminal outputs when the callee yields success.
- Copy-out does not transfer callee-local temporary variables.
- Copy-out on exhausted callee is forbidden.

---

Invocation Frames

- Each invocation instance owns one frame.
- Frame state is sufficient to resume from the last callee suspension point.
- Frame reuse across different invocation instances is invalid.

---

Invocation Backtracking

- Caller-side backtracking may re-enter an existing invocation frame to request the next callee solution.
- If callee is exhausted, control returns to caller fail/resume handling.
- Trail unwind remains scoped to the owner of each trail.

---

Recursive Invocation Constraints

- Invocation semantics must remain recursion-safe by preserving frame ownership per call instance.
- Recursive optimization policies are out of scope; semantics still require deterministic copy-in/copy-out behavior.

---

Generated Call Pattern

var frame = new AdminUsers_Frame { State = 0 };

L_Call:
if (!AdminUsers_MoveNext(ref calleeState, ref frame, ctx))
    goto Fail;

goto Continue;

L_CallResume:
goto L_Call;

---

MoveNext Contract

bool MoveNext(...)

- Returns "true" on success
- Returns "false" when no more solutions exist
- Preserves internal execution state between calls

---

State Initialization

callee.name = caller.name;
callee.name_bound = caller.name_bound;

---

Backtracking Integration

- Caller pushes choice point before invoking predicate
- Resume label targets call continuation
- Frame state determines next execution position
- Trail is local to callee state

---

5. Examples

DSL

AdminUsers(name)

---

Execution Plan

PlanCall(
  PredicateType = AdminUsers,
  ArgumentSlots = [nameSlot]
)

---

Generated Code

var frame = new AdminUsers_Frame { State = 0 };

L_Call:
if (!AdminUsers_MoveNext(ref calleeState, ref frame, ctx))
    goto Fail;

// success
goto Continue;

---

MoveNext Skeleton

bool AdminUsers_MoveNext(
    ref AdminUsers_State state,
    ref AdminUsers_Frame frame,
    EngineContext ctx)
{
    switch (frame.State)
    {
        case 0:
            frame.State = 1;
            goto L_Start;

        case 1:
            goto L_Resume;
    }

L_Start:
    // execution logic
    return true;

L_Resume:
    // resume logic
    return false;
}
