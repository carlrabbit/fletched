using Fletched.Core.IR;

namespace Fletched.Core.DSL;

/// <summary>
/// Represents a query boundary variable — a typed logical variable that is bound
/// by the caller and unified against facts during query execution.
/// </summary>
public sealed class TerminalVar<T>
{
    private static int _nextId;

    internal readonly VarNode Node;

    public TerminalVar()
    {
        Node = new VarNode(typeof(T), Interlocked.Increment(ref _nextId));
    }

    /// <summary>Implicitly converts a <see cref="TerminalVar{T}"/> to a <see cref="LogicExpr{T}"/>.</summary>
    public static implicit operator LogicExpr<T>(TerminalVar<T> v)
        => new(v.Node);
}
