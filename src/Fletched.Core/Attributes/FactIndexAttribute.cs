using System;

namespace Fletched.Core;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
public sealed class FactIndexAttribute(params string[] members) : Attribute
{
    public string[] Members { get; } = members;

    public FactIndexKind Kind { get; init; } = FactIndexKind.Equality;

    public bool Unique { get; init; }

    public string? Name { get; init; }
}
