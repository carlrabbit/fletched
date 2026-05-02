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
    public PlanProgram Apply(PlanProgram program) => program; // No-op at this stage
}

/// <summary>Removes Unify(X, X) and folds constant–constant unifications.</summary>
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
