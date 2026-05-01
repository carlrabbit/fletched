namespace Fletched.Core;

/// <summary>Marks a partial record struct or class as a fact type to be indexed and queried by the Fletched engine.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class FactAttribute : Attribute { }
