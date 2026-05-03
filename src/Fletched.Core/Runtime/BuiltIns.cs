using System;
using System.Collections.Generic;

namespace Fletched.Core.Runtime;

/// <summary>
/// Runtime helpers for built-in predicates.
/// These methods are called from generated predicate execution code.
/// </summary>
public static class BuiltIns
{
    /// <summary>
    /// Returns <see langword="true"/> if all bound elements in <paramref name="values"/> are pairwise distinct.
    /// Unbound elements (where <paramref name="bound"/>[i] is <see langword="false"/>) are ignored.
    /// </summary>
    /// <typeparam name="T">The element type; must support equality comparison.</typeparam>
    /// <param name="values">The values to check.</param>
    /// <param name="bound">Flags indicating which elements are currently bound. Must have the same length as <paramref name="values"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> and <paramref name="bound"/> have different lengths.</exception>
    public static bool AllDistinctPartial<T>(T[] values, bool[] bound)
    {
        if (values.Length != bound.Length)
            throw new ArgumentException("values and bound arrays must have the same length.");

        var seen = new HashSet<T>();

        for (int i = 0; i < values.Length; i++)
        {
            if (!bound[i]) continue;

            if (!seen.Add(values[i]))
                return false;
        }

        return true;
    }
}
