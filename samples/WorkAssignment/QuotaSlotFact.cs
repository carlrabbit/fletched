using Fletched.Core;

namespace WorkAssignment;

public static partial class WorkAssignmentModule
{
    [Fact]
    public readonly partial record struct QuotaSlotFact(int SlotId, string WorkerName);
}
