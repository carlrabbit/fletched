1. Overview

Not implements negation-as-failure.
A goal Not(G) succeeds iff G produces no solutions.
Execution is isolated, uses shallow state cloning, and enforces ground variables.


---

2. Core Concepts / Data Structures

IR Node

record NotNode(ExprNode Goal) : ExprNode(NodeKind.Not);


---

Execution State (relevant parts)

ref struct State
{
    // Typed slots (generated per predicate)
    // e.g.
    public string name;
    public bool name_bound;

    public Trail Trail;
}


---

Trail

struct Trail
{
    int Top;

    public int Snapshot() => Top;

    public void UnwindTo(ref State state, int targetTop);
}


---

Shallow Clone

State CloneShallow(in State source)
{
    var clone = source;

    // Trail is not copied; only snapshot is used
    return clone;
}


---

Predicate Entry (First-Solution Variant)

bool Exec_First(ref State state, EngineContext ctx);

Returns true on first success

Stops immediately

Produces no projections



---

3. Rules and Invariants

Groundness Requirement

All variables referenced in Not(G) MUST be bound before execution

Enforcement: compile-time validation

Violation: compilation error



---

State Isolation

No bindings from G may affect outer state

No choice points from G may escape

Trail mutations must be fully reverted



---

Determinism

Not introduces no choice points

Not is a single-step evaluation



---

Early Exit

Evaluation of G stops on first success

No full enumeration is permitted



---

No Binding Effect

Not(G) must not bind or modify any variable in outer scope

Operational Errors

Operational guard violations (for example recursion depth guard failures) inside `Not(...)` must propagate as operational failures and must not be converted into negation success.



---

4. Execution / Behavior

Compilation Pattern

Given:

Not(G)

Generate:

// snapshot trail position
var trailTop = state.Trail.Snapshot();

// shallow clone
var subState = state;

// execute subgoal (first solution only)
bool found = G_Exec_First(ref subState, ctx);

// restore state (guaranteed no-op for slots, but trail enforced)
state.Trail.UnwindTo(ref state, trailTop);

// negate result
if (found)
    goto Fail;


---

Inlined Optimization (Constant / Pure Expressions)

For:

Not(user.Name == "Alice")

Generate:

if (state.user.Name == "Alice")
    goto Fail;


---

No Choice Point Interaction

Not does not push to choice point stack

Not does not alter continuation labels



---

5. Examples


---

Example 1: IR

NotNode(
    CallNode("InconsistentTestimony", witnesses)
)


---

Example 2: Generated Code

// Not(InconsistentTestimony(witnesses))

var trailTop = state.Trail.Snapshot();

var subState = state;

bool found = InconsistentTestimony_Exec_First(ref subState, ctx);

state.Trail.UnwindTo(ref state, trailTop);

if (found)
    goto Fail;


---

Example 3: Groundness Enforcement

Input:

Not(x == 5)

Condition:

x_bound == false

Result:

Compile-time error: variable 'x' in Not must be bound


---

Example 4: Inline Case

Input:

Not(user.Login == "admin")

Generated:

if (state.user.Login == "admin")
    goto Fail;


---

Example 5: Predicate Usage

Consistent(witnesses) =>
    Not(InconsistentTestimony(witnesses));

Generated:

var trailTop = state.Trail.Snapshot();
var subState = state;

bool found = InconsistentTestimony_Exec_First(ref subState, ctx);

state.Trail.UnwindTo(ref state, trailTop);

if (found)
    goto Fail;
