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

    // ── Comparisons ───────────────────────────────────────────────────────────
    // C# requires < with >, and <= with >= to be defined as pairs.

    /// <summary>Less-than constraint (&lt; in DSL).</summary>
    public static LogicExpr<bool> operator <(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ConstraintNode(
            typeof(LogicExprHelpers).GetMethod(nameof(LogicExprHelpers.LessThan))!
                .MakeGenericMethod(typeof(T)),
            new ExprNode[] { left.Node!, right.Node! }));

    /// <summary>Greater-than constraint (&gt; in DSL).</summary>
    public static LogicExpr<bool> operator >(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ConstraintNode(
            typeof(LogicExprHelpers).GetMethod(nameof(LogicExprHelpers.GreaterThan))!
                .MakeGenericMethod(typeof(T)),
            new ExprNode[] { left.Node!, right.Node! }));

    /// <summary>Less-than-or-equal constraint (&lt;= in DSL).</summary>
    public static LogicExpr<bool> operator <=(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ConstraintNode(
            typeof(LogicExprHelpers).GetMethod(nameof(LogicExprHelpers.LessThanOrEqual))!
                .MakeGenericMethod(typeof(T)),
            new ExprNode[] { left.Node!, right.Node! }));

    /// <summary>Greater-than-or-equal constraint (&gt;= in DSL).</summary>
    public static LogicExpr<bool> operator >=(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ConstraintNode(
            typeof(LogicExprHelpers).GetMethod(nameof(LogicExprHelpers.GreaterThanOrEqual))!
                .MakeGenericMethod(typeof(T)),
            new ExprNode[] { left.Node!, right.Node! }));

    // ── Arithmetic ────────────────────────────────────────────────────────────

    /// <summary>Arithmetic addition (+ in DSL). Returns a LogicExpr of the same type.</summary>
    public static LogicExpr<T> operator +(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ArithNode(ArithOp.Add, left.Node!, right.Node!));

    /// <summary>Arithmetic subtraction (- in DSL). Returns a LogicExpr of the same type.</summary>
    public static LogicExpr<T> operator -(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ArithNode(ArithOp.Subtract, left.Node!, right.Node!));

    /// <summary>Arithmetic multiplication (* in DSL). Returns a LogicExpr of the same type.</summary>
    public static LogicExpr<T> operator *(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ArithNode(ArithOp.Multiply, left.Node!, right.Node!));

    /// <summary>Arithmetic division (/ in DSL). Returns a LogicExpr of the same type.</summary>
    public static LogicExpr<T> operator /(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ArithNode(ArithOp.Divide, left.Node!, right.Node!));

    /// <summary>Arithmetic modulo (% in DSL). Returns a LogicExpr of the same type.</summary>
    public static LogicExpr<T> operator %(LogicExpr<T> left, LogicExpr<T> right) =>
        new(new ArithNode(ArithOp.Modulo, left.Node!, right.Node!));

    /// <summary>Arithmetic unary negation (-x in DSL). Returns a LogicExpr of the same type.</summary>
    public static LogicExpr<T> operator -(LogicExpr<T> value) =>
        new(new ArithNode(ArithOp.Subtract, new ConstNode(LogicExprHelpers.Zero<T>(), typeof(T)), value.Node!));

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
    public static T Zero<T>()
    {
        Type type = typeof(T);
        object value = type == typeof(byte) ? (byte)0
            : type == typeof(sbyte) ? (sbyte)0
            : type == typeof(short) ? (short)0
            : type == typeof(ushort) ? (ushort)0
            : type == typeof(int) ? 0
            : type == typeof(uint) ? 0u
            : type == typeof(long) ? 0L
            : type == typeof(ulong) ? 0UL
            : type == typeof(float) ? 0f
            : type == typeof(double) ? 0d
            : type == typeof(decimal) ? 0m
            : throw new InvalidOperationException($"Unary negation is only supported for numeric LogicExpr types. Type '{type.FullName}' is not supported.");

        return (T)value;
    }

    public static bool NotEqual<T>(T a, T b) => !Equals(a, b);
    public static bool LessThan<T>(T a, T b) => System.Collections.Generic.Comparer<T>.Default.Compare(a, b) < 0;
    public static bool GreaterThan<T>(T a, T b) => System.Collections.Generic.Comparer<T>.Default.Compare(a, b) > 0;
    public static bool LessThanOrEqual<T>(T a, T b) => System.Collections.Generic.Comparer<T>.Default.Compare(a, b) <= 0;
    public static bool GreaterThanOrEqual<T>(T a, T b) => System.Collections.Generic.Comparer<T>.Default.Compare(a, b) >= 0;
}
