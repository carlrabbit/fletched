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

        int shiftCount = WorkAssignmentInput.ShiftNames.Length;
        int minShiftCountPerWorker = shiftCount / workers.Count;
        int workersWithExtraShift = shiftCount % workers.Count;

        var assignments = new List<AssignmentResult>(capacity: maxAssignments);
        var seenAssignmentKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (int[] extraWorkerIndexes in EnumerateExtraWorkerIndexes(workers.Count, workersWithExtraShift))
        {
            EngineMetrics.PredicateInvocations?.Add(1);
            EngineMetrics.FactScans?.Add(1);

            WorkAssignmentModule.EngineContext ctx = BuildEngineContext(workers, minShiftCountPerWorker, extraWorkerIndexes);

            foreach (WorkAssignmentModule.FairAssignmentResult result in default(WorkAssignmentModule.FairAssignment).ExecuteArity14(ctx))
            {
                EngineMetrics.UnifyAttempts?.Add(1);

                AssignmentResult assignment = CreateAssignmentResult(result);
                string assignmentKey = string.Join("|", assignment.ShiftAssignments);
                if (!seenAssignmentKeys.Add(assignmentKey))
                {
                    EngineMetrics.UnifyFailures?.Add(1);
                    EngineMetrics.BacktrackCount?.Add(1);
                    continue;
                }

                EngineMetrics.IndexHits?.Add(1);
                EngineMetrics.ChoicePointCount?.Add(1);
                assignments.Add(assignment);
                if (assignments.Count >= maxAssignments)
                {
                    return assignments;
                }
            }
        }

        return assignments;
    }

    private static WorkAssignmentModule.EngineContext BuildEngineContext(
        IReadOnlyList<WorkerAvailability> workers,
        int minShiftCountPerWorker,
        IReadOnlyCollection<int> extraWorkerIndexes)
    {
        var ctx = new WorkAssignmentModule.EngineContext();

        WorkAssignmentModule.QuotaSlotFact[] quotaSlots = BuildQuotaSlots(workers, minShiftCountPerWorker, extraWorkerIndexes);
        ctx.QuotaSlotFacts = new FactTable<WorkAssignmentModule.QuotaSlotFact>(quotaSlots);
        ctx.ShiftQuotaSlotOptionFacts = new FactTable<WorkAssignmentModule.ShiftQuotaSlotOptionFact>(
            BuildShiftQuotaSlotOptions(workers, quotaSlots));
        return ctx;
    }

    private static WorkAssignmentModule.QuotaSlotFact[] BuildQuotaSlots(
        IReadOnlyList<WorkerAvailability> workers,
        int minShiftCountPerWorker,
        IReadOnlyCollection<int> extraWorkerIndexes)
    {
        var extraWorkerIndexSet = extraWorkerIndexes as ISet<int> ?? new HashSet<int>(extraWorkerIndexes);
        var quotaSlots = new List<WorkAssignmentModule.QuotaSlotFact>(WorkAssignmentInput.ShiftNames.Length);

        int slotId = 0;
        for (int workerIndex = 0; workerIndex < workers.Count; workerIndex++)
        {
            int quota = minShiftCountPerWorker + (extraWorkerIndexSet.Contains(workerIndex) ? 1 : 0);
            for (int quotaIndex = 0; quotaIndex < quota; quotaIndex++)
            {
                quotaSlots.Add(new WorkAssignmentModule.QuotaSlotFact(slotId, workers[workerIndex].Name));
                slotId++;
            }
        }

        return quotaSlots.ToArray();
    }


    private static WorkAssignmentModule.ShiftQuotaSlotOptionFact[] BuildShiftQuotaSlotOptions(
        IReadOnlyList<WorkerAvailability> workers,
        IReadOnlyList<WorkAssignmentModule.QuotaSlotFact> quotaSlots)
    {
        var unavailableByWorker = workers.ToDictionary(
            worker => worker.Name,
            worker => worker.UnavailableShiftIndexes,
            StringComparer.Ordinal);
        var options = new List<WorkAssignmentModule.ShiftQuotaSlotOptionFact>(WorkAssignmentInput.ShiftNames.Length * quotaSlots.Count);

        foreach (WorkAssignmentModule.QuotaSlotFact quotaSlot in quotaSlots)
        {
            IReadOnlySet<int> unavailableShiftIndexes = unavailableByWorker[quotaSlot.WorkerName];
            for (int shiftIndex = 0; shiftIndex < WorkAssignmentInput.ShiftNames.Length; shiftIndex++)
            {
                if (!unavailableShiftIndexes.Contains(shiftIndex))
                {
                    options.Add(new WorkAssignmentModule.ShiftQuotaSlotOptionFact(shiftIndex, quotaSlot.SlotId, quotaSlot.WorkerName));
                }
            }
        }

        return options.ToArray();
    }

    private static IEnumerable<int[]> EnumerateExtraWorkerIndexes(int workerCount, int workersWithExtraShift)
    {
        if (workersWithExtraShift == 0)
        {
            yield return [];
            yield break;
        }

        int[] buffer = new int[workersWithExtraShift];
        foreach (int[] combination in Enumerate(0, 0))
        {
            yield return combination;
        }

        IEnumerable<int[]> Enumerate(int startIndex, int depth)
        {
            if (depth == workersWithExtraShift)
            {
                yield return buffer.ToArray();
                yield break;
            }

            int remaining = workersWithExtraShift - depth;
            for (int workerIndex = startIndex; workerIndex <= workerCount - remaining; workerIndex++)
            {
                buffer[depth] = workerIndex;
                foreach (int[] combination in Enumerate(workerIndex + 1, depth + 1))
                {
                    yield return combination;
                }
            }
        }
    }

    private static AssignmentResult CreateAssignmentResult(WorkAssignmentModule.FairAssignmentResult result)
    {
        string[] shifts =
        [
            result.monEarly,
            result.monLate,
            result.tueEarly,
            result.tueLate,
            result.wedEarly,
            result.wedLate,
            result.thuEarly,
            result.thuLate,
            result.friEarly,
            result.friLate,
            result.satEarly,
            result.satLate,
            result.sunEarly,
            result.sunLate,
        ];

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string workerName in shifts)
        {
            counts[workerName] = counts.GetValueOrDefault(workerName) + 1;
        }

        return new AssignmentResult(shifts, counts);
    }
}
