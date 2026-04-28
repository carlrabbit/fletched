# IR.md

## Overview

The Intermediate Representation (IR) models logical expressions as a typed, minimal, closed set of nodes. It is constructed from the DSL (`LogicExpr<T>`) and consumed by the code generator.

---

# Core Abstractions

## Logic Expression Wrapper

```csharp
readonly struct LogicExpr<T>
{
    internal readonly ExprNode Node;

    internal LogicExpr(ExprNode node) => Node = node;
}
```

---

## Node Kinds

```csharp
enum NodeKind
{
    Var,
    Const,
    Field,
    Unify,
    Conj,
    Disj,
    Constraint
}
```

---

## Base Node

```csharp
abstract record ExprNode(NodeKind Kind);
```

---

# Node Definitions

## Variable

```csharp
record VarNode(Type Type, int Id)
    : ExprNode(NodeKind.Var);
```

---

## Constant

```csharp
record ConstNode(object? Value, Type Type)
    : ExprNode(NodeKind.Const);
```

---

## Field Access

```csharp
record FieldNode(
    ExprNode Target,
    MemberInfo Member,
    Type FieldType
) : ExprNode(NodeKind.Field);
```

---

## Unification

```csharp
record UnifyNode(
    ExprNode Left,
    ExprNode Right
) : ExprNode(NodeKind.Unify);
```

---

## Conjunction (Flattened)

```csharp
record ConjNode(
    IReadOnlyList<ExprNode> Parts
) : ExprNode(NodeKind.Conj);
```

---

## Disjunction

```csharp
record DisjNode(
    ExprNode Left,
    ExprNode Right
) : ExprNode(NodeKind.Disj);
```

---

## Constraint

```csharp
record ConstraintNode(
    MethodInfo Method,
    ExprNode[] Arguments
) : ExprNode(NodeKind.Constraint);
```

---

# DSL → IR Mapping

## Variables

### Terminal Variable

```csharp
Body(TerminalVar<string> name)
```

```text
VarNode(Type=string, Id=0)
```

---

### Fresh Variables

```csharp
With<User, Admin>((user, admin) => ...)
```

```text
VarNode(User, Id=1)
VarNode(Admin, Id=2)
```

---

## Constants

```csharp
user.Name == "Alice"
```

```text
ConstNode("Alice", string)
```

---

## Field Access

```csharp
user.Name
```

```text
FieldNode(
    Target = Var(user),
    Member = User.Name,
    FieldType = string
)
```

---

## Unification

```csharp
user.Name == name
```

```text
UnifyNode(
    Field(user, Name),
    Var(name)
)
```

---

## Conjunction

```csharp
A && B && C
```

```text
ConjNode([A, B, C])
```

---

## Disjunction

```csharp
A || B
```

```text
DisjNode(A, B)
```

---

## Constraint

```csharp
user.Name.StartsWith("A")
```

```text
ConstraintNode(
    Method = string.StartsWith,
    Arguments = [
        Field(user, Name),
        Const("A")
    ]
)
```

---

# Examples

## Example 1

### DSL

```csharp
user.Name == name
```

### IR

```text
Unify(
    Field(user, Name),
    Var(name)
)
```

---

## Example 2

### DSL

```csharp
user.Name == name &&
user.Login == admin.Login
```

### IR

```text
Conj([
    Unify(
        Field(user, Name),
        Var(name)
    ),
    Unify(
        Field(user, Login),
        Field(admin, Login)
    )
])
```

---

## Example 3

### DSL

```csharp
user.Name == "Alice" || user.Name == "Bob"
```

### IR

```text
Disj(
    Unify(Field(user, Name), Const("Alice")),
    Unify(Field(user, Name), Const("Bob"))
)
```

---

## Example 4

### DSL

```csharp
With<User>((user) =>
    user.Name == name &&
    user.Login == admin.Login
)
```

### IR

```text
Conj([
    Unify(
        Field(user, Name),
        Var(name)
    ),
    Unify(
        Field(user, Login),
        Field(admin, Login)
    )
])
```

---

## Example 5 (Nested Field Access)

### DSL

```csharp
user.Address.City == name
```

### IR

```text
Unify(
    Field(
        Field(user, Address),
        City
    ),
    Var(name)
)
```

---

## Example 6 (Constraint + Unification)

### DSL

```csharp
user.Name.StartsWith("A") &&
user.Name == name
```

### IR

```text
Conj([
    Constraint(
        Method = string.StartsWith,
        Arguments = [
            Field(user, Name),
            Const("A")
        ]
    ),
    Unify(
        Field(user, Name),
        Var(name)
    )
])
```

---

## Example 7 (Disjunction + Conjunction)

### DSL

```csharp
(user.Name == "Alice" || user.Name == "Bob") &&
user.Login == admin.Login
```

### IR

```text
Conj([
    Disj(
        Unify(Field(user, Name), Const("Alice")),
        Unify(Field(user, Name), Const("Bob"))
    ),
    Unify(
        Field(user, Login),
        Field(admin, Login)
    )
])
```

---

# Structural Properties

- Conjunctions are flattened into lists
- Disjunctions are binary
- Field access is compositional (nested `FieldNode`)
- Variables are identified by slot index (`Id`)
- Types are preserved on all nodes

---

# End of Document
