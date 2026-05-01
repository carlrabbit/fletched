using Fletched.Core.IR;

namespace Fletched.Emitters;

/// <summary>Emits the outer query method wrapping the execution loop.</summary>
public sealed class MethodEmitter : IEmitter
{
    /// <inheritdoc/>
    public string Emit(PlanBlock block) => string.Empty;
}
