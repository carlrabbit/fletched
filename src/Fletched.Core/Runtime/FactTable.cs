using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Fletched.Core.Runtime;

/// <summary>
/// In-memory table of fact instances of type <typeparamref name="T"/>.
/// </summary>
public readonly record struct GeneratedFactIndexAccessor<T>(string Name, Func<T, object?> GetValue);

/// <summary>
/// Typed generated equality index descriptor.
/// </summary>
public sealed record GeneratedFactIndexAccessor<TFact, TKey>(
    string Name,
    ImmutableArray<string> Members,
    Func<TFact, TKey> GetKey);

/// <summary>
/// Typed generated range index descriptor.
/// </summary>
public sealed record GeneratedFactRangeIndexAccessor<TFact, TKey>(
    string Name,
    string Member,
    Func<TFact, TKey> GetKey)
    where TKey : IComparable<TKey>;

/// <summary>
/// In-memory table of fact instances of type <typeparamref name="T"/>.
/// </summary>
public sealed class FactTable<T>
{
    private readonly Dictionary<string, EqualityFactIndex> _indexes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _rangeIndexes = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private T[] _data;

    public T[] Data => _data;

    public FactTable(T[] data) => _data = data;

    public FactTable() => _data = Array.Empty<T>();

    public bool TryGetIndex(string memberName, object? key, out int[] indices)
    {
        EqualityFactIndex index = GetOrCreateIndex(memberName, getValue: null);
        return index.TryGetIndices(key, out indices);
    }

    public bool TryGetIndex(GeneratedFactIndexAccessor<T> accessor, object? key, out int[] indices)
    {
        if (string.IsNullOrWhiteSpace(accessor.Name))
            throw new ArgumentException("Accessor name must not be null or whitespace.", nameof(accessor));

        if (accessor.GetValue is null)
            throw new ArgumentException("Accessor getter must not be null.", nameof(accessor));

        EqualityFactIndex index = GetOrCreateIndex(accessor.Name, accessor.GetValue);
        return index.TryGetIndices(key, out indices);
    }

    public bool TryGetIndex<TKey>(GeneratedFactIndexAccessor<T, TKey> accessor, TKey key, out int[] indices)
    {
        if (accessor.GetKey is null)
            throw new ArgumentException("Accessor getter must not be null.", nameof(accessor));

        EqualityFactIndex index = GetOrCreateIndex(accessor.Name, fact => accessor.GetKey(fact));
        return index.TryGetIndices(key, out indices);
    }

    public bool TryGetRange<TKey>(
        GeneratedFactRangeIndexAccessor<T, TKey> accessor,
        TKey? lower,
        bool lowerInclusive,
        TKey? upper,
        bool upperInclusive,
        out int[] indices)
        where TKey : IComparable<TKey>
    {
        if (accessor.GetKey is null)
            throw new ArgumentException("Accessor getter must not be null.", nameof(accessor));

        RangeFactIndex<TKey> index = GetOrCreateRangeIndex(accessor);
        return index.TryGetIndices(
            lower is not null,
            lower!,
            lowerInclusive,
            upper is not null,
            upper!,
            upperInclusive,
            out indices);
    }

    public bool TryGetRange<TKey>(
        GeneratedFactRangeIndexAccessor<T, TKey> accessor,
        bool hasLower,
        TKey lower,
        bool lowerInclusive,
        bool hasUpper,
        TKey upper,
        bool upperInclusive,
        out int[] indices)
        where TKey : IComparable<TKey>
    {
        if (accessor.GetKey is null)
            throw new ArgumentException("Accessor getter must not be null.", nameof(accessor));

        RangeFactIndex<TKey> index = GetOrCreateRangeIndex(accessor);
        return index.TryGetIndices(hasLower, lower, lowerInclusive, hasUpper, upper, upperInclusive, out indices);
    }

    public void Add(T fact)
    {
        lock (_sync)
        {
            int rowId = _data.Length;
            Array.Resize(ref _data, rowId + 1);
            _data[rowId] = fact;

            foreach (EqualityFactIndex index in _indexes.Values)
                index.Add(fact, rowId);

            foreach (object rangeIndex in _rangeIndexes.Values)
                ((IFactIndexUpdater<T>)rangeIndex).Add(fact, rowId);
        }
    }

    public void AddRange(IEnumerable<T> facts)
    {
        foreach (T fact in facts)
            Add(fact);
    }

    public void RebuildIndexes()
    {
        lock (_sync)
        {
            foreach (EqualityFactIndex index in _indexes.Values)
                index.Rebuild(_data);

            foreach (object rangeIndex in _rangeIndexes.Values)
                ((IFactIndexUpdater<T>)rangeIndex).Rebuild(_data);
        }
    }

    private EqualityFactIndex GetOrCreateIndex(string memberName, Func<T, object?>? getValue)
    {
        lock (_sync)
        {
            if (_indexes.TryGetValue(memberName, out EqualityFactIndex? existing))
                return existing;

            EqualityFactIndex created = BuildIndex(memberName, getValue);
            _indexes[memberName] = created;
            return created;
        }
    }

