using Fletched.Core.IR;

namespace Fletched.Emitters;

/// <summary>Emits slot-to-field mapping declarations.</summary>
public sealed class SlotMappingEmitter : IEmitter
{
    /// <inheritdoc/>
    public string Emit(PlanBlock block) => string.Empty;
}
