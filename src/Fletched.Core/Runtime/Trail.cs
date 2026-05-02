using System;

namespace Fletched.Core.Runtime;

/// <summary>Records a single variable binding for trail-based backtracking.</summary>
public struct TrailEntry
{
    public int Slot;
    public bool WasBound;
}

/// <summary>
/// Heap-allocated trail of variable bindings used during predicate execution.
/// </summary>
public sealed class Trail
{
    private readonly TrailEntry[] _entries;
    private int _top;

    public Trail(int capacity = 256)
    {
        _entries = new TrailEntry[capacity];
        _top = 0;
    }

    /// <summary>Current trail top — used to capture state before a choice point.</summary>
    public int Top => _top;

    /// <summary>Records a binding before it is made.</summary>
    public void Push(int slot, bool wasBound)
    {
        _entries[_top++] = new TrailEntry { Slot = slot, WasBound = wasBound };
    }

    /// <summary>Pops one entry from the trail (used for inline unwind in generated code).</summary>
    public TrailEntry PopEntry() => _entries[--_top];

    /// <summary>
    /// Unwinds the trail back to <paramref name="targetTop"/>, invoking
    /// <paramref name="unbind"/> for each entry that is undone.
    /// </summary>
    public void UnwindTo(int targetTop, Action<int, bool> unbind)
    {
        while (_top > targetTop)
        {
            TrailEntry entry = _entries[--_top];
            unbind(entry.Slot, entry.WasBound);
        }
    }
}

