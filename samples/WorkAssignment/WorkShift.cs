namespace WorkAssignment;

public readonly record struct WorkShift(DateOnly Date, ShiftPeriod Period)
{
    public override string ToString() => $"{Date:yyyy-MM-dd}{Period}";
}
