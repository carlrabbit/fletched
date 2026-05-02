using System;

namespace Fletched.Core;

/// <summary>Marks the body method inside a <see cref="PredicateAttribute"/> type.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PredicateBodyAttribute : Attribute { }
