using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Core.Tests.Runtime;

public class AllDistinctPartialTests
{
    [Test]
    public async Task AllDistinctPartial_AllBound_AllDistinct_ReturnsTrue()
    {
        bool result = BuiltIns.AllDistinctPartial<int>(
            new[] { 1, 2, 3 },
            new[] { true, true, true });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllDistinctPartial_AllBound_TwoEqual_ReturnsFalse()
    {
        bool result = BuiltIns.AllDistinctPartial<int>(
            new[] { 1, 2, 1 },
            new[] { true, true, true });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task AllDistinctPartial_SomeBound_UnboundDuplicateIgnored_ReturnsTrue()
    {
        // s1=1 bound, s2=1 unbound, s3=2 bound — the duplicate (s2) is unbound so no failure
        bool result = BuiltIns.AllDistinctPartial<int>(
            new[] { 1, 1, 2 },
            new[] { true, false, true });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllDistinctPartial_SomeBound_BoundDuplicatePresent_ReturnsFalse()
    {
        // s1=5 bound, s2=5 bound, s3=9 unbound — s1 and s2 collide
        bool result = BuiltIns.AllDistinctPartial<int>(
            new[] { 5, 5, 9 },
            new[] { true, true, false });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task AllDistinctPartial_NoBound_ReturnsTrue()
    {
        bool result = BuiltIns.AllDistinctPartial<int>(
            new[] { 7, 7, 7 },
            new[] { false, false, false });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllDistinctPartial_Empty_ReturnsTrue()
    {
        bool result = BuiltIns.AllDistinctPartial<int>(
            Array.Empty<int>(),
            Array.Empty<bool>());

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllDistinctPartial_SingleBound_ReturnsTrue()
    {
        bool result = BuiltIns.AllDistinctPartial<int>(
            new[] { 42 },
            new[] { true });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllDistinctPartial_StringElements_AllDistinct_ReturnsTrue()
    {
        bool result = BuiltIns.AllDistinctPartial<string>(
            new[] { "alice", "bob", "charlie" },
            new[] { true, true, true });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllDistinctPartial_StringElements_Duplicate_ReturnsFalse()
    {
        bool result = BuiltIns.AllDistinctPartial<string>(
            new[] { "alice", "alice" },
            new[] { true, true });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task AllDistinctPartial_MismatchedArrayLengths_ThrowsArgumentException()
    {
        await Assert.That(() =>
            BuiltIns.AllDistinctPartial<int>(new[] { 1, 2 }, new[] { true }))
            .Throws<ArgumentException>();
    }
}
