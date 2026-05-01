namespace Fletched.Core;

/// <summary>Marks a partial record struct or class as a predicate type whose body is compiled by the Fletched engine.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PredicateAttribute : Attribute { }
