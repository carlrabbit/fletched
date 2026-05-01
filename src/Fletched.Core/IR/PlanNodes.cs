namespace Fletched.Core.IR;

/// <summary>Base node for all planned IR nodes.</summary>
public abstract record PlanNode;

/// <summary>The top-level planned program, consisting of an entry block and all blocks.</summary>
public record PlanProgram(
    PlanBlock Entry,
    IReadOnlyList<PlanBlock> Blocks
);

/// <summary>A basic block in the planned IR, identified by a label.</summary>
public record PlanBlock(
    string Label,
    IReadOnlyList<PlanInstruction> Instructions,
    PlanTerminator Terminator
);

/// <summary>Base node for all planned IR instructions.</summary>
public abstract record PlanInstruction : PlanNode;

/// <summary>Base node for all planned IR block terminators.</summary>
public abstract record PlanTerminator : PlanNode;

/// <summary>Terminates a block by returning from the current program.</summary>
public sealed record ReturnTerminator : PlanTerminator;

/// <summary>Terminates a block with an unconditional jump to another block.</summary>
public sealed record GotoTerminator(string Target) : PlanTerminator;
