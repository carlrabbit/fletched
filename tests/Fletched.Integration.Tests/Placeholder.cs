namespace Fletched.Integration.Tests;

// Integration tests for the Fletched engine.
// Tests will be added as components are integrated together.

public class BootstrapTests
{
    [Test]
    public async Task IntegrationTestProject_IsBootstrapped()
    {
        await Assert.That(true).IsTrue();
    }
}
