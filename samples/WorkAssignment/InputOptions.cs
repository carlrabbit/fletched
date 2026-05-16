namespace WorkAssignment;

public sealed record InputOptions(
    int Year,
    int Month,
    string? CsvPath,
    int? WorkerCount,
    string? Seed,
    int? MaxRecursionDepth)
{
    public const string UsageText =
        "Usage:\n" +
        "  WorkAssignment --year <year> --month <month> [--max-recursion-depth <positive-integer>] --csv <path-to-workers.csv>\n" +
        "  WorkAssignment --year <year> --month <month> [--max-recursion-depth <positive-integer>] --workers <number> --seed <seed-text>";

    public static InputOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException("No arguments supplied.");
        }

        int? year = null;
        int? month = null;
        string? csvPath = null;
        int? workerCount = null;
        string? seed = null;
        int? maxRecursionDepth = null;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--csv":
                    csvPath = ReadValue(args, ref index, "--csv");
                    break;
                case "--year":
                    year = ParsePositiveInt(ReadValue(args, ref index, "--year"), "--year");
                    break;
                case "--month":
                    month = ParsePositiveInt(ReadValue(args, ref index, "--month"), "--month");
                    if (month is < 1 or > 12)
                    {
                        throw new InvalidOperationException("--month must be between 1 and 12.");
                    }

                    break;
                case "--workers":
                    string workerCountValue = ReadValue(args, ref index, "--workers");
                    workerCount = ParsePositiveInt(workerCountValue, "--workers");
                    break;
                case "--seed":
                    seed = ReadValue(args, ref index, "--seed");
                    break;
                case "--max-recursion-depth":
                    maxRecursionDepth = ParsePositiveInt(ReadValue(args, ref index, "--max-recursion-depth"), "--max-recursion-depth");
                    break;
                case "--help":
                case "-h":
                    throw new InvalidOperationException("Help requested.");
                default:
                    throw new InvalidOperationException($"Unknown argument: {arg}");
            }
        }

        if (year is null || month is null)
        {
            throw new InvalidOperationException("Both --year and --month are required.");
        }

        if (csvPath is not null)
        {
            if (workerCount is not null || seed is not null)
            {
                throw new InvalidOperationException("--csv cannot be combined with --workers or --seed.");
            }

            return new InputOptions(year.Value, month.Value, csvPath, null, null, maxRecursionDepth);
        }

        if (workerCount is null)
        {
            throw new InvalidOperationException("Provide either --csv or --workers.");
        }

        seed ??= "default";
        return new InputOptions(year.Value, month.Value, null, workerCount, seed, maxRecursionDepth);
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

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, out int parsedValue) || parsedValue <= 0)
        {
            throw new InvalidOperationException($"{option} must be a positive integer.");
        }

        return parsedValue;
    }
}
