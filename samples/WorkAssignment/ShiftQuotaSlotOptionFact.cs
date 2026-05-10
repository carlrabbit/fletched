using Fletched.Core;

namespace WorkAssignment;

public static partial class WorkAssignmentModule
{
    [Fact]
    public readonly partial record struct ShiftQuotaSlotOptionFact(int ShiftIndex, int SlotId, string WorkerName);
}