    private RangeFactIndex<TKey> GetOrCreateRangeIndex<TKey>(GeneratedFactRangeIndexAccessor<T, TKey> accessor)
        where TKey : IComparable<TKey>
    {
        lock (_sync)
        {
            if (_rangeIndexes.TryGetValue(accessor.Name, out object? existing))
                return (RangeFactIndex<TKey>)existing;

            var created = new RangeFactIndex<TKey>(accessor.GetKey);
            created.Rebuild(_data);
            _rangeIndexes[accessor.Name] = created;
            return created;
        }
    }

    private EqualityFactIndex BuildIndex(string memberName, Func<T, object?>? getValue)
    {
        getValue ??= CreateReflectionAccessor(memberName);
        var index = new EqualityFactIndex(getValue);
        index.Rebuild(_data);
        return index;
    }

    private static Func<T, object?> CreateReflectionAccessor(string memberName)
    {
        MemberInfo member = (MemberInfo?)typeof(T).GetProperty(memberName)
            ?? typeof(T).GetField(memberName)
            ?? throw new InvalidOperationException($"Fact type '{typeof(T).Name}' does not contain member '{memberName}'.");

        return value => GetMemberValue(member, value);
    }

    private static object? GetMemberValue(MemberInfo member, T value)
    {
        return member switch
        {
            PropertyInfo property => property.GetValue(value),
            FieldInfo field => field.GetValue(value),
            _ => throw new InvalidOperationException($"Unsupported member kind '{member.MemberType}'."),
        };
    }

    private interface IFactIndexUpdater<in TFact>
    {
        void Add(TFact fact, int rowId);
        void Rebuild(TFact[] data);
    }

    private sealed class EqualityFactIndex(Func<T, object?> getValue) : IFactIndexUpdater<T>
    {
        private readonly Dictionary<object, List<int>> _buckets = new();
        private List<int>? _nullBucket;

        public void Add(T fact, int rowId)
        {
            object? key = getValue(fact);
            if (key is null)
            {
                _nullBucket ??= [];
                _nullBucket.Add(rowId);
                return;
            }

            if (!_buckets.TryGetValue(key, out List<int>? matches))
            {
                matches = [];
                _buckets[key] = matches;
            }

            matches.Add(rowId);
        }

        public void Rebuild(T[] data)
        {
            _buckets.Clear();
            _nullBucket = null;

            for (int index = 0; index < data.Length; index++)
                Add(data[index], index);
        }

        public bool TryGetIndices(object? key, out int[] indices)
        {
            if (key is null)
            {
                if (_nullBucket is not null)
                {
                    indices = [.. _nullBucket];
                    return true;
                }

                indices = Array.Empty<int>();
                return false;
            }

            if (_buckets.TryGetValue(key, out List<int>? matches))
            {
                indices = [.. matches];
                return true;
            }

            indices = Array.Empty<int>();
            return false;
        }
    }

    private sealed class RangeFactIndex<TKey>(Func<T, TKey> getValue) : IFactIndexUpdater<T>
        where TKey : IComparable<TKey>
    {
        private readonly SortedDictionary<TKey, List<int>> _buckets = new();

        public void Add(T fact, int rowId)
        {
            TKey key = getValue(fact);
            if (key is null)
                return;

            if (!_buckets.TryGetValue(key, out List<int>? matches))
            {
                matches = [];
                _buckets[key] = matches;
            }

            matches.Add(rowId);
        }

        public void Rebuild(T[] data)
        {
            _buckets.Clear();
            for (int index = 0; index < data.Length; index++)
                Add(data[index], index);
        }

        public bool TryGetIndices(
            bool hasLower,
            TKey lower,
            bool lowerInclusive,
            bool hasUpper,
            TKey upper,
            bool upperInclusive,
            out int[] indices)
        {
            if (!hasLower && !hasUpper)
            {
                indices = Array.Empty<int>();
                return false;
            }

            List<int> matches = [];
            foreach (KeyValuePair<TKey, List<int>> entry in _buckets)
            {
                TKey key = entry.Key;
                List<int> rowIds = entry.Value;
                if (hasLower)
                {
                    int lowerComparison = key.CompareTo(lower);
                    if (lowerComparison < 0 || (lowerComparison == 0 && !lowerInclusive))
                        continue;
                }

                if (hasUpper)
                {
                    int upperComparison = key.CompareTo(upper);
                    if (upperComparison > 0 || (upperComparison == 0 && !upperInclusive))
                        continue;
                }

                matches.AddRange(rowIds);
            }

            if (matches.Count == 0)
            {
                indices = Array.Empty<int>();
                return false;
            }

            indices = [.. matches.OrderBy(index => index)];
            return true;
        }
    }
}

/// <summary>
/// In-memory table with an additional dictionary index on a key field.
/// </summary>
public sealed class FactTable<T, TKey> where TKey : notnull
{
    public T[] Data { get; }

    /// <summary>Maps key values to arrays of indices into <see cref="Data"/>.</summary>
    public Dictionary<TKey, int[]> Index { get; }

    public FactTable(T[] data, Dictionary<TKey, int[]> index)
    {
        Data = data;
        Index = index;
    }
}
