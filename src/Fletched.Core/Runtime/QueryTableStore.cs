using System;
using System.Collections.Generic;

namespace Fletched.Core.Runtime;

/// <summary>Query-scoped memoization store for tabled predicate calls.</summary>
public sealed class QueryTableStore<TAnswer>
    where TAnswer : notnull
{
    private readonly Dictionary<TableKey, AnswerTable<TAnswer>> _tables = [];

    public bool TryGetTable(TableKey key, out AnswerTable<TAnswer>? table) =>
        _tables.TryGetValue(key, out table);

    public AnswerTable<TAnswer> GetOrAddTable(TableKey key, out bool isProducer)
    {
        if (_tables.TryGetValue(key, out AnswerTable<TAnswer>? existing))
        {
            isProducer = false;
            return existing;
        }

        var created = new AnswerTable<TAnswer>();
        _tables.Add(key, created);
        isProducer = true;
        return created;
    }

    public void Clear() => _tables.Clear();
}
