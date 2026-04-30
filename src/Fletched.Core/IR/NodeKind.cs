namespace Fletched.Core.IR;

/// <summary>Identifies the kind of an IR expression node.</summary>
public enum NodeKind
{
    Var,
    Const,
    Field,
    Unify,
    Conj,
    Disj,
    Constraint
}
