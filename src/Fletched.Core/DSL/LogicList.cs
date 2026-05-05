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
    /// <summary>Creates a concrete logical list from a sequence of values.</summary>
    public static LogicList<T> Create(params T[] items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        LogicList<T> result = new LogicListEmpty<T>();
        for (int index = items.Length - 1; index >= 0; index--)
            result = new LogicListCons<T>(items[index], result);

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
}

/// <summary>Represents the empty list.</summary>
public record LogicListEmpty<T> : LogicList<T>;

/// <summary>Represents a cons cell with a head element and a tail list.</summary>
public record LogicListCons<T>(T Head, LogicList<T> Tail) : LogicList<T>;
