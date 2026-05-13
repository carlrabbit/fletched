using Fletched.Core;

namespace WorkAssignment;

public static partial class WorkAssignmentModule
{
    [Fact, Predicate]
    public readonly partial record struct AvailableShiftFact(WorkShift Shift, string WorkerName)
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(
            TerminalVar<WorkShift> shift,
            TerminalVar<string> workerName) =>
            Logic.With<AvailableShiftFact>(available =>
                available.Shift == shift &&
                available.WorkerName == workerName);
    }
}
