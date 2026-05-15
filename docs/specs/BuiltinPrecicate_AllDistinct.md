1. Overview

AllDistinct is a built-in global constraint ensuring that all elements in a collection are pairwise distinct.
It operates over collections of logical variables and integrates into the constraint evaluation pipeline.


---

2. Core Concepts / Data Structures

DSL Surface

LogicExpr<bool> AllDistinct<T>(LogicExpr<T[]> values);


---

IR Node

enum NodeKind
{
    // ...
    AllDistinct
}

record AllDistinctNode(
    ExprNode Collection,
    Type ElementType
) : ExprNode(NodeKind.AllDistinct);


---

Execution Plan Instruction

enum PlanOp
{
    // ...
    AllDistinct
}

record PlanInstruction(
    PlanOp Op,
    int CollectionSlot
);


---

Typed State Access (Example)

ref struct State
{
    public int s1;
    public int s2;
    public int s3;

    public bool s1_bound;
    public bool s2_bound;
    public bool s3_bound;
}


---

3. Rules and Invariants

1. All elements in the collection must be pairwise unequal:

∀ i ≠ j: values[i] ≠ values[j]


2. Only bound elements participate in evaluation.


3. If any two bound elements are equal, evaluation fails.


4. Unbound elements do not cause failure.


5. The constraint does not bind variables.


6. The constraint is deterministic and side-effect free.




---

4. Execution / Behavior

Lowering

AllDistinct(values)
→ AllDistinctNode(collection)
→ PlanInstruction(AllDistinct, collectionSlot)


---

Runtime Evaluation (Incremental)

bool AllDistinctPartial(Span<int> values, Span<bool> bound)
{
    var seen = new HashSet<int>();

    for (int i = 0; i < values.Length; i++)
    {
        if (!bound[i]) continue;

        if (!seen.Add(values[i]))
            return false;
    }

    return true;
}


---

Generated Execution (Typed State)

if (!AllDistinctPartial(
    stackalloc int[] { state.s1, state.s2, state.s3 },
    stackalloc bool[] { state.s1_bound, state.s2_bound, state.s3_bound }))
{
    goto Fail;
}


---

Generated Execution (Fully Inlined)

if (state.s1_bound && state.s2_bound && state.s1 == state.s2) goto Fail;
if (state.s1_bound && state.s3_bound && state.s1 == state.s3) goto Fail;
if (state.s2_bound && state.s3_bound && state.s2 == state.s3) goto Fail;


---

5. Examples

DSL

AllDistinct(Map(board, s => s.Student))


---

IR

AllDistinctNode(
    Collection = MapNode(...),
    ElementType = typeof(int)
)


---

Plan

PlanInstruction(AllDistinct, slotId)


---

Generated Code

if (state.s1_bound && state.s2_bound && state.s1 == state.s2) goto Fail;
if (state.s1_bound && state.s3_bound && state.s1 == state.s3) goto Fail;
if (state.s2_bound && state.s3_bound && state.s2 == state.s3) goto Fail;