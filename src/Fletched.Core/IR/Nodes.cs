using System.Reflection;

namespace Fletched.Core.IR;

/// <summary>A logical variable identified by slot index and type.</summary>
public record VarNode(Type Type, int Id)
    : ExprNode(NodeKind.Var);

/// <summary>A constant value with a known type.</summary>
public record ConstNode(object? Value, Type Type)
    : ExprNode(NodeKind.Const);

/// <summary>Access to a typed field or property of a target expression.</summary>
public record FieldNode(
    ExprNode Target,
    MemberInfo Member,
    Type FieldType
) : ExprNode(NodeKind.Field);

/// <summary>Unification of two expressions.</summary>
public record UnifyNode(
    ExprNode Left,
    ExprNode Right
) : ExprNode(NodeKind.Unify);

/// <summary>Flattened conjunction of two or more expressions.</summary>
public record ConjNode(
    IReadOnlyList<ExprNode> Parts
) : ExprNode(NodeKind.Conj);

/// <summary>Binary disjunction of two expressions.</summary>
public record DisjNode(
    ExprNode Left,
    ExprNode Right
) : ExprNode(NodeKind.Disj);

/// <summary>An external constraint applied via a method call.</summary>
public record ConstraintNode(
    MethodInfo Method,
    ExprNode[] Arguments
) : ExprNode(NodeKind.Constraint);
