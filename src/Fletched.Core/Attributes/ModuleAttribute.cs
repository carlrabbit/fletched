using System;

namespace Fletched.Core;

/// <summary>Marks a <c>partial class</c> as a relational module boundary.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleAttribute : Attribute { }
