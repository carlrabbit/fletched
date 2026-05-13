using System.Globalization;

namespace WorkAssignment;

public static class WorkAssignmentInput
{
    public static IReadOnlyList<WorkShift> GetMonthShifts(int year, int month)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var shifts = new List<WorkShift>(daysInMonth * 2);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            shifts.Add(new WorkShift(date, ShiftPeriod.Early));
            shifts.Add(new WorkShift(date, ShiftPeriod.Late));
        }

        return shifts;
    }

    public static IReadOnlyList<WorkerAvailability> ParseWorkersFromCsv(string csvPath, int year, int month)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvPath}");
        }

        return ParseWorkersFromCsvLines(File.ReadLines(csvPath), year, month);
    }

    public static IReadOnlyList<WorkerAvailability> ParseWorkersFromCsvLines(
        IEnumerable<string> csvLines,
        int year,
        int month)
    {
        IReadOnlyList<WorkShift> shifts = GetMonthShifts(year, month);
        var validShifts = shifts.ToHashSet();
        var workers = new List<WorkerAvailability>();
        var workerNames = new HashSet<string>(StringComparer.Ordinal);

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
            if (!workerNames.Add(workerName))
            {
                throw new InvalidOperationException($"Worker '{workerName}' is listed more than once.");
            }

            var unavailableShifts = new HashSet<WorkShift>();

            for (int partIndex = 1; partIndex < parts.Length; partIndex++)
            {
                string shiftToken = parts[partIndex];
                if (string.IsNullOrWhiteSpace(shiftToken))
                {
                    continue;
                }

                unavailableShifts.Add(ParseShiftToken(shiftToken, year, month, validShifts));
            }

            if (unavailableShifts.Count > 5)
            {
                throw new InvalidOperationException(
                    $"Worker '{workerName}' has {unavailableShifts.Count} unavailable shifts. Maximum is 5.");
            }

            workers.Add(new WorkerAvailability(workerName, unavailableShifts));
        }

        return workers;
    }

    public static IReadOnlyList<WorkerAvailability> GenerateWorkers(
        int workerCount,
        string seed,
        int year,
        int month)
    {
        if (workerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Worker count must be greater than 0.");
        }

        IReadOnlyList<WorkShift> shifts = GetMonthShifts(year, month);
        var random = new Random(CalculateStableSeed($"{seed}:{year:D4}-{month:D2}"));
        var workers = new List<WorkerAvailability>(workerCount);
        int maxUnavailableShiftCount = Math.Min(5, shifts.Count);

        for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
        {
            int unavailableShiftCount = random.Next(0, maxUnavailableShiftCount + 1);
            var unavailableShifts = new HashSet<WorkShift>();

            while (unavailableShifts.Count < unavailableShiftCount)
            {
                unavailableShifts.Add(shifts[random.Next(0, shifts.Count)]);
            }

            workers.Add(new WorkerAvailability($"Worker{workerIndex + 1}", unavailableShifts));
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

    private static WorkShift ParseShiftToken(
        string shiftToken,
        int year,
        int month,
        IReadOnlySet<WorkShift> validShifts)
    {
        string trimmedToken = shiftToken.Trim();
        ShiftPeriod period = ParseShiftPeriod(trimmedToken, out string dateText);

        if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            throw new InvalidOperationException(
                $"Unknown shift token '{shiftToken}'. Expected values like 2026-05-05Early or 2026-05-05 Late.");
        }

        var shift = new WorkShift(date, period);
        if (date.Year != year || date.Month != month || !validShifts.Contains(shift))
        {
            throw new InvalidOperationException(
                $"Shift '{shiftToken}' is outside {year:D4}-{month:D2} weekdays.");
        }

        return shift;
    }

    private static ShiftPeriod ParseShiftPeriod(string shiftToken, out string dateText)
    {
        if (shiftToken.EndsWith(nameof(ShiftPeriod.Early), StringComparison.OrdinalIgnoreCase))
        {
            dateText = RemoveDateSeparator(shiftToken[..^nameof(ShiftPeriod.Early).Length]);
            return ShiftPeriod.Early;
        }

        if (shiftToken.EndsWith(nameof(ShiftPeriod.Late), StringComparison.OrdinalIgnoreCase))
        {
            dateText = RemoveDateSeparator(shiftToken[..^nameof(ShiftPeriod.Late).Length]);
            return ShiftPeriod.Late;
        }

        throw new InvalidOperationException(
            $"Unknown shift token '{shiftToken}'. Expected values like 2026-05-05Early or 2026-05-05 Late.");
    }

    private static string RemoveDateSeparator(string dateText) =>
        dateText.Trim().TrimEnd(':', '-', '_');
}
