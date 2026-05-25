using System;
using System.Collections.Generic;
using System.Linq;

namespace Fletched.Core;

/// <summary>
/// Abstract logical list type used at DSL and type level.
/// No runtime allocation occurs during execution; all list operations are compiled
/// into structural unification and field access over typed state.
/// </summary>
public abstract record LogicList<T>
{
    /// <summary>Returns the empty logical list value.</summary>
    public static LogicList<T> Empty() => new LogicListEmpty<T>();

    /// <summary>Prepends a head element to a tail logical list.</summary>
    public static LogicList<T> Cons(T head, LogicList<T> tail)
    {
        if (tail is null)
            throw new ArgumentNullException(nameof(tail));

        return new LogicListCons<T>(head, tail);
    }

    /// <summary>Creates a concrete logical list from a sequence of values.</summary>
    public static LogicList<T> Create(params T[] items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        LogicList<T> result = new LogicListEmpty<T>();
        for (int itemIndex = items.Length - 1; itemIndex >= 0; itemIndex--)
            result = new LogicListCons<T>(items[itemIndex], result);

        return result;
    }

    /// <summary>Creates a concrete logical list from an enumerable sequence.</summary>
    public static LogicList<T> FromEnumerable(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        return items is T[] array
            ? Create(array)
            : Create(items.ToArray());
    }

    /// <summary>Creates a concrete logical list from zero or more values.</summary>
    public static LogicList<T> From(params T[] items) => Create(items);

    /// <summary>Creates a concrete logical list from an enumerable sequence.</summary>
    public static LogicList<T> From(IEnumerable<T> items) => FromEnumerable(items);
}

/// <summary>Represents the empty list.</summary>
public record LogicListEmpty<T> : LogicList<T>;

/// <summary>Represents a cons cell with a head element and a tail list.</summary>
public record LogicListCons<T>(T Head, LogicList<T> Tail) : LogicList<T>;

/// <summary>Non-generic entry point for logical list constructors.</summary>
public static class LogicList
{
    public static LogicList<T> Empty<T>() => LogicList<T>.Empty();

    public static LogicList<T> Cons<T>(T head, LogicList<T> tail) => LogicList<T>.Cons(head, tail);

    public static LogicList<T> From<T>(params T[] items) => LogicList<T>.From(items);

    public static LogicList<T> From<T>(IEnumerable<T> items) => LogicList<T>.From(items);
}
