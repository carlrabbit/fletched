namespace WorkAssignment;

public sealed record WorkerAvailability(string Name, IReadOnlySet<WorkShift> UnavailableShifts);
