namespace Fletched.Core.Runtime;

/// <summary>Runtime options for operational engine behavior.</summary>
public sealed class EngineRuntimeOptions
{
    private int? _maxRecursionDepth;

    /// <summary>
    /// Maximum allowed predicate invocation depth.
    /// <c>null</c> disables the guard.
    /// </summary>
    public int? MaxRecursionDepth
    {
        get => _maxRecursionDepth;
        set
        {
            if (value is <= 0)
                throw new InvalidRecursionDepthConfigurationException(value.Value);

            _maxRecursionDepth = value;
        }
    }
}
