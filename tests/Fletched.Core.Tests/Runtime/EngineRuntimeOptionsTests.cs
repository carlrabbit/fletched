using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Core.Tests.Runtime;

public class EngineRuntimeOptionsTests
{
    [Test]
    public async Task MaxRecursionDepth_Zero_ThrowsInvalidConfiguration()
    {
        var options = new EngineRuntimeOptions();

        await Assert.That(() => options.MaxRecursionDepth = 0)
            .Throws<InvalidRecursionDepthConfigurationException>();
    }

    [Test]
    public async Task MaxRecursionDepth_Negative_ThrowsInvalidConfiguration()
    {
        var options = new EngineRuntimeOptions();

        await Assert.That(() => options.MaxRecursionDepth = -1)
            .Throws<InvalidRecursionDepthConfigurationException>();
    }

    [Test]
    public async Task MaxRecursionDepth_Null_DisablesGuard()
    {
        var options = new EngineRuntimeOptions
        {
            MaxRecursionDepth = null
        };

        await Assert.That(options.MaxRecursionDepth).IsNull();
    }

    [Test]
    public async Task MaxRecursionDepth_Positive_SetsGuard()
    {
        var options = new EngineRuntimeOptions
        {
            MaxRecursionDepth = 8
        };

        await Assert.That(options.MaxRecursionDepth).IsEqualTo(8);
    }
}
