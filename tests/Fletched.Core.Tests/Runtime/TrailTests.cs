using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Core.Tests.Runtime;

public class TrailTests
{
    [Test]
    public async Task Trail_InitialTopIsZero()
    {
        var trail = new Trail();
        await Assert.That(trail.Top).IsEqualTo(0);
    }

    [Test]
    public async Task Trail_Push_IncrementsTop()
    {
        var trail = new Trail();
        trail.Push(0, false);
        await Assert.That(trail.Top).IsEqualTo(1);
        trail.Push(1, true);
        await Assert.That(trail.Top).IsEqualTo(2);
    }

    [Test]
    public async Task Trail_UnwindTo_RestoresBoundFlags()
    {
        var trail = new Trail();
        int top0 = trail.Top;
        trail.Push(0, false);
        trail.Push(1, true);

        bool slot0Restored = false, slot1Restored = false;
        bool slot0WasBound = true, slot1WasBound = false;

        trail.UnwindTo(top0, (slot, wasBound) =>
        {
            if (slot == 0) { slot0Restored = true; slot0WasBound = wasBound; }
            if (slot == 1) { slot1Restored = true; slot1WasBound = wasBound; }
        });

        await Assert.That(slot0Restored).IsTrue();
        await Assert.That(slot1Restored).IsTrue();
        await Assert.That(slot0WasBound).IsFalse();
        await Assert.That(slot1WasBound).IsTrue();
        await Assert.That(trail.Top).IsEqualTo(0);
    }

    [Test]
    public async Task Trail_UnwindTo_PartialUnwind()
    {
        var trail = new Trail();
        trail.Push(0, false);
        int mid = trail.Top;
        trail.Push(1, false);
        trail.Push(2, false);

        int unwound = 0;
        trail.UnwindTo(mid, (_, __) => unwound++);

        await Assert.That(unwound).IsEqualTo(2);
        await Assert.That(trail.Top).IsEqualTo(mid);
    }

    [Test]
    public async Task Trail_UnwindTo_NoOp_AtSameLevel()
    {
        var trail = new Trail();
        trail.Push(0, false);
        int top = trail.Top;
        trail.UnwindTo(top, (_, __) => throw new Exception("Should not be called"));
        await Assert.That(trail.Top).IsEqualTo(1);
    }

    [Test]
    public async Task Trail_PopEntry_DecreasesTop()
    {
        var trail = new Trail();
        trail.Push(3, true);
        TrailEntry entry = trail.PopEntry();
        await Assert.That(entry.Slot).IsEqualTo(3);
        await Assert.That(entry.WasBound).IsTrue();
        await Assert.That(trail.Top).IsEqualTo(0);
    }
}
