IR.md


---

1. Overview

Defines the typed intermediate representation (IR) for logical expressions derived from the DSL.
The IR is declarative, normalized, and independent of execution strategy.


---

2. Core Concepts / Data Structures

2.1 Base Node

abstract record ExprNode;


---

2.2 Node Kinds

enum ExprNodeKind
{
    Var,
    Const,
    Field,
    Unify,
    Conj,
    Disj,
    Constraint
}


---

2.3 Variable

record VarNode(
    string Name,
    Type Type
) : ExprNode;

Represents a logical variable

Corresponds to DSL variables (TerminalVar<T>, lambda parameters)



---

2.4 Constant

record ConstNode(
    object? Value,
    Type Type
) : ExprNode;


---

2.5 Field Access

record FieldNode(
    ExprNode Target,
    MemberInfo Member,
    Type FieldType
) : ExprNode;

Member MUST reference a property or field

Target MUST be a VarNode or another FieldNode



---

2.6 Unification

record UnifyNode(
    ExprNode Left,
    ExprNode Right
) : ExprNode;

Represents logical unification (== in DSL)



---

2.7 Conjunction

record ConjNode(
    IReadOnlyList<ExprNode> Parts
) : ExprNode;

Represents logical AND

MUST be flattened (see invariants)



---

2.8 Disjunction

record DisjNode(
    ExprNode Left,
    ExprNode Right
) : ExprNode;

Represents logical OR



---

2.9 Constraint

record ConstraintNode(
    MethodInfo Method,
    IReadOnlyList<ExprNode> Arguments
) : ExprNode;

Represents boolean method invocation

MUST return bool



---

3. Rules and Invariants

3.1 Typing

Every ExprNode MUST have a statically known Type

UnifyNode.Left.Type MUST equal UnifyNode.Right.Type

ConstraintNode.Method.ReturnType MUST be bool



---

3.2 Variable Scope

Each VarNode is uniquely identified by (Name, Type) within a predicate

Variables are immutable identifiers



---

3.3 Conjunction Normalization

Nested conjunctions MUST be flattened


Invalid:

Conj(A, Conj(B, C))

Valid:

Conj(A, B, C)


---

3.4 Field Access

FieldNode.Member MUST belong to Target.Type

Only instance members are allowed

Static members are not allowed



---

3.5 Constraints

Constraints MUST be side-effect free

Arguments MUST be fully evaluable expressions

No assignment or mutation is allowed



---

3.6 Disjunction Structure

DisjNode MUST be binary

Nested disjunctions are allowed



---

3.7 No Execution Semantics

IR MUST NOT contain:

slots

indices

control flow constructs

backtracking logic




---

4. Execution / Behavior

4.1 Logical Meaning

UnifyNode represents bidirectional equality

ConjNode represents conjunction (all parts must succeed)

DisjNode represents branching (either side may succeed)

ConstraintNode represents filtering (must evaluate to true)



---

4.2 Evaluation Order

Conjunction order is preserved but not semantically significant

Disjunction order is preserved



---

5. Examples


---

5.1 Simple Unification

DSL

user.Name == name

IR

Unify(
  Field(Var(user), User.Name),
  Var(name)
)


---

5.2 Conjunction

DSL

user.Name == name &&
user.Login == admin.Login

IR

Conj(
  Unify(
    Field(Var(user), User.Name),
    Var(name)
  ),
  Unify(
    Field(Var(user), User.Login),
    Field(Var(admin), Admin.Login)
  )
)


---

5.3 Disjunction

DSL

A || B

IR

Disj(
  A,
  B
)


---

5.4 Constraint

DSL

user.Name.StartsWith("A")

IR

Constraint(
  Method = string.StartsWith,
  Arguments = [
    Field(Var(user), User.Name),
    Const("A")
  ]
)


---

5.5 Nested Field Access

DSL

user.Address.City == city

IR

Unify(
  Field(
    Field(Var(user), User.Address),
    Address.City
  ),
  Var(city)
)