using System;
using System.Threading;
using Fletched.Core.IR;

namespace Fletched.Core;

/// <summary>
/// Provides the <c>With&lt;T&gt;</c> scoped variable introduction construct for predicate DSL bodies.
/// </summary>
public static class Logic
{
    private static int _counter;

    private static string NextName() => $"_w{Interlocked.Increment(ref _counter)}";

    /// <summary>Introduces one scoped fact variable of type <typeparamref name="T1"/>.</summary>
    public static LogicExpr<bool> With<T1>(Func<Proxy<T1>, LogicExpr<bool>> body)
    {
        var v1 = new Proxy<T1>(NextName());
        LogicExpr<bool> result = body(v1);
        return new LogicExpr<bool>(new WithNode(
            new[] { new VarNode(v1.VariableName, typeof(T1)) },
            result.Node!));
    }

    /// <summary>Introduces two scoped fact variables.</summary>
    public static LogicExpr<bool> With<T1, T2>(Func<Proxy<T1>, Proxy<T2>, LogicExpr<bool>> body)
    {
        var v1 = new Proxy<T1>(NextName());
        var v2 = new Proxy<T2>(NextName());
        LogicExpr<bool> result = body(v1, v2);
        return new LogicExpr<bool>(new WithNode(
            new[] { new VarNode(v1.VariableName, typeof(T1)), new VarNode(v2.VariableName, typeof(T2)) },
            result.Node!));
    }

    /// <summary>Introduces three scoped fact variables.</summary>
    public static LogicExpr<bool> With<T1, T2, T3>(
        Func<Proxy<T1>, Proxy<T2>, Proxy<T3>, LogicExpr<bool>> body)
    {
        var v1 = new Proxy<T1>(NextName());
        var v2 = new Proxy<T2>(NextName());
        var v3 = new Proxy<T3>(NextName());
        LogicExpr<bool> result = body(v1, v2, v3);
        return new LogicExpr<bool>(new WithNode(
            new[]
            {
                new VarNode(v1.VariableName, typeof(T1)),
                new VarNode(v2.VariableName, typeof(T2)),
                new VarNode(v3.VariableName, typeof(T3)),
            },
            result.Node!));
    }

    /// <summary>
    /// Built-in AllDistinct constraint: asserts that all elements in the collection are pairwise distinct.
    /// Returns a boolean <see cref="LogicExpr{T}"/> that fails if any two bound elements are equal.
    /// </summary>
    public static LogicExpr<bool> AllDistinct<T>(LogicExpr<T[]> values) =>
        new(new AllDistinctNode(values.Node!, typeof(T)));

    /// <summary>
    /// Creates an empty logical list of type <typeparamref name="T"/>.
    /// Produces a <see cref="ListEmptyNode"/> in the IR.
    /// </summary>
    public static LogicExpr<LogicList<T>> Empty<T>() =>
        new(new ListEmptyNode(typeof(T)));

    /// <summary>
    /// Creates an empty logical list pattern of type <typeparamref name="T"/>.
    /// Produces a <see cref="ListEmptyNode"/> in the IR.
    /// </summary>
    public static LogicExpr<LogicList<T>> List<T>() => Empty<T>();

    /// <summary>
    /// Creates an exact logical list pattern from zero or more literal values.
    /// Produces nested <see cref="ListConsNode"/> and <see cref="ListEmptyNode"/> values in the IR.
    /// </summary>
    public static LogicExpr<LogicList<T>> List<T>(params T[] items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        LogicExpr<LogicList<T>> result = Empty<T>();
        for (int index = items.Length - 1; index >= 0; index--)
            result = Cons(items[index], result);

        return result;
    }

    /// <summary>
    /// Creates an exact logical list pattern from zero or more symbolic elements.
    /// Produces nested <see cref="ListConsNode"/> and <see cref="ListEmptyNode"/> values in the IR.
    /// </summary>
    public static LogicExpr<LogicList<T>> List<T>(params LogicExpr<T>[] items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        LogicExpr<LogicList<T>> result = Empty<T>();
        for (int index = items.Length - 1; index >= 0; index--)
            result = Cons(items[index], result);

        return result;
    }

    /// <summary>
    /// Creates a cons cell logical list with the given head and tail.
    /// Produces a <see cref="ListConsNode"/> in the IR.
    /// </summary>
    public static LogicExpr<LogicList<T>> Cons<T>(LogicExpr<T> head, LogicExpr<LogicList<T>> tail) =>
        new(new ListConsNode(head.Node!, tail.Node!));

    /// <summary>
    /// Negation-as-failure. <c>Not(G)</c> succeeds iff <c>G</c> produces no solutions.
    /// All variables referenced inside <c>goal</c> must be bound before this expression is evaluated.
    /// Produces a <see cref="NotNode"/> in the IR.
    /// </summary>
    public static LogicExpr<bool> Not(LogicExpr<bool> goal) =>
        new(new NotNode(goal.Node!));
}
