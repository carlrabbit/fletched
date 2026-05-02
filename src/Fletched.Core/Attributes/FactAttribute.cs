using System;

namespace Fletched.Core;

/// <summary>Marks a <c>partial record struct</c> as a relational fact type.</summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class FactAttribute : Attribute { }
