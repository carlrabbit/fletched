namespace WorkAssignment;

public sealed record AssignmentResult(
    IReadOnlyList<string> ShiftAssignments,
    IReadOnlyDictionary<string, int> ShiftCountsByWorker);
