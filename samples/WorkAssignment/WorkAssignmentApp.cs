using System.Diagnostics.Metrics;
using Fletched.Core.Performance;

namespace WorkAssignment;

public static class WorkAssignmentApp
{
    private const int MaxAssignmentsToShow = 5;

    public static int Run(string[] args)
    {
        try
        {
            InputOptions options = InputOptions.Parse(args);
            IReadOnlyList<WorkShift> shifts = WorkAssignmentInput.GetMonthShifts(options.Year, options.Month);
            IReadOnlyList<WorkerAvailability> workers = options.CsvPath is { } csvPath
                ? WorkAssignmentInput.ParseWorkersFromCsv(csvPath, options.Year, options.Month)
                : WorkAssignmentInput.GenerateWorkers(options.WorkerCount!.Value, options.Seed!, options.Year, options.Month);

            if (workers.Count == 0)
            {
                Console.WriteLine("No workers were provided.");
                return 1;
            }

            using var collector = new InMemoryMetricsCollector("work-assignment");
            using Meter meter = EngineMetrics.Initialize("work-assignment");

            IReadOnlyList<AssignmentResult> assignments =
                WorkAssignmentSolver.FindFirstAssignments(shifts, workers, MaxAssignmentsToShow);

            Console.WriteLine($"Schedule month: {options.Year:D4}-{options.Month:D2}");
            Console.WriteLine();
            PrintWorkerOverview(workers);
            PrintAssignments(assignments);
            PrintMetrics(collector.Snapshot());

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(InputOptions.UsageText);
            return 1;
        }
    }

    private static void PrintWorkerOverview(IReadOnlyList<WorkerAvailability> workers)
    {
        Console.WriteLine("Workers and unavailable shifts:");
        foreach (WorkerAvailability worker in workers)
        {
            string unavailable = worker.UnavailableShifts.Count == 0
                ? "(none)"
                : string.Join(", ", worker.UnavailableShifts
                    .OrderBy(shift => shift.Date)
                    .ThenBy(shift => shift.Period)
                    .Select(shift => shift.ToString()));

            Console.WriteLine($"- {worker.Name}: {unavailable}");
        }

        Console.WriteLine();
    }

    private static void PrintAssignments(IReadOnlyList<AssignmentResult> assignments)
    {
        Console.WriteLine($"First {MaxAssignmentsToShow} possible assignments:");

        if (assignments.Count == 0)
        {
            Console.WriteLine("No valid assignment found for the provided constraints.");
            Console.WriteLine();
            return;
        }

        for (int assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
        {
            AssignmentResult assignment = assignments[assignmentIndex];
            Console.WriteLine($"Assignment #{assignmentIndex + 1}:");

            foreach (IGrouping<DateOnly, ShiftAssignment> dateAssignments in assignment.ShiftAssignments
                         .OrderBy(shiftAssignment => shiftAssignment.Shift.Date)
                         .ThenBy(shiftAssignment => shiftAssignment.Shift.Period)
                         .GroupBy(shiftAssignment => shiftAssignment.Shift.Date))
            {
                string earlyWorker = dateAssignments.Single(shiftAssignment => shiftAssignment.Shift.Period == ShiftPeriod.Early).WorkerName;
                string lateWorker = dateAssignments.Single(shiftAssignment => shiftAssignment.Shift.Period == ShiftPeriod.Late).WorkerName;

                Console.WriteLine(
                    $"  {dateAssignments.Key:yyyy-MM-dd}: Early={earlyWorker}, Late={lateWorker}");
            }

            string counts = string.Join(", ", assignment.ShiftCountsByWorker
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));
            Console.WriteLine($"  Shift count: {counts}");
            Console.WriteLine();
        }
    }

    private static void PrintMetrics(IReadOnlyDictionary<string, long> metrics)
    {
        Console.WriteLine("Collected metrics (in-memory collector):");

        if (metrics.Count == 0)
        {
            Console.WriteLine("- (none)");
            return;
        }

        foreach (KeyValuePair<string, long> metric in metrics.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"- {metric.Key}: {metric.Value}");
        }
    }
}
