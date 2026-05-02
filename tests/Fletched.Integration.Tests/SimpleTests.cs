using TUnit;

namespace Fletched.Integration.Tests;

public class SimpleTests
{
    [Test]
    public async Task Placeholder_AlwaysPasses()
    {
        await Assert.That(true).IsTrue();
    }
}
