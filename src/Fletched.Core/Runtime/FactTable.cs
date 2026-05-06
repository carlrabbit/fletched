using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fletched.Core.Runtime;

/// <summary>
/// In-memory table of fact instances of type <typeparamref name="T"/>.
/// </summary>
public sealed class FactTable<T>
{
    private readonly Dictionary<string, FactIndex> _indexes = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public T[] Data { get; }

    public FactTable(T[] data) => Data = data;

    public FactTable() => Data = Array.Empty<T>();

    public bool TryGetIndex(string memberName, object? key, out int[] indices)
    {
        FactIndex index = GetOrCreateIndex(memberName);
        return index.TryGetIndices(key, out indices);
    }

    private FactIndex GetOrCreateIndex(string memberName)
    {
        lock (_sync)
        {
            if (_indexes.TryGetValue(memberName, out FactIndex? existing))
                return existing;

            FactIndex created = BuildIndex(memberName);
            _indexes[memberName] = created;
            return created;
        }
    }

    private FactIndex BuildIndex(string memberName)
    {
        MemberInfo member = (MemberInfo?)typeof(T).GetProperty(memberName)
            ?? typeof(T).GetField(memberName)
            ?? throw new InvalidOperationException($"Fact type '{typeof(T).Name}' does not contain member '{memberName}'.");

        var buckets = new Dictionary<object, List<int>>();
        List<int>? nullBucket = null;

        for (int index = 0; index < Data.Length; index++)
        {
            object? key = GetMemberValue(member, Data[index]);
            if (key is null)
            {
                nullBucket ??= [];
                nullBucket.Add(index);
                continue;
            }

            if (!buckets.TryGetValue(key, out List<int>? matches))
            {
                matches = [];
                buckets[key] = matches;
            }

            matches.Add(index);
        }

        var frozenBuckets = new Dictionary<object, int[]>(buckets.Count);
        foreach (KeyValuePair<object, List<int>> bucket in buckets)
            frozenBuckets[bucket.Key] = bucket.Value.ToArray();

        return new FactIndex(frozenBuckets, nullBucket?.ToArray());
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

    private sealed class FactIndex(Dictionary<object, int[]> buckets, int[]? nullBucket)
    {
        public bool TryGetIndices(object? key, out int[] indices)
        {
            if (key is null)
            {
                if (nullBucket is not null)
                {
                    indices = nullBucket;
                    return true;
                }

                indices = Array.Empty<int>();
                return false;
            }

            return buckets.TryGetValue(key, out indices!);
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
