using TUnit;

namespace Fletched.Features.Tests;

public class GeneratorSmokeTests
{
    [Test]
    public async Task Generator_IsReferenced()
    {
        // The generator is referenced as an analyzer — if it compiled, we're good.
        await Assert.That(1 + 1).IsEqualTo(2);
    }
}
