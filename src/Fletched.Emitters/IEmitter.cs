using Fletched.Core.IR;

namespace Fletched.Emitters;

/// <summary>Generates source code from a <see cref="PlanBlock"/>.</summary>
public interface IEmitter
{
    /// <summary>Emits source code for the given planned IR block.</summary>
    string Emit(PlanBlock block);
}
