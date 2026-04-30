using System.Collections.Concurrent;

namespace Fletched.Emitters;

/// <summary>Central registry of all registered <see cref="ICodeEmitter"/> instances.</summary>
public static class EmitterRegistry
{
    private static readonly ConcurrentDictionary<string, ICodeEmitter> Emitters = new();

    /// <summary>Registers an emitter. Later registrations overwrite earlier ones for the same feature name.</summary>
    public static void Register(ICodeEmitter emitter) =>
        Emitters[emitter.Feature] = emitter;

    /// <summary>Retrieves the emitter registered for the given feature name.</summary>
    /// <exception cref="KeyNotFoundException">Thrown when no emitter is registered for <paramref name="feature"/>.</exception>
    public static ICodeEmitter Get(string feature) => Emitters[feature];

    /// <summary>Removes all registered emitters. Intended for use in tests only.</summary>
    public static void Clear() => Emitters.Clear();
}
