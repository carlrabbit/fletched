using Fletched.Core.Runtime;

namespace WorkAssignment;

public static class WorkAssignmentSolver
{
    public static IReadOnlyList<AssignmentResult> FindFirstAssignments(
        IReadOnlyList<WorkShift> shifts,
        IReadOnlyList<WorkerAvailability> workers,
        int maxAssignments,
        int? maxRecursionDepth = null)
    {
        if (shifts.Count == 0 || workers.Count == 0 || maxAssignments <= 0)
        {
            return [];
        }

        int shiftCount = shifts.Count;
        int minShiftCountPerWorker = shiftCount / workers.Count;
        int workersWithExtraShift = shiftCount % workers.Count;

        var assignments = new List<AssignmentResult>(capacity: maxAssignments);

        WorkAssignmentModule.EngineContext ctx = BuildEngineContext(shifts, workers, maxRecursionDepth);
        Dictionary<WorkShift, IReadOnlyList<string>> availabilityByShift = GetAvailabilityByShift(shifts, ctx);

        foreach (int[] extraWorkerIndexes in EnumerateExtraWorkerIndexes(workers.Count, workersWithExtraShift))
        {
            var assignmentWorkers = new string[shifts.Count];
            IReadOnlyDictionary<string, int> quotas = BuildWorkerQuotasByName(workers, minShiftCountPerWorker, extraWorkerIndexes);

            SearchAssignments(
                shifts,
                availabilityByShift,
                quotas.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal),
                assignmentWorkers,
                assignments,
                maxAssignments);

            if (assignments.Count >= maxAssignments)
            {
                return assignments;
            }
        }

        return assignments;
    }

    private static WorkAssignmentModule.EngineContext BuildEngineContext(
        IReadOnlyList<WorkShift> shifts,
        IReadOnlyList<WorkerAvailability> workers,
        int? maxRecursionDepth)
    {
        var ctx = new WorkAssignmentModule.EngineContext();
        if (maxRecursionDepth.HasValue)
            RecursionGuard.SetMaxRecursionDepth(ctx, maxRecursionDepth.Value);

        ctx.AvailableShiftFacts = new FactTable<WorkAssignmentModule.AvailableShiftFact>(
            BuildAvailableShifts(shifts, workers));
        return ctx;
    }

    private static IReadOnlyDictionary<string, int> BuildWorkerQuotasByName(
        IReadOnlyList<WorkerAvailability> workers,
        int minShiftCountPerWorker,
        int[] extraWorkerIndexes)
    {
        var quotas = new Dictionary<string, int>(workers.Count, StringComparer.Ordinal);

        for (int workerIndex = 0; workerIndex < workers.Count; workerIndex++)
        {
            quotas[workers[workerIndex].Name] =
                minShiftCountPerWorker + (Array.BinarySearch(extraWorkerIndexes, workerIndex) >= 0 ? 1 : 0);
        }

        return quotas;
    }

    private static WorkAssignmentModule.AvailableShiftFact[] BuildAvailableShifts(
        IReadOnlyList<WorkShift> shifts,
        IReadOnlyList<WorkerAvailability> workers)
    {
        var availableShifts = new List<WorkAssignmentModule.AvailableShiftFact>();
        foreach (WorkerAvailability worker in workers)
        {
            foreach (WorkShift shift in shifts)
            {
                if (!worker.UnavailableShifts.Contains(shift))
                {
                    availableShifts.Add(new WorkAssignmentModule.AvailableShiftFact(shift, worker.Name));
                }
            }
        }

        return availableShifts.ToArray();
    }

    private static Dictionary<WorkShift, IReadOnlyList<string>> GetAvailabilityByShift(
        IReadOnlyList<WorkShift> shifts,
        WorkAssignmentModule.EngineContext ctx)
    {
        var availability = shifts.ToDictionary(
            shift => shift,
            _ => (IReadOnlyList<string>)Array.Empty<string>());

        var workersByShift = new Dictionary<WorkShift, List<string>>();
        foreach (var result in default(WorkAssignmentModule.AvailableShiftFact).ExecuteArity2(ctx))
        {
            if (!workersByShift.TryGetValue(result.shift, out List<string>? workerNames))
            {
                workerNames = [];
                workersByShift[result.shift] = workerNames;
            }

            workerNames.Add(result.workerName);
        }

        foreach ((WorkShift shift, List<string> workerNames) in workersByShift)
        {
            availability[shift] = workerNames;
        }

        return availability;
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

    private static void SearchAssignments(
        IReadOnlyList<WorkShift> shifts,
        IReadOnlyDictionary<WorkShift, IReadOnlyList<string>> availabilityByShift,
        Dictionary<string, int> remainingQuotas,
        string[] assignmentWorkers,
        List<AssignmentResult> assignments,
        int maxAssignments,
        int shiftIndex = 0)
    {
        if (assignments.Count >= maxAssignments)
        {
            return;
        }

        if (shiftIndex == shifts.Count)
        {
            assignments.Add(CreateAssignmentResult(shifts, assignmentWorkers));
            return;
        }

        WorkShift shift = shifts[shiftIndex];
        if (!availabilityByShift.TryGetValue(shift, out IReadOnlyList<string>? availableWorkers) || availableWorkers.Count == 0)
        {
            return;
        }

        foreach (string workerName in availableWorkers)
        {
            if (!remainingQuotas.TryGetValue(workerName, out int quota) || quota == 0)
            {
                continue;
            }

            remainingQuotas[workerName] = quota - 1;
            assignmentWorkers[shiftIndex] = workerName;
            SearchAssignments(shifts, availabilityByShift, remainingQuotas, assignmentWorkers, assignments, maxAssignments, shiftIndex + 1);
            remainingQuotas[workerName] = quota;

            if (assignments.Count >= maxAssignments)
            {
                return;
            }
        }
    }

    private static AssignmentResult CreateAssignmentResult(
        IReadOnlyList<WorkShift> shifts,
        IReadOnlyList<string> assignedWorkers)
    {
        if (assignedWorkers.Count != shifts.Count)
        {
            throw new InvalidOperationException(
                $"Expected {shifts.Count} assigned workers but received {assignedWorkers.Count}.");
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var assignments = new List<ShiftAssignment>(shifts.Count);

        for (int shiftIndex = 0; shiftIndex < shifts.Count; shiftIndex++)
        {
            string workerName = assignedWorkers[shiftIndex];
            counts[workerName] = counts.GetValueOrDefault(workerName) + 1;
            assignments.Add(new ShiftAssignment(shifts[shiftIndex], workerName));
        }

        return new AssignmentResult(assignments, counts);
    }
}
