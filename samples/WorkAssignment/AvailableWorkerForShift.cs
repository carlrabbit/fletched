using Fletched.Core;

namespace WorkAssignment;

public static partial class WorkAssignmentModule
{
    [Predicate]
    public readonly partial record struct AvailableWorkerForShift
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<int> shiftIndex, TerminalVar<string> workerName) =>
            Logic.With<ShiftFact, WorkerFact>((shift, worker) =>
                shift.ShiftIndex == shiftIndex &&
                worker.Name == workerName &&
                Logic.Not(Logic.With<WorkerUnavailableFact>(unavailable =>
                    unavailable.WorkerName == worker.Name &&
                    unavailable.ShiftIndex == shift.ShiftIndex)));
    }
}
