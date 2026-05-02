using Fletched.Core.IR;

namespace Fletched.Core;

/// <summary>
/// Represents a terminal (output) variable in a predicate query.
/// Terminal variables must be bound in all execution paths.
/// </summary>
public readonly struct TerminalVar<T>
{
    internal readonly string Name;

    public TerminalVar(string name) => Name = name;

    /// <summary>Converts to a <see cref="LogicExpr{T}"/> wrapping a <see cref="VarNode"/>.</summary>
    public static implicit operator LogicExpr<T>(TerminalVar<T> v) =>
        new(new VarNode(v.Name, typeof(T)));
}
