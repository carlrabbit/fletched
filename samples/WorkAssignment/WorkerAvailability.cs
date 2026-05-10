namespace WorkAssignment;

public sealed record WorkerAvailability(string Name, IReadOnlySet<int> UnavailableShiftIndexes);
