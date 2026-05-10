using Fletched.Core;

namespace WorkAssignment;

public static partial class WorkAssignmentModule
{
    [Predicate]
    public readonly partial record struct FairAssignment
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(
            TerminalVar<string> monEarly,
            TerminalVar<string> monLate,
            TerminalVar<string> tueEarly,
            TerminalVar<string> tueLate,
            TerminalVar<string> wedEarly,
            TerminalVar<string> wedLate,
            TerminalVar<string> thuEarly,
            TerminalVar<string> thuLate,
            TerminalVar<string> friEarly,
            TerminalVar<string> friLate,
            TerminalVar<string> satEarly,
            TerminalVar<string> satLate,
            TerminalVar<string> sunEarly,
            TerminalVar<string> sunLate) =>
            Logic.With<ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact>((q0, q1, q2) =>
                q0.ShiftIndex == 0 &&
                q1.ShiftIndex == 1 &&
                q2.ShiftIndex == 2 &&
                q0.WorkerName == monEarly &&
                q1.WorkerName == monLate &&
                q2.WorkerName == tueEarly &&
                q0.SlotId != q1.SlotId &&
                q0.SlotId != q2.SlotId &&
                q1.SlotId != q2.SlotId &&
                Logic.With<ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact>((q3, q4, q5) =>
                    q3.ShiftIndex == 3 &&
                    q4.ShiftIndex == 4 &&
                    q5.ShiftIndex == 5 &&
                    q3.WorkerName == tueLate &&
                    q4.WorkerName == wedEarly &&
                    q5.WorkerName == wedLate &&
                    q3.SlotId != q0.SlotId &&
                    q3.SlotId != q1.SlotId &&
                    q3.SlotId != q2.SlotId &&
                    q4.SlotId != q0.SlotId &&
                    q4.SlotId != q1.SlotId &&
                    q4.SlotId != q2.SlotId &&
                    q5.SlotId != q0.SlotId &&
                    q5.SlotId != q1.SlotId &&
                    q5.SlotId != q2.SlotId &&
                    q3.SlotId != q4.SlotId &&
                    q3.SlotId != q5.SlotId &&
                    q4.SlotId != q5.SlotId &&
                    Logic.With<ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact>((q6, q7, q8) =>
                        q6.ShiftIndex == 6 &&
                        q7.ShiftIndex == 7 &&
                        q8.ShiftIndex == 8 &&
                        q6.WorkerName == thuEarly &&
                        q7.WorkerName == thuLate &&
                        q8.WorkerName == friEarly &&
                        q6.SlotId != q0.SlotId &&
                        q6.SlotId != q1.SlotId &&
                        q6.SlotId != q2.SlotId &&
                        q6.SlotId != q3.SlotId &&
                        q6.SlotId != q4.SlotId &&
                        q6.SlotId != q5.SlotId &&
                        q7.SlotId != q0.SlotId &&
                        q7.SlotId != q1.SlotId &&
                        q7.SlotId != q2.SlotId &&
                        q7.SlotId != q3.SlotId &&
                        q7.SlotId != q4.SlotId &&
                        q7.SlotId != q5.SlotId &&
                        q8.SlotId != q0.SlotId &&
                        q8.SlotId != q1.SlotId &&
                        q8.SlotId != q2.SlotId &&
                        q8.SlotId != q3.SlotId &&
                        q8.SlotId != q4.SlotId &&
                        q8.SlotId != q5.SlotId &&
                        q6.SlotId != q7.SlotId &&
                        q6.SlotId != q8.SlotId &&
                        q7.SlotId != q8.SlotId &&
                        Logic.With<ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact>((q9, q10, q11) =>
                            q9.ShiftIndex == 9 &&
                            q10.ShiftIndex == 10 &&
                            q11.ShiftIndex == 11 &&
                            q9.WorkerName == friLate &&
                            q10.WorkerName == satEarly &&
                            q11.WorkerName == satLate &&
                            q9.SlotId != q0.SlotId &&
                            q9.SlotId != q1.SlotId &&
                            q9.SlotId != q2.SlotId &&
                            q9.SlotId != q3.SlotId &&
                            q9.SlotId != q4.SlotId &&
                            q9.SlotId != q5.SlotId &&
                            q9.SlotId != q6.SlotId &&
                            q9.SlotId != q7.SlotId &&
                            q9.SlotId != q8.SlotId &&
                            q10.SlotId != q0.SlotId &&
                            q10.SlotId != q1.SlotId &&
                            q10.SlotId != q2.SlotId &&
                            q10.SlotId != q3.SlotId &&
                            q10.SlotId != q4.SlotId &&
                            q10.SlotId != q5.SlotId &&
                            q10.SlotId != q6.SlotId &&
                            q10.SlotId != q7.SlotId &&
                            q10.SlotId != q8.SlotId &&
                            q11.SlotId != q0.SlotId &&
                            q11.SlotId != q1.SlotId &&
                            q11.SlotId != q2.SlotId &&
                            q11.SlotId != q3.SlotId &&
                            q11.SlotId != q4.SlotId &&
                            q11.SlotId != q5.SlotId &&
                            q11.SlotId != q6.SlotId &&
                            q11.SlotId != q7.SlotId &&
                            q11.SlotId != q8.SlotId &&
                            q9.SlotId != q10.SlotId &&
                            q9.SlotId != q11.SlotId &&
                            q10.SlotId != q11.SlotId &&
                            Logic.With<ShiftQuotaSlotOptionFact, ShiftQuotaSlotOptionFact>((q12, q13) =>
                                q12.ShiftIndex == 12 &&
                                q13.ShiftIndex == 13 &&
                                q12.WorkerName == sunEarly &&
                                q13.WorkerName == sunLate &&
                                q12.SlotId != q0.SlotId &&
                                q12.SlotId != q1.SlotId &&
                                q12.SlotId != q2.SlotId &&
                                q12.SlotId != q3.SlotId &&
                                q12.SlotId != q4.SlotId &&
                                q12.SlotId != q5.SlotId &&
                                q12.SlotId != q6.SlotId &&
                                q12.SlotId != q7.SlotId &&
                                q12.SlotId != q8.SlotId &&
                                q12.SlotId != q9.SlotId &&
                                q12.SlotId != q10.SlotId &&
                                q12.SlotId != q11.SlotId &&
                                q13.SlotId != q0.SlotId &&
                                q13.SlotId != q1.SlotId &&
                                q13.SlotId != q2.SlotId &&
                                q13.SlotId != q3.SlotId &&
                                q13.SlotId != q4.SlotId &&
                                q13.SlotId != q5.SlotId &&
                                q13.SlotId != q6.SlotId &&
                                q13.SlotId != q7.SlotId &&
                                q13.SlotId != q8.SlotId &&
                                q13.SlotId != q9.SlotId &&
                                q13.SlotId != q10.SlotId &&
                                q13.SlotId != q11.SlotId &&
                                q13.SlotId != q12.SlotId)))));
    }
}
