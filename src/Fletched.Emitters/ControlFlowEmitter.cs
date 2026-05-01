using Fletched.Core.IR;

namespace Fletched.Emitters;

/// <summary>Emits control-flow constructs such as branches and choice points.</summary>
public sealed class ControlFlowEmitter : IEmitter
{
    /// <inheritdoc/>
    public string Emit(PlanBlock block) => string.Empty;
}
