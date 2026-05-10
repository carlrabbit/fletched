using WorkAssignment;
using TUnit;

namespace WorkAssignment.Tests;

public class WorkAssignmentInputTests
{
    [Test]
    public async Task ParseWorkersFromCsvLines_ValidInput_ParsesWorkersAndShifts()
    {
        string[] lines =
        [
            "Alice;MonEarly;WedLate",
            "Bob;FridayEarly",
        ];

        IReadOnlyList<WorkerAvailability> workers = WorkAssignmentInput.ParseWorkersFromCsvLines(lines);

        await Assert.That(workers.Count).IsEqualTo(2);
        await Assert.That(workers[0].UnavailableShiftIndexes.Contains(0)).IsTrue();
        await Assert.That(workers[0].UnavailableShiftIndexes.Contains(5)).IsTrue();
        await Assert.That(workers[1].UnavailableShiftIndexes.Contains(8)).IsTrue();
    }

    [Test]
    public async Task GenerateWorkers_WithSeed_UsesMaximumOfFiveUnavailableShifts()
    {
        IReadOnlyList<WorkerAvailability> workers = WorkAssignmentInput.GenerateWorkers(20, "sample-seed");

        bool allWithinMax = workers.All(worker => worker.UnavailableShiftIndexes.Count <= 5);

        await Assert.That(allWithinMax).IsTrue();
    }
}

public class WorkAssignmentSolverTests
{
    [Test]
    public async Task FindFirstAssignments_ValidConstraints_ReturnsFairFirstFiveAssignments()
    {
        IReadOnlyList<WorkerAvailability> workers =
        [
            new WorkerAvailability("Alice", new HashSet<int>()),
            new WorkerAvailability("Bob", new HashSet<int>()),
            new WorkerAvailability("Cara", new HashSet<int>()),
            new WorkerAvailability("Dan", new HashSet<int>()),
        ];

        IReadOnlyList<AssignmentResult> assignments = WorkAssignmentSolver.FindFirstAssignments(workers, 5);

        await Assert.That(assignments.Count).IsEqualTo(5);

        bool allFair = assignments.All(assignment =>
            assignment.ShiftCountsByWorker.Values.Min() >= 3 &&
            assignment.ShiftCountsByWorker.Values.Max() <= 4);

        await Assert.That(allFair).IsTrue();
    }
}
