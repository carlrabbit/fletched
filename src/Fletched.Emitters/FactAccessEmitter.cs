using Fletched.Core.IR;

namespace Fletched.Emitters;

/// <summary>Emits fact table access and iteration code.</summary>
public sealed class FactAccessEmitter : IEmitter
{
    /// <inheritdoc/>
    public string Emit(PlanBlock block) => string.Empty;
}
