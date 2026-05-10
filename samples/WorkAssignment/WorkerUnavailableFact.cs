using Fletched.Core;

namespace WorkAssignment;

public static partial class WorkAssignmentModule
{
    [Fact]
    public readonly partial record struct WorkerUnavailableFact(string WorkerName, int ShiftIndex);
}
