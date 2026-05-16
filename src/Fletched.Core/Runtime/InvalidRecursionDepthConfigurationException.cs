using System;

namespace Fletched.Core.Runtime;

/// <summary>Raised when recursion depth configuration uses a non-positive value.</summary>
public sealed class InvalidRecursionDepthConfigurationException : ArgumentOutOfRangeException
{
    public const string DiagnosticIdValue = "FLR1002";

    public InvalidRecursionDepthConfigurationException(int configuredDepth)
        : base(
            paramName: nameof(EngineRuntimeOptions.MaxRecursionDepth),
            actualValue: configuredDepth,
            message: $"Recursion depth configuration must be null or a positive integer. Received {configuredDepth}.")
    {
        ConfiguredDepth = configuredDepth;
    }

    public int ConfiguredDepth { get; }

    public string DiagnosticId => DiagnosticIdValue;
}
