using System;
using System.Collections.Generic;

namespace Fletched.Core.Runtime;

/// <summary>Query-scoped store of magic sources keyed by deterministic source identity.</summary>
public sealed class QueryMagicStore<TTuple>
    where TTuple : notnull
{
    private readonly Dictionary<string, MagicSource<TTuple>> _sources = new(StringComparer.Ordinal);

    public bool TryGetSource(string sourceIdentity, out MagicSource<TTuple>? source) =>
        _sources.TryGetValue(sourceIdentity, out source);

    public MagicSource<TTuple> GetOrAddSource(string sourceIdentity)
    {
        if (string.IsNullOrWhiteSpace(sourceIdentity))
            throw new ArgumentException("Source identity must not be null or whitespace.", nameof(sourceIdentity));

        if (_sources.TryGetValue(sourceIdentity, out MagicSource<TTuple>? existing))
            return existing;

        var created = new MagicSource<TTuple>();
        _sources.Add(sourceIdentity, created);
        return created;
    }

    public void Clear() => _sources.Clear();
}
