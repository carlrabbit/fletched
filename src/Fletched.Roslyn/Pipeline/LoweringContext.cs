using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

/// <summary>Mutable context during lowering: slot allocation, label generation, block accumulation.</summary>
public sealed class LoweringContext
{
    private readonly Dictionary<VariableSymbol, int> _slotMap = new();
    private readonly List<PlanBlock> _blocks = new();
    private readonly Stack<string> _loopBodyLabels = new();
    private int _labelCounter;
    private int _slotCounter;
    private int _indexCounter;

    public IReadOnlyDictionary<VariableSymbol, int> SlotMap => _slotMap;

    public int AllocateSlot(VariableSymbol variable)
    {
        if (_slotMap.TryGetValue(variable, out int existing)) return existing;
        int id = _slotCounter++;
        _slotMap[variable] = id;
        return id;
    }

    public int GetSlot(VariableSymbol variable)
    {
        if (_slotMap.TryGetValue(variable, out int slot)) return slot;
        // Allocate on demand for unresolved references
        return AllocateSlot(variable);
    }

    /// <summary>Allocates an anonymous (temporary) slot not associated with any variable.</summary>
    public int AllocateAnonymousSlot() => _slotCounter++;

    public string NextLabel(string hint = "L")
    {
        return $"{hint}_{_labelCounter++}";
    }

    /// <summary>Peeks what the next label with the given hint would be (without allocating).</summary>
    public string PeekNextLabel(string hint) => $"{hint}_{_labelCounter}";

    public string NextIndexVar() => $"_idx{_indexCounter++}";

    public void AddBlock(PlanBlock block) => _blocks.Add(block);

    public PlanBlock? FindBlock(string label) =>
        _blocks.FirstOrDefault(b => b.Label == label);

    public void PushLoopContext(string bodyLabel, string nextLabel, string indexVar) =>
        _loopBodyLabels.Push(bodyLabel);

    public string? PopLoopContext() =>
        _loopBodyLabels.Count > 0 ? _loopBodyLabels.Pop() : null;

    public List<PlanBlock> FinalizeBlocks() => new(_blocks);
}
