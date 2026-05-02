namespace Fletched.Core;

/// <summary>Marks a <c>partial record struct</c> as a predicate definition.</summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class PredicateAttribute : Attribute { }
