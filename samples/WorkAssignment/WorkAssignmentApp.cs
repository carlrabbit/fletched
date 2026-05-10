using System.Collections.Concurrent;
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

public sealed record WorkerAvailability(string Name, IReadOnlySet<int> UnavailableShiftIndexes);

public sealed record AssignmentResult(
    IReadOnlyList<string> ShiftAssignments,
    IReadOnlyDictionary<string, int> ShiftCountsByWorker);

public static class WorkAssignmentInput
{
    public static readonly string[] DayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
    public static readonly string[] ShiftNames = BuildShiftNames();

    private static readonly Dictionary<string, int> ShiftTokenToIndex = BuildShiftTokenToIndex();

    public static IReadOnlyList<WorkerAvailability> ParseWorkersFromCsv(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvPath}");
        }

        return ParseWorkersFromCsvLines(File.ReadLines(csvPath));
    }

    public static IReadOnlyList<WorkerAvailability> ParseWorkersFromCsvLines(IEnumerable<string> csvLines)
    {
        var workers = new List<WorkerAvailability>();

        int lineNumber = 0;
        foreach (string rawLine in csvLines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            string[] parts = rawLine.Split(';', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new InvalidOperationException($"CSV line {lineNumber} does not include a worker name.");
            }

            string workerName = parts[0];
            var unavailableShiftIndexes = new HashSet<int>();

            for (int partIndex = 1; partIndex < parts.Length; partIndex++)
            {
                string shiftToken = parts[partIndex];
                if (string.IsNullOrWhiteSpace(shiftToken))
                {
                    continue;
                }

                int shiftIndex = ParseShiftToken(shiftToken);
                unavailableShiftIndexes.Add(shiftIndex);
            }

            if (unavailableShiftIndexes.Count > 5)
            {
                throw new InvalidOperationException(
                    $"Worker '{workerName}' has {unavailableShiftIndexes.Count} unavailable shifts. Maximum is 5.");
            }

            workers.Add(new WorkerAvailability(workerName, unavailableShiftIndexes));
        }

        return workers;
    }

    public static IReadOnlyList<WorkerAvailability> GenerateWorkers(int workerCount, string seed)
    {
        if (workerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Worker count must be greater than 0.");
        }

        var random = new Random(CalculateStableSeed(seed));
        var workers = new List<WorkerAvailability>(workerCount);

        for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
        {
            int unavailableShiftCount = random.Next(0, 6);
            var unavailableShiftIndexes = new HashSet<int>();

            while (unavailableShiftIndexes.Count < unavailableShiftCount)
            {
                unavailableShiftIndexes.Add(random.Next(0, ShiftNames.Length));
            }

            workers.Add(new WorkerAvailability($"Worker{workerIndex + 1}", unavailableShiftIndexes));
        }

        return workers;
    }

    private static int ParseShiftToken(string shiftToken)
    {
        string normalized = NormalizeShiftToken(shiftToken);
        if (ShiftTokenToIndex.TryGetValue(normalized, out int index))
        {
            return index;
        }

        throw new InvalidOperationException(
            $"Unknown shift token '{shiftToken}'. Expected values like MonEarly, MonLate, MondayEarly, MondayLate.");
    }

    private static string[] BuildShiftNames()
    {
        var shifts = new string[DayNames.Length * 2];
        for (int dayIndex = 0; dayIndex < DayNames.Length; dayIndex++)
        {
            int baseIndex = dayIndex * 2;
            shifts[baseIndex] = $"{DayNames[dayIndex]}Early";
            shifts[baseIndex + 1] = $"{DayNames[dayIndex]}Late";
        }

        return shifts;
    }

    private static Dictionary<string, int> BuildShiftTokenToIndex()
    {
        var mapping = new Dictionary<string, int>(StringComparer.Ordinal);
        string[] fullDayNames = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

        for (int dayIndex = 0; dayIndex < DayNames.Length; dayIndex++)
        {
            int earlyIndex = dayIndex * 2;
            int lateIndex = earlyIndex + 1;

            AddAlias(mapping, $"{DayNames[dayIndex]}Early", earlyIndex);
            AddAlias(mapping, $"{DayNames[dayIndex]}Late", lateIndex);
            AddAlias(mapping, $"{fullDayNames[dayIndex]}Early", earlyIndex);
            AddAlias(mapping, $"{fullDayNames[dayIndex]}Late", lateIndex);
        }

        return mapping;
    }

    private static void AddAlias(Dictionary<string, int> mapping, string token, int index)
    {
        mapping[NormalizeShiftToken(token)] = index;
    }

    private static string NormalizeShiftToken(string token)
    {
        var chars = token.Where(char.IsLetter).Select(char.ToLowerInvariant);
        return new string(chars.ToArray());
    }

    private static int CalculateStableSeed(string seed)
    {
        unchecked
        {
            int hash = 17;
            foreach (char value in seed)
            {
                hash = (hash * 31) + value;
            }

            return hash;
        }
    }
}

