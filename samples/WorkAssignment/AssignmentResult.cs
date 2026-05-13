namespace WorkAssignment;

public sealed record AssignmentResult(
    IReadOnlyList<ShiftAssignment> ShiftAssignments,
    IReadOnlyDictionary<string, int> ShiftCountsByWorker);
