namespace Fletched.Core.Runtime;

/// <summary>Records a backtracking continuation point.</summary>
public struct ChoicePoint
{
    /// <summary>The label id (integer) to resume execution at.</summary>
    public int LabelId;

    /// <summary>Trail top at the moment this choice point was created.</summary>
    public int TrailTop;
}