public static class WorkAssignmentSolver
{
    public static IReadOnlyList<AssignmentResult> FindFirstAssignments(
        IReadOnlyList<WorkerAvailability> workers,
        int maxAssignments)
    {
        if (workers.Count == 0 || maxAssignments <= 0)
        {
            return [];
        }

        int shiftCount = WorkAssignmentInput.ShiftNames.Length;
        int minShiftCountPerWorker = shiftCount / workers.Count;
        int maxShiftCountPerWorker = (int)Math.Ceiling((double)shiftCount / workers.Count);

        int[] workerShiftCounts = new int[workers.Count];
        int[] workerByShiftIndex = new int[shiftCount];
        Array.Fill(workerByShiftIndex, -1);

        var assignments = new List<AssignmentResult>(capacity: maxAssignments);

        FindAssignments(0);
        return assignments;

        void FindAssignments(int shiftIndex)
        {
            IncrementCounter(EngineMetrics.PredicateInvocations);

            if (assignments.Count >= maxAssignments)
            {
                return;
            }

            int remainingShiftCount = shiftCount - shiftIndex;
            for (int workerIndex = 0; workerIndex < workers.Count; workerIndex++)
            {
                if (workerShiftCounts[workerIndex] + remainingShiftCount < minShiftCountPerWorker)
                {
                    IncrementCounter(EngineMetrics.UnifyFailures);
                    return;
                }
            }

            if (shiftIndex == shiftCount)
            {
                IncrementCounter(EngineMetrics.IndexHits);
                assignments.Add(CreateAssignmentResult(workers, workerByShiftIndex));
                return;
            }

            IncrementCounter(EngineMetrics.FactScans);

            List<int> candidateWorkerIndexes = Enumerable
                .Range(0, workers.Count)
                .OrderBy(workerIndex => workerShiftCounts[workerIndex])
                .ToList();

            foreach (int workerIndex in candidateWorkerIndexes)
            {
                IncrementCounter(EngineMetrics.UnifyAttempts);

                if (workers[workerIndex].UnavailableShiftIndexes.Contains(shiftIndex) ||
                    workerShiftCounts[workerIndex] >= maxShiftCountPerWorker)
                {
                    IncrementCounter(EngineMetrics.UnifyFailures);
                    continue;
                }

                workerByShiftIndex[shiftIndex] = workerIndex;
                workerShiftCounts[workerIndex]++;
                IncrementCounter(EngineMetrics.ChoicePointCount);

                FindAssignments(shiftIndex + 1);

                workerShiftCounts[workerIndex]--;
                workerByShiftIndex[shiftIndex] = -1;
                IncrementCounter(EngineMetrics.BacktrackCount);

                if (assignments.Count >= maxAssignments)
                {
                    return;
                }
            }
        }
    }

    private static AssignmentResult CreateAssignmentResult(
        IReadOnlyList<WorkerAvailability> workers,
        IReadOnlyList<int> workerByShiftIndex)
    {
        var shifts = new string[workerByShiftIndex.Count];
        var counts = workers.ToDictionary(worker => worker.Name, _ => 0, StringComparer.Ordinal);

        for (int shiftIndex = 0; shiftIndex < workerByShiftIndex.Count; shiftIndex++)
        {
            int workerIndex = workerByShiftIndex[shiftIndex];
            string workerName = workers[workerIndex].Name;
            shifts[shiftIndex] = workerName;
            counts[workerName]++;
        }

        return new AssignmentResult(shifts, counts);
    }

    private static void IncrementCounter(Counter<long>? counter)
    {
        counter?.Add(1);
    }
}

public sealed record InputOptions(string? CsvPath, int? WorkerCount, string? Seed)
{
    public const string UsageText =
        "Usage:\n" +
        "  WorkAssignment --csv <path-to-workers.csv>\n" +
        "  WorkAssignment --workers <number> --seed <seed-text>";

    public static InputOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException("No arguments supplied.");
        }

        string? csvPath = null;
        int? workerCount = null;
        string? seed = null;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--csv":
                    csvPath = ReadValue(args, ref index, "--csv");
                    break;
                case "--workers":
                    string workerCountValue = ReadValue(args, ref index, "--workers");
                    if (!int.TryParse(workerCountValue, out int parsedWorkerCount) || parsedWorkerCount <= 0)
                    {
                        throw new InvalidOperationException("--workers must be a positive integer.");
                    }

                    workerCount = parsedWorkerCount;
                    break;
                case "--seed":
                    seed = ReadValue(args, ref index, "--seed");
                    break;
                case "--help":
                case "-h":
                    throw new InvalidOperationException("Help requested.");
                default:
                    throw new InvalidOperationException($"Unknown argument: {arg}");
            }
        }

        if (csvPath is not null)
        {
            if (workerCount is not null || seed is not null)
            {
                throw new InvalidOperationException("--csv cannot be combined with --workers or --seed.");
            }

            return new InputOptions(csvPath, null, null);
        }

        if (workerCount is null)
        {
            throw new InvalidOperationException("Provide either --csv or --workers.");
        }

        seed ??= "default";
        return new InputOptions(null, workerCount, seed);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        int nextIndex = index + 1;
        if (nextIndex >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        index = nextIndex;
        return args[nextIndex];
    }
}

public sealed class InMemoryMetricsCollector : IDisposable
{
    private readonly MeterListener _listener;
    private readonly ConcurrentDictionary<string, long> _values = new(StringComparer.Ordinal);

    public InMemoryMetricsCollector(string meterName)
    {
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            _values.AddOrUpdate(instrument.Name, measurement, (_, existing) => existing + measurement);
        });

        _listener.Start();
    }

    public IReadOnlyDictionary<string, long> Snapshot() =>
        _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

    public void Dispose()
    {
        _listener.Dispose();
    }
}
