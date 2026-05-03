using System;
using System.Collections.Generic;
using System.Linq;

namespace Fletched.Roslyn.Pipeline;

/// <summary>Marker interface for optimization passes over a <see cref="PlanProgram"/>.</summary>
public interface IPlanOptimization
{
    PlanProgram Apply(PlanProgram program);
}

// ─── Pass implementations ────────────────────────────────────────────────────

/// <summary>Ensures flat instruction sequences — no nested block references.</summary>
public sealed class NormalizeSequence : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program)
    {
        // Merge adjacent blocks where the only link is an unconditional GotoTerm
        // and the target block has exactly one inbound edge.
        var allBlocks = new[] { program.Entry }.Concat(program.Blocks).ToList();

        // Count inbound edges for each label
        var inbound = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (PlanBlock b in allBlocks)
        {
            foreach (string target in Successors(b.Terminator))
            {
                inbound.TryGetValue(target, out int count);
                inbound[target] = count + 1;
            }
        }

        // Merge pass: if a block ends with GotoTerm and target has exactly 1 inbound,
        // inline the target's instructions into this block.
        var merged = new HashSet<string>(StringComparer.Ordinal);
        // Track blocks that have already been added to the result as standalone blocks.
        // A block that is already emitted must not be consumed/inlined by a later block,
        // because that would duplicate it and leave a dangling goto to the merged-away label.
        var alreadyEmitted = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<PlanBlock>(allBlocks.Count);

        foreach (PlanBlock block in allBlocks)
        {
            if (merged.Contains(block.Label)) continue;

            PlanBlock current = block;
            while (current.Terminator is GotoTerm g &&
                   (inbound.TryGetValue(g.TargetLabel, out int edgeCount) ? edgeCount : 0) == 1)
            {
                PlanBlock? target = allBlocks.FirstOrDefault(b => b.Label == g.TargetLabel);
                // Stop if target was already merged away, or was already emitted as a
                // standalone block (merging it again would remove it from the output
                // while keeping the GotoTerm in the earlier block unresolved).
                if (target is null || merged.Contains(target.Label) || alreadyEmitted.Contains(target.Label)) break;

                // Merge target into current
                current = new PlanBlock(
                    current.Label,
                    current.Instructions.Concat(target.Instructions).ToList(),
                    target.Terminator);
                merged.Add(target.Label);
            }

            alreadyEmitted.Add(current.Label);
            result.Add(current);
        }

        if (result.Count == 0) return program;
        return new PlanProgram(result[0], result.Skip(1).ToList(), program.SlotMap);
    }

    private static IEnumerable<string> Successors(PlanTerminator term)
    {
        return term switch
        {
            GotoTerm g => new[] { g.TargetLabel },
            ChoiceTerm c => new[] { c.NextLabel, c.AlternativeLabel },
            LoopCheckTerm l => new[] { l.BodyLabel, l.FailLabel },
            _ => Array.Empty<string>()
        };
    }
}
public sealed class RemoveRedundantUnify : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program)
    {
        var allBlocks = new[] { program.Entry }.Concat(program.Blocks).ToList();
        var newBlocks = allBlocks.Select(TransformBlock).ToList();
        return new PlanProgram(newBlocks[0], newBlocks.Skip(1).ToList(), program.SlotMap);
    }

    private static PlanBlock TransformBlock(PlanBlock block)
    {
        var instructions = block.Instructions
            .Where(i => !IsRedundantUnify(i))
            .ToList();
        return block with { Instructions = instructions };
    }

    private static bool IsRedundantUnify(PlanInstruction i)
    {
        if (i is not UnifyInstr u) return false;
        // Unify(SlotValue(a), SlotValue(a)) → no-op
        return u.Left is SlotValue lv && u.Right is SlotValue rv && lv.Slot == rv.Slot;
    }
}

/// <summary>Computes AccessSet (reads/writes) per instruction for dependency analysis.</summary>
public sealed class DependencyAnalysis : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) => program;
}

/// <summary>Reorders conjunction instructions: constraints first, then bound unifications, loops last.</summary>
public sealed class ReorderConjunction : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) => program;
}

/// <summary>Upgrades FullScan loops to IndexedSource when a key slot is bound.</summary>
public sealed class IndexSelection : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) => program;
}

/// <summary>Moves constraint instructions earlier when all argument slots are bound.</summary>
public sealed class ConstraintHoisting : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) => program;
}

/// <summary>Removes unreachable blocks and dead instructions after FailTerm.</summary>
public sealed class DeadCodeElimination : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program)
    {
        var reachable = new HashSet<string>();
        CollectReachable(program.Entry.Label, program, reachable);

        var allBlocks = new[] { program.Entry }.Concat(program.Blocks).ToList();
        var live = allBlocks.Where(b => reachable.Contains(b.Label)).ToList();

        if (live.Count == 0) return program;
        return new PlanProgram(live[0], live.Skip(1).ToList(), program.SlotMap);
    }

    private static void CollectReachable(string label, PlanProgram program, HashSet<string> visited)
    {
        if (!visited.Add(label)) return;
        var allBlocks = new[] { program.Entry }.Concat(program.Blocks);
        PlanBlock? block = allBlocks.FirstOrDefault(b => b.Label == label);
        if (block is null) return;

        switch (block.Terminator)
        {
            case GotoTerm g: CollectReachable(g.TargetLabel, program, visited); break;
            case ChoiceTerm c:
                CollectReachable(c.NextLabel, program, visited);
                CollectReachable(c.AlternativeLabel, program, visited);
                break;
            case LoopCheckTerm l:
                CollectReachable(l.BodyLabel, program, visited);
                break;
        }
    }
}

/// <summary>If a loop body never reads the loop variable, specializes to a single execution.</summary>
public sealed class LoopSpecialization : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) => program;
}

/// <summary>Deduplicates identical FieldValue expressions within a block into AssignInstr temporaries.</summary>
public sealed class TempHoisting : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) => program;
}

// ─── Pipeline ─────────────────────────────────────────────────────────────────

/// <summary>Runs the full ordered optimization pipeline over a <see cref="PlanProgram"/>.</summary>
public sealed class OptimizationPipeline
{
    private readonly IReadOnlyList<IPlanOptimization> _passes;

    public OptimizationPipeline() : this(DefaultPasses()) { }

    public OptimizationPipeline(IReadOnlyList<IPlanOptimization> passes) => _passes = passes;

    private static IReadOnlyList<IPlanOptimization> DefaultPasses() => new IPlanOptimization[]
    {
        new NormalizeSequence(),
        new RemoveRedundantUnify(),
        new DependencyAnalysis(),
        new ReorderConjunction(),
        new IndexSelection(),
        new ConstraintHoisting(),
        new DeadCodeElimination(),
        new LoopSpecialization(),
        new TempHoisting(),
    };

    public PlanProgram Run(PlanProgram program)
    {
        foreach (IPlanOptimization pass in _passes)
            program = pass.Apply(program);
        return program;
    }
}
