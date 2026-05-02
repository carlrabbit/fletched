namespace Fletched.Core;

/// <summary>
/// A typed proxy for a <see cref="FactAttribute"/> type.
/// The source generator extends this with one <see cref="LogicExpr{TField}"/> property per field.
/// </summary>
public readonly struct Proxy<T>
{
    internal readonly string VariableName;

    public Proxy(string variableName) => VariableName = variableName;
}
