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
            IReadOnlyList<WorkerAvailability> workers = options.CsvPath is { } csvPath
                ? WorkAssignmentInput.ParseWorkersFromCsv(csvPath)
                : WorkAssignmentInput.GenerateWorkers(options.WorkerCount!.Value, options.Seed!);

            if (workers.Count == 0)
            {
                Console.WriteLine("No workers were provided.");
                return 1;
            }

            using var collector = new InMemoryMetricsCollector("work-assignment");
            using Meter meter = EngineMetrics.Initialize("work-assignment");

            IReadOnlyList<AssignmentResult> assignments =
                WorkAssignmentSolver.FindFirstAssignments(workers, MaxAssignmentsToShow);

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
            string unavailable = worker.UnavailableShiftIndexes.Count == 0
                ? "(none)"
                : string.Join(", ", worker.UnavailableShiftIndexes
                    .OrderBy(shiftIndex => shiftIndex)
                    .Select(shiftIndex => WorkAssignmentInput.ShiftNames[shiftIndex]));

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

            for (int dayIndex = 0; dayIndex < WorkAssignmentInput.DayNames.Length; dayIndex++)
            {
                int earlyShiftIndex = dayIndex * 2;
                int lateShiftIndex = earlyShiftIndex + 1;
                string dayName = WorkAssignmentInput.DayNames[dayIndex];

                Console.WriteLine(
                    $"  {dayName}: Early={assignment.ShiftAssignments[earlyShiftIndex]}, Late={assignment.ShiftAssignments[lateShiftIndex]}");
            }

            string counts = string.Join(", ", assignment.ShiftCountsByWorker.Select(kvp => $"{kvp.Key}={kvp.Value}"));
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
