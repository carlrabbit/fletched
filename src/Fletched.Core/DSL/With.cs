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
    /// Projects each element of a collection using a field selector.
    /// Returns a <see cref="LogicExpr{T}"/> of array type containing one projected value per source element.
    /// </summary>
    /// <typeparam name="T">The element type of the source collection.</typeparam>
    /// <typeparam name="TResult">The element type of the projected collection.</typeparam>
    /// <param name="collection">The source collection expression.</param>
    /// <param name="selector">A selector applied to each element via a proxy.</param>
    public static LogicExpr<TResult[]> Map<T, TResult>(LogicExpr<T[]> collection, Func<Proxy<T>, LogicExpr<TResult>> selector)
    {
        var elementProxy = new Proxy<T>(NextName());
        LogicExpr<TResult> selectorExpr = selector(elementProxy);
        return new LogicExpr<TResult[]>(new MapNode(
            collection.Node!,
            new VarNode(elementProxy.VariableName, typeof(T)),
            selectorExpr.Node!,
            typeof(T),
            typeof(TResult)));
    }
}
