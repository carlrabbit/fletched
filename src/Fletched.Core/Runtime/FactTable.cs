namespace Fletched.Core.Runtime;

/// <summary>
/// In-memory table of fact instances of type <typeparamref name="T"/>.
/// </summary>
public sealed class FactTable<T>
{
    public T[] Data { get; }

    public FactTable(T[] data) => Data = data;

    public FactTable() => Data = Array.Empty<T>();
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
