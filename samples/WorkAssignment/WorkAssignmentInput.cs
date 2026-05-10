namespace WorkAssignment;

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
        return string.Concat(token.Where(char.IsLetter).Select(char.ToLowerInvariant));
    }
}
