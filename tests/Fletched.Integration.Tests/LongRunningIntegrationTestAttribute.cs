using System;
using System.Threading.Tasks;
using TUnit;

namespace Fletched.Integration.Tests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public sealed class LongRunningIntegrationTestAttribute : SkipAttribute
{
    private const string EnableVariable = "FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS";

    public LongRunningIntegrationTestAttribute()
        : base($"Long-running integration test. Set {EnableVariable}=1 to include it.")
    {
    }

    public override Task<bool> ShouldSkip(TestRegisteredContext testRegisteredContext)
    {
        string? value = Environment.GetEnvironmentVariable(EnableVariable);
        bool shouldRun = string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(!shouldRun);
    }
}
