using System;

namespace Fletched.Core;

/// <summary>Declares a predicate as tabled for recursive execution.</summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class TabledAttribute : Attribute
{
    /// <summary>Creates a tabled predicate declaration using variant tabling.</summary>
    public TabledAttribute()
        : this(TablingMode.Variant)
    {
    }

    /// <summary>Creates a tabled predicate declaration with the provided tabling mode.</summary>
    public TabledAttribute(TablingMode mode)
    {
        Mode = mode;
    }

    /// <summary>Configured tabling mode for this predicate.</summary>
    public TablingMode Mode { get; }
}

/// <summary>Supported tabling modes for predicate declarations.</summary>
public enum TablingMode
{
    /// <summary>Variant tabling where equivalent calls modulo variable names share a table.</summary>
    Variant = 0,
    /// <summary>Subsumptive tabling mode (unsupported in the current milestone).</summary>
    Subsumptive = 1
}
