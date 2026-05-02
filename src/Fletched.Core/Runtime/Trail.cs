namespace Fletched.Core.Runtime;

/// <summary>Records a single variable binding for trail-based backtracking.</summary>
public struct TrailEntry
{
    public int Slot;
    public bool WasBound;
}

/// <summary>
/// A stack-allocated trail of variable bindings.
/// Uses a fixed-size <see cref="Span{T}"/> buffer supplied by the generated state struct.
/// </summary>
public ref struct Trail
{
    private Span<TrailEntry> _entries;
    private int _top;

    public Trail(Span<TrailEntry> buffer)
    {
        _entries = buffer;
        _top = 0;
    }

    /// <summary>Current trail top — used to capture state before a choice point.</summary>
    public int Top => _top;

    /// <summary>Records a binding before it is made.</summary>
    public void Push(int slot, bool wasBound)
    {
        _entries[_top++] = new TrailEntry { Slot = slot, WasBound = wasBound };
    }

    /// <summary>
    /// Pops the trail back to <paramref name="targetTop"/>, invoking
    /// <paramref name="unbind"/> for each entry that is unwound.
    /// </summary>
    public void UnwindTo(int targetTop, UnbindCallback unbind)
    {
        while (_top > targetTop)
        {
            TrailEntry entry = _entries[--_top];
            unbind(entry.Slot, entry.WasBound);
        }
    }
}

/// <summary>Callback used by <see cref="Trail.UnwindTo"/> to restore bound flags.</summary>
public delegate void UnbindCallback(int slot, bool wasBound);
