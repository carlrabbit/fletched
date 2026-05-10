using System.Diagnostics.Metrics;
using Fletched.Core.Performance;
using Fletched.Core.Runtime;

namespace WorkAssignment;

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

        WorkAssignmentModule.EngineContext ctx = BuildEngineContext(workers);
        List<WorkAssignmentModule.AvailableWorkerForShiftResult> options =
            WorkAssignmentModule.Query_AvailableWorkerForShift(ctx).ToList();
        Dictionary<int, List<string>> availableWorkersByShift = BuildAvailableWorkersByShift(options);

        int shiftCount = WorkAssignmentInput.ShiftNames.Length;
        int minShiftCountPerWorker = shiftCount / workers.Count;
        int maxShiftCountPerWorker = (int)Math.Ceiling((double)shiftCount / workers.Count);

        var workerShiftCounts = workers.ToDictionary(worker => worker.Name, _ => 0, StringComparer.Ordinal);
        var workerByShiftIndex = new string[shiftCount];
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
            foreach (KeyValuePair<string, int> workerShiftCount in workerShiftCounts)
            {
                if (workerShiftCount.Value + remainingShiftCount < minShiftCountPerWorker)
                {
                    IncrementCounter(EngineMetrics.UnifyFailures);
                    return;
                }
            }

            if (shiftIndex == shiftCount)
            {
                IncrementCounter(EngineMetrics.IndexHits);
                assignments.Add(CreateAssignmentResult(workerByShiftIndex, workerShiftCounts));
                return;
            }

            IncrementCounter(EngineMetrics.FactScans);

            if (!availableWorkersByShift.TryGetValue(shiftIndex, out List<string>? availableWorkers) ||
                availableWorkers.Count == 0)
            {
                IncrementCounter(EngineMetrics.UnifyFailures);
                return;
            }

            List<string> orderedCandidates = availableWorkers
                .OrderBy(workerName => workerShiftCounts[workerName])
                .ThenBy(workerName => workerName, StringComparer.Ordinal)
                .ToList();

            foreach (string workerName in orderedCandidates)
            {
                IncrementCounter(EngineMetrics.UnifyAttempts);

                if (workerShiftCounts[workerName] >= maxShiftCountPerWorker)
                {
                    IncrementCounter(EngineMetrics.UnifyFailures);
                    continue;
                }

                workerByShiftIndex[shiftIndex] = workerName;
                workerShiftCounts[workerName]++;
                IncrementCounter(EngineMetrics.ChoicePointCount);

                FindAssignments(shiftIndex + 1);

                workerShiftCounts[workerName]--;
                workerByShiftIndex[shiftIndex] = string.Empty;
                IncrementCounter(EngineMetrics.BacktrackCount);

                if (assignments.Count >= maxAssignments)
                {
                    return;
                }
            }
        }
    }

    private static WorkAssignmentModule.EngineContext BuildEngineContext(IReadOnlyList<WorkerAvailability> workers)
    {
        var ctx = new WorkAssignmentModule.EngineContext();
        ctx.WorkerFacts = new FactTable<WorkAssignmentModule.WorkerFact>(
            workers.Select(worker => new WorkAssignmentModule.WorkerFact(worker.Name)).ToArray());
        ctx.ShiftFacts = new FactTable<WorkAssignmentModule.ShiftFact>(
            Enumerable.Range(0, WorkAssignmentInput.ShiftNames.Length)
                .Select(index => new WorkAssignmentModule.ShiftFact(index))
                .ToArray());

        WorkAssignmentModule.WorkerUnavailableFact[] unavailableFacts = workers
            .SelectMany(worker => worker.UnavailableShiftIndexes
                .Select(shiftIndex => new WorkAssignmentModule.WorkerUnavailableFact(worker.Name, shiftIndex)))
            .ToArray();

        ctx.WorkerUnavailableFacts = new FactTable<WorkAssignmentModule.WorkerUnavailableFact>(unavailableFacts);
        return ctx;
    }

    private static Dictionary<int, List<string>> BuildAvailableWorkersByShift(
        IEnumerable<WorkAssignmentModule.AvailableWorkerForShiftResult> options)
    {
        var byShift = new Dictionary<int, List<string>>();

        foreach (WorkAssignmentModule.AvailableWorkerForShiftResult option in options)
        {
            if (!byShift.TryGetValue(option.shiftIndex, out List<string>? workers))
            {
                workers = [];
                byShift[option.shiftIndex] = workers;
            }

            workers.Add(option.workerName);
        }

        return byShift;
    }

    private static AssignmentResult CreateAssignmentResult(
        IReadOnlyList<string> workerByShiftIndex,
        IReadOnlyDictionary<string, int> workerShiftCounts)
    {
        string[] shifts = workerByShiftIndex.ToArray();
        Dictionary<string, int> counts = workerShiftCounts.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.Ordinal);

        return new AssignmentResult(shifts, counts);
    }

    private static void IncrementCounter(Counter<long>? counter)
    {
        counter?.Add(1);
    }
}
