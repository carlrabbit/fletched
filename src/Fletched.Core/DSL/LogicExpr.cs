using System;
using System.Collections.Generic;
using Fletched.Core.IR;

namespace Fletched.Core;

/// <summary>
/// A typed logical expression node used in predicate body DSL.
/// Operators build an <see cref="ExprNode"/> tree which the source generator reads.
/// </summary>
public readonly struct LogicExpr<T>
{
    internal readonly ExprNode? Node;

    public LogicExpr(ExprNode node) => Node = node;

    /// <summary>Creates a constant expression.</summary>
    public static LogicExpr<T> Constant(T value) =>
        new(new ConstNode(value, typeof(T)));

    /// <summary>Implicit conversion from a literal value.</summary>
    public static implicit operator LogicExpr<T>(T value) =>
        new(new ConstNode(value, typeof(T)));

    // ── Unification ──────────────────────────────────────────────────────────
    /// <summary>Logical unification (== in DSL maps to UnifyNode).</summary>
    public static LogicExpr<bool> operator ==(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new UnifyNode(left.Node!, right.Node!));

    /// <summary>Inequality constraint.</summary>
    public static LogicExpr<bool> operator !=(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ConstraintNode(
            typeof(LogicExprHelpers).GetMethod(nameof(LogicExprHelpers.NotEqual))!
                .MakeGenericMethod(typeof(T)),
            new ExprNode[] { left.Node!, right.Node! }));

    // ── Conjunction / Disjunction ─────────────────────────────────────────────
    // C# expands `a && b` as: LogicExpr<T>.false(a) ? a : (a & b)
    // So we need true/false operators plus & and | for && and || to work.

    public static bool operator true(LogicExpr<T> _) => false;
    public static bool operator false(LogicExpr<T> _) => false;

    /// <summary>Logical AND (&amp;&amp; in DSL).</summary>
    public static LogicExpr<T> operator &(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ConjNode(new ExprNode[] { left.Node!, right.Node! }));

    /// <summary>Logical OR (|| in DSL).</summary>
    public static LogicExpr<T> operator |(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new DisjNode(left.Node!, right.Node!));

    public override bool Equals(object? obj) =>
        throw new InvalidOperationException("Use == operator for DSL unification, not object equality.");

    public override int GetHashCode() =>
        throw new InvalidOperationException("LogicExpr<T> is a DSL type and cannot be used as a dictionary key.");
}

internal static class LogicExprHelpers
{
    public static bool NotEqual<T>(T a, T b) => !Equals(a, b);
}
