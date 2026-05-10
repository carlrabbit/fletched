namespace WorkAssignment;

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
