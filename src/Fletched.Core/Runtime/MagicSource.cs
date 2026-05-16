using System;
using System.Collections.Generic;

namespace Fletched.Core.Runtime;

/// <summary>Query-scoped deduplicated storage for magic predicate tuples.</summary>
public sealed class MagicSource<TTuple>
    where TTuple : notnull
{
    private readonly List<TTuple> _tuples = [];
    private readonly HashSet<TTuple> _seen = [];

    public IReadOnlyList<TTuple> Tuples => _tuples;

    public bool TryAdd(TTuple tuple)
    {
        if (!_seen.Add(tuple))
            return false;

        _tuples.Add(tuple);
        return true;
    }
}
