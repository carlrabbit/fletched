using System.Diagnostics.Metrics;
using Fletched.Core.Performance;
using WorkAssignment;
using TUnit;

namespace WorkAssignment.Tests;

public class WorkAssignmentInputTests
{
    [Test]
    public async Task InputOptions_Parse_WithMaxRecursionDepth_ParsesOption()
    {
        InputOptions options = InputOptions.Parse([
            "--year", "2026",
            "--month", "2",
            "--workers", "4",
            "--seed", "alpha",
            "--max-recursion-depth", "64",
        ]);

        await Assert.That(options.MaxRecursionDepth).IsEqualTo(64);
    }

    [Test]
    public async Task InputOptions_Parse_WithInvalidMaxRecursionDepth_Throws()
    {
        await Assert.That(() => InputOptions.Parse([
                "--year", "2026",
                "--month", "2",
                "--workers", "4",
                "--seed", "alpha",
                "--max-recursion-depth", "0",
            ]))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetMonthShifts_WeekdayMonth_ExcludesWeekendsAndBuildsEarlyLatePairs()
    {
        IReadOnlyList<WorkShift> shifts = WorkAssignmentInput.GetMonthShifts(2026, 2);

        await Assert.That(shifts.Count).IsEqualTo(40);
        await Assert.That(shifts.Any(shift => shift.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)).IsFalse();

        bool allDatesHaveEarlyAndLate = shifts
            .GroupBy(shift => shift.Date)
            .All(group => group.Select(shift => shift.Period).OrderBy(period => period).SequenceEqual([ShiftPeriod.Early, ShiftPeriod.Late]));

        await Assert.That(allDatesHaveEarlyAndLate).IsTrue();
    }

    [Test]
    public async Task ParseWorkersFromCsvLines_ValidInput_ParsesWorkersAndDateBasedShifts()
    {
        string[] lines =
        [
            "Alice;2026-02-02Early;2026-02-04 Late",
            "Bob;2026-02-06Late",
        ];

        IReadOnlyList<WorkerAvailability> workers = WorkAssignmentInput.ParseWorkersFromCsvLines(lines, 2026, 2);

        await Assert.That(workers.Count).IsEqualTo(2);
        await Assert.That(workers[0].UnavailableShifts.Contains(new WorkShift(new DateOnly(2026, 2, 2), ShiftPeriod.Early))).IsTrue();
        await Assert.That(workers[0].UnavailableShifts.Contains(new WorkShift(new DateOnly(2026, 2, 4), ShiftPeriod.Late))).IsTrue();
        await Assert.That(workers[1].UnavailableShifts.Contains(new WorkShift(new DateOnly(2026, 2, 6), ShiftPeriod.Late))).IsTrue();
    }

    [Test]
    public async Task GenerateWorkers_WithSeed_UsesMaximumOfFiveUnavailableShifts()
    {
        IReadOnlyList<WorkerAvailability> workers = WorkAssignmentInput.GenerateWorkers(20, "sample-seed", 2026, 2);

        bool allWithinMax = workers.All(worker => worker.UnavailableShifts.Count <= 5);

        await Assert.That(allWithinMax).IsTrue();
    }
}

public class WorkAssignmentSolverTests
{
    [Test]
    public async Task FindFirstAssignments_ValidConstraints_ReturnsBalancedAssignmentsForMonth()
    {
        using Meter meter = EngineMetrics.Initialize("work-assignment-tests-solver");

        IReadOnlyList<WorkShift> shifts = WorkAssignmentInput.GetMonthShifts(2026, 2);
        IReadOnlyList<WorkerAvailability> workers =
        [
            new WorkerAvailability("Alice", new HashSet<WorkShift>()),
            new WorkerAvailability("Bob", new HashSet<WorkShift>()),
            new WorkerAvailability("Cara", new HashSet<WorkShift>()),
            new WorkerAvailability("Dan", new HashSet<WorkShift>()),
        ];

        IReadOnlyList<AssignmentResult> assignments = WorkAssignmentSolver.FindFirstAssignments(shifts, workers, 3);

        await Assert.That(assignments.Count).IsEqualTo(3);

        bool allFair = assignments.All(assignment =>
            assignment.ShiftCountsByWorker.Values.Min() == 10 &&
            assignment.ShiftCountsByWorker.Values.Max() == 10 &&
            assignment.ShiftAssignments.Count == shifts.Count);

        await Assert.That(allFair).IsTrue();
    }

    [Test]
    public async Task FindFirstAssignments_UnavailableShift_NeverAssignsWorkerToBlockedShift()
    {
        using Meter meter = EngineMetrics.Initialize("work-assignment-tests-unavailable");

        IReadOnlyList<WorkShift> shifts = WorkAssignmentInput.GetMonthShifts(2026, 2);
        WorkShift firstShift = shifts[0];
        IReadOnlyList<WorkerAvailability> workers =
        [
            new WorkerAvailability("Alice", new HashSet<WorkShift> { firstShift }),
            new WorkerAvailability("Bob", new HashSet<WorkShift>()),
            new WorkerAvailability("Cara", new HashSet<WorkShift>()),
            new WorkerAvailability("Dan", new HashSet<WorkShift>()),
        ];

        IReadOnlyList<AssignmentResult> assignments = WorkAssignmentSolver.FindFirstAssignments(shifts, workers, 1);

        bool aliceIncorrectlyAssignedToFirstShift = assignments.Any(assignment =>
            assignment.ShiftAssignments.Any(shiftAssignment =>
                shiftAssignment.Shift == firstShift &&
                shiftAssignment.WorkerName == "Alice"));

        await Assert.That(aliceIncorrectlyAssignedToFirstShift).IsFalse();
    }
}

public class WorkAssignmentAppTests
{
    [Test]
    public async Task Run_GeneratedInput_PrintsMonthAssignmentsAndCollectedMetrics()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        try
        {
#pragma warning disable TUnit0055
            Console.SetOut(output);
            Console.SetError(error);
#pragma warning restore TUnit0055

            int exitCode = WorkAssignmentApp.Run(["--year", "2026", "--month", "2", "--workers", "4", "--seed", "alpha"]);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output.ToString().Contains("Schedule month: 2026-02", StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.ToString().Contains("Collected metrics (in-memory collector):", StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.ToString().Contains("unify_attempts", StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.ToString().Contains("recursive_invocations", StringComparison.Ordinal)).IsTrue();
            await Assert.That(output.ToString().Contains("recursive_depth", StringComparison.Ordinal)).IsTrue();
            await Assert.That(error.ToString()).IsEqualTo(string.Empty);
        }
        finally
        {
#pragma warning disable TUnit0055
            Console.SetOut(originalOut);
            Console.SetError(originalError);
#pragma warning restore TUnit0055
        }
    }
}
