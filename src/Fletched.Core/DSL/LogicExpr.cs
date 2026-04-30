using Fletched.Core.IR;

namespace Fletched.Core.DSL;

/// <summary>
/// A typed wrapper around an <see cref="ExprNode"/> that provides the DSL surface
/// for building logical expressions. Operators are reinterpreted as:
/// <list type="bullet">
///   <item><c>==</c> → unification</item>
///   <item><c>&amp;&amp;</c> → logical conjunction</item>
///   <item><c>||</c> → logical disjunction</item>
/// </list>
/// </summary>
public readonly struct LogicExpr<T>
{
    internal readonly ExprNode Node;

    internal LogicExpr(ExprNode node) => Node = node;

    public static LogicExpr<bool> operator ==(LogicExpr<T> left, LogicExpr<T> right)
        => new(new UnifyNode(left.Node, right.Node));

    public static LogicExpr<bool> operator !=(LogicExpr<T> left, LogicExpr<T> right)
        => throw new NotSupportedException("Negation-as-failure is not supported in LogicExpr.");

    public static LogicExpr<T> operator &(LogicExpr<T> left, LogicExpr<T> right)
        => left.Node is ConjNode { Parts: var parts }
            ? new(new ConjNode([.. parts, right.Node]))
            : new(new ConjNode([left.Node, right.Node]));

    public static LogicExpr<T> operator |(LogicExpr<T> left, LogicExpr<T> right)
        => new(new DisjNode(left.Node, right.Node));

    // C# requires true/false operators when overloading & and | as logical operators.
    public static bool operator true(LogicExpr<T> _) => false;
    public static bool operator false(LogicExpr<T> _) => false;

    public override bool Equals(object? obj) => throw new NotSupportedException();
    public override int GetHashCode() => throw new NotSupportedException();
}
