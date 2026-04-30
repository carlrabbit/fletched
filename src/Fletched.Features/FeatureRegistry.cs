using Fletched.Abstractions;

namespace Fletched.Features;

/// <summary>Central registry of all registered <see cref="IFeatureModule"/> instances.</summary>
public static class FeatureRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<IFeatureModule> Modules = [];

    /// <summary>All registered feature modules.</summary>
    public static IReadOnlyList<IFeatureModule> All
    {
        get
        {
            lock (SyncRoot)
                return [.. Modules];
        }
    }

    /// <summary>Registers a feature module.</summary>
    public static void Register(IFeatureModule module)
    {
        lock (SyncRoot)
            Modules.Add(module);
    }

    /// <summary>Removes all registered feature modules. Intended for use in tests only.</summary>
    public static void Clear()
    {
        lock (SyncRoot)
            Modules.Clear();
    }
}
