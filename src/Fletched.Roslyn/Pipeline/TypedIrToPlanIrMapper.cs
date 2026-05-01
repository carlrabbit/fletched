using Fletched.Core.IR;

namespace Fletched.Roslyn.Pipeline;

/// <summary>
/// Second pipeline stage: maps a <see cref="TypedSymbol"/> typed IR node to a <see cref="PlanProgram"/> planned IR.
/// </summary>
public static class TypedIrToPlanIrMapper
{
    /// <summary>Produces a <see cref="PlanProgram"/> for the given <paramref name="typed"/> symbol.</summary>
    public static PlanProgram Map(TypedSymbol typed)
    {
        PlanBlock entry = new(
            Label: $"{typed.Namespace}.{typed.Name}",
            Instructions: [],
            Terminator: new ReturnTerminator());

        return new PlanProgram(entry, [entry]);
    }
}
