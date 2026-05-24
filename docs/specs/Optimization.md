Optimization.md

1. Overview

Defines compile-time transformations applied to the Execution Plan to improve runtime performance.
Optimizations operate on "ExecutionPlan" and preserve semantic equivalence.

---

2. Core Concepts / Data Structures

Optimization Pipeline

interface IPlanOptimization
{
    PlanProgram Apply(PlanProgram program);
}

sealed class OptimizationPipeline
{
    IReadOnlyList<IPlanOptimization> Passes;

    PlanProgram Run(PlanProgram program);
}

---

Analysis Data

record AccessSet(
    IReadOnlyCollection<int> Reads,
    IReadOnlyCollection<int> Writes
);

---

3. Rules and Invariants

General

- All transformations must preserve logical equivalence.
- No optimization introduces additional bindings.
- No optimization removes required backtracking points.
- Slot identities remain stable.
- Control flow graph remains valid (all labels resolvable).

---

Conjunction Reordering

- Applicable only within "PlanSequence".
- Reordering is allowed if:
  - No data dependency violation occurs.
  - No variable is used before it may be bound.

Dependency Rule:
If Step B reads Slot S, and Step A may bind S,
then A must precede B.

---

Index Selection

- The current Roslyn plan IR does not encode a separate indexed loop source.
- The optimization pass instead promotes loop key-filter checks such as:
  - "Field(Slot(loopVar), Member) == Slot(keySlot)"
  - "Field(Slot(loopVar), Member) == Const(value)"
- These checks are moved as early as dependency ordering allows so the loop fails fast.

---

Constraint Hoisting

- "PlanConstraint" may be moved earlier in a sequence if:
  - All its argument slots are bound at the new position.
  - It does not depend on loop-local bindings.

---

Redundant Unification Elimination

- Remove "PlanUnify(X, X)".

- Replace:

Unify(Const(a), Const(b))

with:

- "Fail" if "a != b"
- no-op if "a == b"

---

Dead Code Elimination

- Remove instructions after unconditional "Fail".
- Remove unreachable blocks.

Note: Unused slot-binding removal is not currently implemented. Only provable instruction-level
failure and block-level reachability are handled.

---

Loop Specialization

- The pass detects loop-invariant bodies as an analysis step.
- No structural loop transformation or loop removal is performed.
- The current lowering keeps the original loop structure because the existing plan IR
  does not carry enough cardinality information to remove the loop safely.

Note: This is an analysis-only pass with no current code-transformation effect.

---

Temporary Value Hoisting

- Reuse identical "FieldValue" expressions within read-only instruction segments.
- Hoisted values are stored in temporary slots via "AssignInstr".

Field(Slot(user), Name) → hoisted to temp

---

4. Roadmap Optimizations

Predicate Call Inlining (Milestone 09 scope)

- Inline "PlanCall" when all of the following are satisfied:
  - Target predicate is non-recursive.
  - Target predicate has a single execution-plan block (no disjunctions).
  - Target predicate body contains only deterministic instructions (no loops, no nested calls,
    no negation-as-failure instructions).
  - Call argument count is within the configured threshold (default: 8).
  - Target predicate is not tabled.
- Conservative fallback: any call that does not satisfy all eligibility criteria is left as a
  "CallInstr" and executed via the normal predicate-invocation state machine.
- Calls inside "NotInstr" subgoal lists are never traversed for inlining.
- Recursive and mutually recursive calls are never inlined.
- Tabled calls are never inlined.
- Slot remapping: callee parameter slots are mapped to caller argument slots; additional callee
  slots are mapped to fresh caller slots.
- Inlining preserves logical result sets and caller result projection.

---

5. Execution / Behavior

Pipeline Order

1. NormalizeSequence
2. RemoveRedundantUnify
3. DependencyAnalysis
4. ReorderConjunction
5. IndexSelection
6. ConstraintHoisting
7. DeadCodeElimination
8. LoopSpecialization
9. TempHoisting
10. PredicateCallInlining

---

Dependency Analysis

- Compute per-instruction:
  - Read slots
  - Write slots

record AccessSet(
    IReadOnlySet<int> Reads,
    IReadOnlySet<int> Writes
);

---

Reordering Algorithm

- Topological sort of "PlanSequence" steps
- Respect dependency constraints
- Prefer:
  - constraints first
  - loop key-filter checks
  - bound-variable unifications
  - loops last

---

6. Examples

Example 1: Redundant Unification

Before

Unify(Slot(0), Slot(0))

After

(no-op)

---

Example 2: Constant Unification

Before

Unify(Const("A"), Const("B"))

After

Fail

---

Example 3: Early Loop Key Filter

Before

Loop(
  Slot = user,
  Source = FullScan(User),
  Body:
    Unify(Field(user, Login), Slot(name))
)

After

Loop(
  Slot = user,
  Body:
    Unify(Field(user, Login), name)   // moved before unrelated work
)

---

Example 4: Constraint Hoisting

Before

Sequence:
  Loop(user)
  Constraint(user.Name.StartsWith("A"))

After

Sequence:
  Constraint(name.StartsWith("A"))
  Loop(user)

---

Example 5: Temporary Hoisting

Before

Unify(Field(user, Name), Slot(name))
Constraint(Field(user, Name).StartsWith("A"))

After

temp = Field(user, Name)
Unify(temp, Slot(name))
Constraint(temp.StartsWith("A"))

---

Example 6: Dead Code

Before

Unify(Const(1), Const(2))
Unify(Slot(x), Const(3))

After

Fail

---

Example 7: Reordering

Before

Sequence:
  Loop(User)
  Unify(user.Name, name)
  Constraint(name.StartsWith("A"))

After

Sequence:
  Constraint(name.StartsWith("A"))
  Loop(User indexed by name)

---

Example 8: Predicate Call Inlining

Before

CallInstr(SimpleName/1, argSlots=[2])   // callee: Unify(Slot(0), Const("admin"))

After (inlined)

Unify(Slot(2), Const("admin"))          // slot 0 of callee remapped to argSlots[0]=2

---

End of Document
