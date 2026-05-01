using Fletched.Core.IR;

namespace Fletched.Emitters;

/// <summary>Emits result projection from execution state.</summary>
public sealed class ResultProjectionEmitter : IEmitter
{
    /// <inheritdoc/>
    public string Emit(PlanBlock block) => string.Empty;
}
