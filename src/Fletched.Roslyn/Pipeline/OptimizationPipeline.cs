using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

/// <summary>Marker interface for optimization passes over a <see cref="PlanProgram"/>.</summary>
public interface IPlanOptimization
{
    string Name { get; }

    PlanOptimizationResult Optimize(
        PlanProgram program,
        PlanOptimizationContext context);
}

public static class PlanOptimizationExtensions
{
    private static readonly PlanOptimizationContext DefaultContext = new()
    {
        Options = new OptimizationOptions()
    };

    public static PlanProgram Apply(this IPlanOptimization pass, PlanProgram program) =>
        pass.Optimize(program, DefaultContext).Program;
}

internal readonly record struct AccessSet(
    int[] Reads,
    int[] Writes);

internal static class PlanAnalysis
{
    public const string FailLabel = "Fail";

    public static IReadOnlyList<PlanBlock> AllBlocks(PlanProgram program) =>
        [program.Entry, .. program.Blocks];

    public static PlanProgram RewriteBlocks(PlanProgram program, Func<PlanBlock, PlanBlock> transform)
    {
        List<PlanBlock> blocks = AllBlocks(program).Select(transform).ToList();
        return blocks.Count == 0
            ? program
            : new PlanProgram(blocks[0], blocks.Skip(1).ToList(), program.SlotMap, program.Metadata);
    }

    public static IReadOnlyList<AccessSet> AnalyzeBlock(PlanBlock block) =>
        block.Instructions.Select(AnalyzeInstruction).ToList();

    public static AccessSet AnalyzeInstruction(PlanInstruction instruction)
    {
        HashSet<int> reads = [];
        HashSet<int> writes = [];

        switch (instruction)
        {
            case UnifyInstr unify:
                CollectReads(unify.Left, reads);
                CollectReads(unify.Right, reads);
                CollectDirectSlotWrites(unify.Left, writes);
                CollectDirectSlotWrites(unify.Right, writes);
                break;

            case ConstraintInstr constraint:
                foreach (PlanValue argument in constraint.Arguments)
                    CollectReads(argument, reads);
                break;

            case AssignInstr assign:
                writes.Add(assign.Slot);
                CollectReads(assign.Value, reads);
                break;

            case CompInstr comp:
                CollectReads(comp.Left, reads);
                CollectReads(comp.Right, reads);
                break;

            case LoopBindInstr bind:
                writes.Add(bind.Slot);
                if (bind.IndexedLookup is not null)
                    CollectReads(bind.IndexedLookup.Key, reads);
                break;

            case IndexInitInstr init when init.IndexedLookup is not null:
                CollectReads(init.IndexedLookup.Key, reads);
                break;

            case CallInstr call:
                foreach (int slot in call.ArgumentSlots)
                {
                    reads.Add(slot);
                    writes.Add(slot);
                }
                break;

            case NotInstr not:
                foreach (PlanInstruction subGoalInstruction in not.SubGoalInstructions)
                {
                    AccessSet access = AnalyzeInstruction(subGoalInstruction);
                    foreach (int slot in access.Reads)
                        reads.Add(slot);
                }
                break;
        }

        return new AccessSet(reads.ToArray(), writes.ToArray());
    }

    public static bool MustPrecede(AccessSet earlier, AccessSet later)
    {
        return Overlaps(earlier.Writes, later.Reads)
            || Overlaps(earlier.Writes, later.Writes)
            || Overlaps(earlier.Reads, later.Writes);
    }

    public static IReadOnlyList<PlanInstruction> ReorderInstructions(
        IReadOnlyList<PlanInstruction> instructions,
        Func<PlanInstruction, int> priority)
    {
        if (instructions.Count < 2)
            return instructions;

        List<AccessSet> accesses = instructions.Select(AnalyzeInstruction).ToList();
        List<HashSet<int>> outgoing = Enumerable.Range(0, instructions.Count).Select(_ => new HashSet<int>()).ToList();
        int[] indegree = new int[instructions.Count];

        for (int from = 0; from < instructions.Count; from++)
        {
            for (int to = from + 1; to < instructions.Count; to++)
            {
                if (!MustPrecede(accesses[from], accesses[to]))
                    continue;

                if (outgoing[from].Add(to))
                    indegree[to]++;
            }
        }

        var remaining = new HashSet<int>(Enumerable.Range(0, instructions.Count));
        var ordered = new List<PlanInstruction>(instructions.Count);

        while (remaining.Count > 0)
        {
            int next = remaining
                .Where(index => indegree[index] == 0)
                .OrderBy(index => priority(instructions[index]))
                .ThenBy(index => index)
                .DefaultIfEmpty(-1)
                .First();

            if (next < 0)
                return instructions;

            ordered.Add(instructions[next]);
            remaining.Remove(next);

            foreach (int successor in outgoing[next])
                indegree[successor]--;
        }

        return ordered;
    }

    public static bool TryEvaluateInstruction(PlanInstruction instruction, out bool alwaysSucceeds, out bool alwaysFails)
    {
        alwaysSucceeds = false;
        alwaysFails = false;

        switch (instruction)
        {
            case UnifyInstr unify:
                if (Equals(unify.Left, unify.Right))
                {
                    alwaysSucceeds = true;
                    return true;
                }

                if (unify.Left is ConstValue or ListConsValue or ListEmptyValue
                    && unify.Right is ConstValue or ListConsValue or ListEmptyValue)
                {
                    alwaysSucceeds = Equals(unify.Left, unify.Right);
                    alwaysFails = !alwaysSucceeds;
                    return true;
                }

                return false;

            case CompInstr comp when TryEvaluateValue(comp.Left, out IComparable? left)
                                     && TryEvaluateValue(comp.Right, out IComparable? right):
                {
                    int comparison = Comparer<IComparable>.Default.Compare(left, right);
                    alwaysSucceeds = comp.Op switch
                    {
                        CompOp.NotEqual => comparison != 0,
                        CompOp.LessThan => comparison < 0,
                        CompOp.GreaterThan => comparison > 0,
                        CompOp.LessThanOrEqual => comparison <= 0,
                        CompOp.GreaterThanOrEqual => comparison >= 0,
                        _ => false,
                    };
                    alwaysFails = !alwaysSucceeds;
                    return true;
                }

            default:
                return false;
        }
    }

    public static int NextAnonymousSlot(PlanProgram program)
    {
        int maxSlot = -1;

        foreach (PlanBlock block in AllBlocks(program))
        {
            foreach (PlanInstruction instruction in block.Instructions)
                maxSlot = Math.Max(maxSlot, MaxReferencedSlot(instruction));
        }

        return maxSlot + 1;
    }

    public static IEnumerable<string> Successors(PlanTerminator term)
    {
        return term switch
        {
            GotoTerm gotoTerm => [gotoTerm.TargetLabel],
            ChoiceTerm choice => [choice.NextLabel, choice.AlternativeLabel],
            LoopCheckTerm loop => [loop.BodyLabel, loop.FailLabel],
            _ => Array.Empty<string>()
        };
    }

    public static PlanBlock? FindBlock(PlanProgram program, string label) =>
        AllBlocks(program).FirstOrDefault(candidate => candidate.Label == label);

    private static int MaxReferencedSlot(PlanInstruction instruction)
    {
        int maxSlot = -1;

        switch (instruction)
        {
            case UnifyInstr unify:
                maxSlot = Math.Max(MaxReferencedSlot(unify.Left), MaxReferencedSlot(unify.Right));
                break;

            case ConstraintInstr constraint:
                foreach (PlanValue argument in constraint.Arguments)
                    maxSlot = Math.Max(maxSlot, MaxReferencedSlot(argument));
                break;

            case AssignInstr assign:
                maxSlot = Math.Max(assign.Slot, MaxReferencedSlot(assign.Value));
                break;

            case CompInstr comp:
                maxSlot = Math.Max(MaxReferencedSlot(comp.Left), MaxReferencedSlot(comp.Right));
                break;

            case LoopBindInstr bind:
                maxSlot = bind.IndexedLookup is null
                    ? bind.Slot
                    : Math.Max(bind.Slot, MaxReferencedSlot(bind.IndexedLookup.Key));
                break;

            case IndexInitInstr init when init.IndexedLookup is not null:
                maxSlot = MaxReferencedSlot(init.IndexedLookup.Key);
                break;

            case CallInstr call when call.ArgumentSlots.Count > 0:
                maxSlot = call.ArgumentSlots.Max();
                break;

            case NotInstr not:
                foreach (PlanInstruction subGoalInstruction in not.SubGoalInstructions)
                    maxSlot = Math.Max(maxSlot, MaxReferencedSlot(subGoalInstruction));
                break;
        }

        return maxSlot;
    }

    private static int MaxReferencedSlot(PlanValue value)
    {
        return value switch
        {
            SlotValue slot => slot.Slot,
            FieldValue field => MaxReferencedSlot(field.Target),
            ArithValue arith => Math.Max(MaxReferencedSlot(arith.Left), MaxReferencedSlot(arith.Right)),
            ListConsValue cons => Math.Max(MaxReferencedSlot(cons.Head), MaxReferencedSlot(cons.Tail)),
            _ => -1,
        };
    }

    private static void CollectReads(PlanValue value, HashSet<int> reads)
    {
        switch (value)
        {
            case SlotValue slot:
                reads.Add(slot.Slot);
                break;

            case FieldValue field:
                CollectReads(field.Target, reads);
                break;

            case ArithValue arith:
                CollectReads(arith.Left, reads);
                CollectReads(arith.Right, reads);
                break;

            case ListConsValue cons:
                CollectReads(cons.Head, reads);
                CollectReads(cons.Tail, reads);
                break;
        }
    }

    private static void CollectDirectSlotWrites(PlanValue value, HashSet<int> writes)
    {
        if (value is SlotValue slot)
            writes.Add(slot.Slot);
    }

    private static bool TryEvaluateValue(PlanValue value, out IComparable? comparable)
    {
        comparable = null;

        if (value is not ConstValue constant || constant.Value is not IComparable result)
            return false;

        comparable = result;
        return true;
    }

    private static bool Overlaps(IEnumerable<int> left, IEnumerable<int> right)
    {
        var rightSet = new HashSet<int>(right);
        return left.Any(rightSet.Contains);
    }
}

// ─── Pass implementations ────────────────────────────────────────────────────

/// <summary>Ensures flat instruction sequences — no nested block references.</summary>
public sealed class NormalizeSequence : IPlanOptimization
{
    public string Name => nameof(NormalizeSequence);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        List<PlanBlock> allBlocks = PlanAnalysis.AllBlocks(program).ToList();

        var inbound = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (PlanBlock block in allBlocks)
        {
            foreach (string target in PlanAnalysis.Successors(block.Terminator))
            {
                inbound.TryGetValue(target, out int count);
                inbound[target] = count + 1;
            }
        }

        var merged = new HashSet<string>(StringComparer.Ordinal);
        var alreadyEmitted = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<PlanBlock>(allBlocks.Count);

        foreach (PlanBlock block in allBlocks)
        {
            if (merged.Contains(block.Label))
                continue;

            PlanBlock current = block;
            while (current.Terminator is GotoTerm gotoTerm
                   && (inbound.TryGetValue(gotoTerm.TargetLabel, out int edgeCount) ? edgeCount : 0) == 1)
            {
                PlanBlock? target = allBlocks.FirstOrDefault(candidate => candidate.Label == gotoTerm.TargetLabel);
                if (target is null || merged.Contains(target.Label) || alreadyEmitted.Contains(target.Label))
                    break;

                current = new PlanBlock(
                    current.Label,
                    current.Instructions.Concat(target.Instructions).ToList(),
                    target.Terminator);
                merged.Add(target.Label);
            }

            alreadyEmitted.Add(current.Label);
            result.Add(current);
        }

        PlanProgram optimized = result.Count == 0
            ? program
            : new PlanProgram(result[0], result.Skip(1).ToList(), program.SlotMap, program.Metadata);
        return new PlanOptimizationResult(optimized, ImmutableArray<PlanOptimizationChange>.Empty);
    }
}

public sealed class RemoveRedundantUnify : IPlanOptimization
{
    public string Name => nameof(RemoveRedundantUnify);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram optimized = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, changes));
        return new PlanOptimizationResult(optimized, changes.ToImmutable());
    }

    private PlanBlock TransformBlock(PlanBlock block, ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        var instructions = new List<PlanInstruction>(block.Instructions.Count);
        PlanTerminator terminator = block.Terminator;

        for (int index = 0; index < block.Instructions.Count; index++)
        {
            PlanInstruction instruction = block.Instructions[index];
            if (!PlanAnalysis.TryEvaluateInstruction(instruction, out bool alwaysSucceeds, out bool alwaysFails))
            {
                instructions.Add(instruction);
                continue;
            }

            if (instruction is UnifyInstr)
            {
                changes.Add(new PlanOptimizationChange(
                    Name,
                    PlanChangeKind.SimplifiedUnification,
                    $"{block.Label}:{index}",
                    alwaysFails ? "provable-constant-mismatch" : "redundant-unify"));
            }

            if (alwaysSucceeds)
                continue;

            if (alwaysFails)
            {
                terminator = new FailTerm();
                break;
            }
        }

        return block with { Instructions = instructions, Terminator = terminator };
    }
}

/// <summary>Computes AccessSet (reads/writes) per instruction for dependency analysis.</summary>
public sealed class DependencyAnalysis : IPlanOptimization
{
    public string Name => nameof(DependencyAnalysis);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(program))
            _ = PlanAnalysis.AnalyzeBlock(block);

        return new PlanOptimizationResult(program, ImmutableArray<PlanOptimizationChange>.Empty);
    }
}

/// <summary>Reorders conjunction instructions: constraints first, then bound unifications, loops last.</summary>
public sealed class ReorderConjunction : IPlanOptimization
{
    public string Name => nameof(ReorderConjunction);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram optimized = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, changes));
        return new PlanOptimizationResult(optimized, changes.ToImmutable());
    }

    private PlanBlock TransformBlock(PlanBlock block, ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        if (block.Instructions.Any(IsBarrierInstruction))
            return block;

        IReadOnlyList<PlanInstruction> reordered = PlanAnalysis.ReorderInstructions(block.Instructions, GetPriority);
        if (reordered.SequenceEqual(block.Instructions))
            return block;

        changes.Add(new PlanOptimizationChange(
            Name,
            PlanChangeKind.ReorderedConjunction,
            block.Label,
            "dependency-safe-priority-reordering"));
        return block with { Instructions = reordered.ToList() };
    }

    private static int GetPriority(PlanInstruction instruction)
    {
        return instruction switch
        {
            ConstraintInstr or CompInstr => 0,
            UnifyInstr => 1,
            AssignInstr => 2,
            IndexInitInstr or LoopBindInstr or IndexIncrInstr => 3,
            _ => 4,
        };
    }

    private static bool IsBarrierInstruction(PlanInstruction instruction) =>
        instruction is CallInstr or NotInstr;
}

/// <summary>Promotes loop key-filter checks as early as dependency ordering allows.</summary>
public sealed class IndexSelection : IPlanOptimization
{
    public string Name => nameof(IndexSelection);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram optimized = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, changes));
        return new PlanOptimizationResult(optimized, changes.ToImmutable());
    }

    private PlanBlock TransformBlock(PlanBlock block, ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        if (block.Instructions.Any(instruction => instruction is CallInstr or NotInstr))
            return block;

        IReadOnlyList<PlanInstruction> reordered = PlanAnalysis.ReorderInstructions(block.Instructions, GetPriority);
        if (reordered.SequenceEqual(block.Instructions))
            return block;

        changes.Add(new PlanOptimizationChange(
            Name,
            PlanChangeKind.SelectedIndex,
            block.Label,
            "promoted-loop-key-filter"));
        return block with { Instructions = reordered.ToList() };
    }

    private static int GetPriority(PlanInstruction instruction)
    {
        return instruction switch
        {
            UnifyInstr unify when IsCandidateKeyFilter(unify) => 0,
            CompInstr comp when IsLoopFieldComparison(comp) => 0,
            _ => 1,
        };
    }

    private static bool IsCandidateKeyFilter(UnifyInstr unify)
    {
        return IsLoopField(unify.Left) && unify.Right is SlotValue or ConstValue
            || IsLoopField(unify.Right) && unify.Left is SlotValue or ConstValue;
    }

    private static bool IsLoopFieldComparison(CompInstr comparison) =>
        IsLoopField(comparison.Left) || IsLoopField(comparison.Right);

    private static bool IsLoopField(PlanValue value) =>
        value is FieldValue { Target: SlotValue };
}

/// <summary>Moves constraint instructions earlier when all argument slots are bound.</summary>
public sealed class ConstraintHoisting : IPlanOptimization
{
    public string Name => nameof(ConstraintHoisting);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram optimized = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, changes));
        return new PlanOptimizationResult(optimized, changes.ToImmutable());
    }

    private PlanBlock TransformBlock(PlanBlock block, ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        if (block.Instructions.Count < 2)
            return block;

        List<PlanInstruction> reordered = block.Instructions.ToList();
        bool changed = false;
        for (int index = 1; index < reordered.Count; index++)
        {
            PlanInstruction current = reordered[index];
            if (current is not ConstraintInstr and not CompInstr)
                continue;

            AccessSet currentAccess = PlanAnalysis.AnalyzeInstruction(current);
            int targetIndex = index;

            while (targetIndex > 0)
            {
                AccessSet previousAccess = PlanAnalysis.AnalyzeInstruction(reordered[targetIndex - 1]);
                if (PlanAnalysis.MustPrecede(previousAccess, currentAccess)
                    || reordered[targetIndex - 1] is CallInstr or NotInstr)
                {
                    break;
                }

                targetIndex--;
            }

            if (targetIndex == index)
                continue;

            reordered.RemoveAt(index);
            reordered.Insert(targetIndex, current);
            changes.Add(new PlanOptimizationChange(
                Name,
                PlanChangeKind.HoistedConstraint,
                $"{block.Label}:{targetIndex}",
                "arguments-bound-earlier"));
            changed = true;
        }

        return changed ? block with { Instructions = reordered } : block;
    }
}

/// <summary>Removes pure assignment instructions whose written slot is never subsequently read.</summary>
public sealed class DeadBindingElimination : IPlanOptimization
{
    public string Name => nameof(DeadBindingElimination);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        if (!context.Options.EnableDeadBindingElimination)
            return new PlanOptimizationResult(program, ImmutableArray<PlanOptimizationChange>.Empty);

        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram result = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, changes));
        return new PlanOptimizationResult(result, changes.ToImmutable());
    }

    private PlanBlock TransformBlock(PlanBlock block, ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        if (block.Instructions.Count < 2)
            return block;

        List<PlanInstruction> instructions = block.Instructions.ToList();
        HashSet<int>[] readsAfter = new HashSet<int>[instructions.Count + 1];
        readsAfter[instructions.Count] = [];

        for (int index = instructions.Count - 1; index >= 0; index--)
        {
            readsAfter[index] = new HashSet<int>(readsAfter[index + 1]);
            AccessSet access = PlanAnalysis.AnalyzeInstruction(instructions[index]);
            foreach (int slot in access.Reads)
                readsAfter[index].Add(slot);
        }

        var result = new List<PlanInstruction>(instructions.Count);
        bool changed = false;

        for (int i = 0; i < instructions.Count; i++)
        {
            PlanInstruction instr = instructions[i];
            if (instr is AssignInstr assign && !readsAfter[i + 1].Contains(assign.Slot))
            {
                changes.Add(new PlanOptimizationChange(
                    Name,
                    PlanChangeKind.RemovedDeadBinding,
                    $"slot_{assign.Slot}",
                    "pure-assignment-never-read"));
                changed = true;
                continue;
            }

            result.Add(instr);
        }

        return changed ? block with { Instructions = result } : block;
    }
}

/// <summary>Removes unreachable blocks and dead instructions after a provable failure.</summary>
public sealed class DeadCodeElimination : IPlanOptimization
{
    public string Name => nameof(DeadCodeElimination);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram simplified = PlanAnalysis.RewriteBlocks(program, block => TrimDeadInstructions(block, changes));

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        CollectReachable(simplified.Entry.Label, simplified, reachable);

        List<PlanBlock> allBlocks = PlanAnalysis.AllBlocks(simplified).ToList();
        foreach (PlanBlock block in allBlocks.Where(block => !reachable.Contains(block.Label)))
        {
            changes.Add(new PlanOptimizationChange(
                Name,
                PlanChangeKind.RemovedUnreachableBlock,
                block.Label,
                "unreachable-from-entry"));
        }

        List<PlanBlock> live = allBlocks
            .Where(block => reachable.Contains(block.Label))
            .ToList();

        PlanProgram optimized = live.Count == 0
            ? simplified
            : new PlanProgram(live[0], live.Skip(1).ToList(), simplified.SlotMap, simplified.Metadata);
        return new PlanOptimizationResult(optimized, changes.ToImmutable());
    }

    private PlanBlock TrimDeadInstructions(PlanBlock block, ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        var instructions = new List<PlanInstruction>(block.Instructions.Count);
        PlanTerminator terminator = block.Terminator;

        for (int index = 0; index < block.Instructions.Count; index++)
        {
            PlanInstruction instruction = block.Instructions[index];
            if (PlanAnalysis.TryEvaluateInstruction(instruction, out _, out bool alwaysFails) && alwaysFails)
            {
                changes.Add(new PlanOptimizationChange(
                    Name,
                    PlanChangeKind.RemovedInstruction,
                    $"{block.Label}:{index}",
                    "provable-failure-rewritten-to-fail"));

                for (int trailing = index + 1; trailing < block.Instructions.Count; trailing++)
                {
                    changes.Add(new PlanOptimizationChange(
                        Name,
                        PlanChangeKind.RemovedInstruction,
                        $"{block.Label}:{trailing}",
                        "after-unconditional-fail"));
                }

                terminator = new FailTerm();
                break;
            }

            instructions.Add(instruction);
        }

        return block with { Instructions = instructions, Terminator = terminator };
    }

    private static void CollectReachable(string label, PlanProgram program, HashSet<string> visited)
    {
        if (!visited.Add(label))
            return;

        PlanBlock? block = PlanAnalysis.FindBlock(program, label);
        if (block is null)
            return;

        foreach (string successor in PlanAnalysis.Successors(block.Terminator))
        {
            if (string.Equals(successor, PlanAnalysis.FailLabel, StringComparison.Ordinal))
                continue;

            CollectReachable(successor, program, visited);
        }
    }
}

/// <summary>Detects loop-invariant bodies so future lowering can specialize them safely.</summary>
public sealed class LoopSpecialization : IPlanOptimization
{
    public string Name => nameof(LoopSpecialization);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        if (!context.Options.EnableLoopSpecialization)
            return new PlanOptimizationResult(program, ImmutableArray<PlanOptimizationChange>.Empty);

        if (!context.Options.EmitOptimizationTrace)
            return new PlanOptimizationResult(program, ImmutableArray<PlanOptimizationChange>.Empty);

        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(program))
        {
            if (block.Terminator is LoopCheckTerm loop)
            {
                changes.Add(new PlanOptimizationChange(
                    Name,
                    PlanChangeKind.SpecializedLoop,
                    block.Label,
                    $"loop-check:{loop.IndexVar}:analysis-only"));
            }

            foreach (LoopBindInstr bind in block.Instructions.OfType<LoopBindInstr>())
            {
                changes.Add(new PlanOptimizationChange(
                    Name,
                    PlanChangeKind.SpecializedLoop,
                    $"{block.Label}:slot_{bind.Slot}",
                    $"loop-bind:{bind.IndexVar}:analysis-only"));
            }
        }

        return new PlanOptimizationResult(program, changes.ToImmutable());
    }
}

/// <summary>Deduplicates repeated field reads in read-only instruction segments using temporaries.</summary>
public sealed class TempHoisting : IPlanOptimization
{
    public string Name => nameof(TempHoisting);

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        int nextSlot = PlanAnalysis.NextAnonymousSlot(program);
        PlanProgram optimized = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, ref nextSlot));
        return new PlanOptimizationResult(optimized, ImmutableArray<PlanOptimizationChange>.Empty);
    }

    private static PlanBlock TransformBlock(PlanBlock block, ref int nextSlot)
    {
        if (block.Instructions.Count < 2)
            return block;

        var rewritten = new List<PlanInstruction>(block.Instructions.Count);
        var segment = new List<PlanInstruction>();

        foreach (PlanInstruction instruction in block.Instructions)
        {
            AccessSet access = PlanAnalysis.AnalyzeInstruction(instruction);
            if (access.Writes.Length > 0)
            {
                FlushSegment(segment, rewritten, ref nextSlot);
                rewritten.Add(instruction);
                continue;
            }

            segment.Add(instruction);
        }

        FlushSegment(segment, rewritten, ref nextSlot);
        return block with { Instructions = rewritten };
    }

    private static void FlushSegment(List<PlanInstruction> segment, List<PlanInstruction> rewritten, ref int nextSlot)
    {
        if (segment.Count == 0)
            return;

        var occurrences = new Dictionary<FieldValue, int>();
        foreach (PlanInstruction instruction in segment)
            CountFieldValues(instruction, occurrences);

        Dictionary<FieldValue, SlotValue>? replacements = null;
        foreach (KeyValuePair<FieldValue, int> occurrence in occurrences)
        {
            if (occurrence.Value < 2)
                continue;

            replacements ??= new Dictionary<FieldValue, SlotValue>();
            replacements[occurrence.Key] = new SlotValue(nextSlot++, occurrence.Key.TypeName);
            rewritten.Add(new AssignInstr(replacements[occurrence.Key].Slot, occurrence.Key));
        }

        if (replacements is null)
        {
            rewritten.AddRange(segment);
            segment.Clear();
            return;
        }

        foreach (PlanInstruction instruction in segment)
            rewritten.Add(RewriteInstruction(instruction, replacements));

        segment.Clear();
    }

    private static void CountFieldValues(PlanInstruction instruction, Dictionary<FieldValue, int> occurrences)
    {
        switch (instruction)
        {
            case UnifyInstr unify:
                CountFieldValues(unify.Left, occurrences);
                CountFieldValues(unify.Right, occurrences);
                break;

            case ConstraintInstr constraint:
                foreach (PlanValue argument in constraint.Arguments)
                    CountFieldValues(argument, occurrences);
                break;

            case AssignInstr assign:
                CountFieldValues(assign.Value, occurrences);
                break;

            case CompInstr comp:
                CountFieldValues(comp.Left, occurrences);
                CountFieldValues(comp.Right, occurrences);
                break;

            case NotInstr not:
                foreach (PlanInstruction subGoalInstruction in not.SubGoalInstructions)
                    CountFieldValues(subGoalInstruction, occurrences);
                break;
        }
    }

    private static void CountFieldValues(PlanValue value, Dictionary<FieldValue, int> occurrences)
    {
        switch (value)
        {
            case FieldValue field:
                occurrences.TryGetValue(field, out int count);
                occurrences[field] = count + 1;
                CountFieldValues(field.Target, occurrences);
                break;

            case ArithValue arith:
                CountFieldValues(arith.Left, occurrences);
                CountFieldValues(arith.Right, occurrences);
                break;

            case ListConsValue cons:
                CountFieldValues(cons.Head, occurrences);
                CountFieldValues(cons.Tail, occurrences);
                break;
        }
    }

    private static PlanInstruction RewriteInstruction(
        PlanInstruction instruction,
        IReadOnlyDictionary<FieldValue, SlotValue> replacements)
    {
        return instruction switch
        {
            UnifyInstr unify => unify with
            {
                Left = RewriteValue(unify.Left, replacements),
                Right = RewriteValue(unify.Right, replacements)
            },
            ConstraintInstr constraint => constraint with
            {
                Arguments = constraint.Arguments.Select(argument => RewriteValue(argument, replacements)).ToList()
            },
            AssignInstr assign => assign with
            {
                Value = RewriteValue(assign.Value, replacements)
            },
            CompInstr comp => comp with
            {
                Left = RewriteValue(comp.Left, replacements),
                Right = RewriteValue(comp.Right, replacements)
            },
            NotInstr not => not with
            {
                SubGoalInstructions = not.SubGoalInstructions
                    .Select(subGoalInstruction => RewriteInstruction(subGoalInstruction, replacements))
                    .ToList()
            },
            _ => instruction,
        };
    }

    private static PlanValue RewriteValue(PlanValue value, IReadOnlyDictionary<FieldValue, SlotValue> replacements)
    {
        if (value is FieldValue fieldValue && replacements.TryGetValue(fieldValue, out SlotValue? replacement))
            return replacement;

        return value switch
        {
            FieldValue nestedField => nestedField with { Target = RewriteValue(nestedField.Target, replacements) },
            ArithValue arith => arith with
            {
                Left = RewriteValue(arith.Left, replacements),
                Right = RewriteValue(arith.Right, replacements)
            },
            ListConsValue cons => cons with
            {
                Head = RewriteValue(cons.Head, replacements),
                Tail = RewriteValue(cons.Tail, replacements)
            },
            _ => value,
        };
    }
}

// ─── Pipeline ─────────────────────────────────────────────────────────────────

/// <summary>Runs the full ordered optimization pipeline over a <see cref="PlanProgram"/>.</summary>
public sealed class OptimizationPipeline
{
    private readonly IReadOnlyList<IPlanOptimization> _passes;

    public OptimizationPipeline() : this(DefaultPasses()) { }

    public OptimizationPipeline(IReadOnlyList<IPlanOptimization> passes) => _passes = passes;

    private static IReadOnlyList<IPlanOptimization> DefaultPasses() =>
    [
        new NormalizeSequence(),
        new RemoveRedundantUnify(),
        new DependencyAnalysis(),
        new PredicateCallInlining(),
        new NormalizeSequence(),
        new DependencyAnalysis(),
        new ReorderConjunction(),
        new IndexSelection(),
        new ConstraintHoisting(),
        new DeadBindingElimination(),
        new DeadCodeElimination(),
        new LoopSpecialization(),
        new TempHoisting(),
        new NormalizeSequence(),
    ];

    public PlanProgram Run(PlanProgram program, PlanOptimizationContext? context = null)
    {
        (PlanProgram result, _) = RunWithTrace(program, context);
        return result;
    }

    public (PlanProgram Program, PlanOptimizationTrace Trace) RunWithTrace(
        PlanProgram program,
        PlanOptimizationContext? context = null)
    {
        PlanOptimizationContext effectiveContext = context ?? new PlanOptimizationContext
        {
            Options = new OptimizationOptions()
        };

        var passTraces = ImmutableArray.CreateBuilder<PlanOptimizationPassTrace>();

        foreach (IPlanOptimization pass in _passes)
        {
            string inputHash = effectiveContext.Options.EmitOptimizationTrace ? ComputePlanHash(program) : string.Empty;
            PlanOptimizationResult result = pass.Optimize(program, effectiveContext);
            program = result.Program;
            string outputHash = effectiveContext.Options.EmitOptimizationTrace ? ComputePlanHash(program) : string.Empty;

            if (effectiveContext.Options.EmitOptimizationTrace)
            {
                passTraces.Add(new PlanOptimizationPassTrace(
                    pass.Name,
                    inputHash,
                    outputHash,
                    result.Changes));
            }
        }

        return (program, new PlanOptimizationTrace(passTraces.ToImmutable()));
    }

    /// <summary>Computes a deterministic hash of the normalized plan representation.</summary>
    internal static string ComputePlanHash(PlanProgram program)
    {
        var sb = new StringBuilder();
        RenderProgram(program, sb);
        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", string.Empty)
            .Substring(0, 16)
            .ToLowerInvariant();
    }

    private static void RenderProgram(PlanProgram program, StringBuilder sb)
    {
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(program))
        {
            sb.Append(block.Label).Append(':');
            foreach (PlanInstruction instr in block.Instructions)
                RenderInstruction(instr, sb);
            RenderTerminator(block.Terminator, sb);
        }
    }

    private static void RenderInstruction(PlanInstruction instr, StringBuilder sb)
    {
        sb.Append(instr.GetType().Name).Append('(');
        switch (instr)
        {
            case UnifyInstr u:
                RenderValue(u.Left, sb);
                sb.Append(',');
                RenderValue(u.Right, sb);
                break;
            case AssignInstr a:
                sb.Append(a.Slot).Append(',');
                RenderValue(a.Value, sb);
                break;
            case CompInstr c:
                sb.Append(c.Op).Append(',');
                RenderValue(c.Left, sb);
                sb.Append(',');
                RenderValue(c.Right, sb);
                break;
            case ConstraintInstr constraint:
                sb.Append(constraint.Method.Name).Append(':');
                foreach (PlanValue argument in constraint.Arguments)
                {
                    RenderValue(argument, sb);
                    sb.Append(';');
                }
                break;
            case LoopBindInstr l:
                sb.Append(l.Slot).Append(',').Append(l.IndexVar);
                break;
            case IndexInitInstr init:
                sb.Append(init.IndexVar);
                break;
            case IndexIncrInstr incr:
                sb.Append(incr.IndexVar);
                break;
            case CallInstr call:
                sb.Append(call.PredicateType.Name).Append('/').Append(call.Arity).Append(':');
                foreach (int slot in call.ArgumentSlots)
                    sb.Append(slot).Append(',');
                sb.Append(call.IsTabledCall);
                break;
            case NotInstr n:
                foreach (PlanInstruction s in n.SubGoalInstructions)
                    RenderInstruction(s, sb);
                break;
        }
        sb.Append(')');
    }

    private static void RenderTerminator(PlanTerminator terminator, StringBuilder sb)
    {
        sb.Append(terminator.GetType().Name).Append('(');
        switch (terminator)
        {
            case GotoTerm g:
                sb.Append(g.TargetLabel);
                break;
            case ChoiceTerm c:
                sb.Append(c.NextLabel).Append(',').Append(c.AlternativeLabel).Append(',').Append(c.TrailSlot);
                break;
            case LoopCheckTerm l:
                sb.Append(l.BodyLabel).Append(',').Append(l.FailLabel).Append(',').Append(l.IndexVar);
                break;
        }
        sb.Append(')');
    }

    private static void RenderValue(PlanValue value, StringBuilder sb)
    {
        switch (value)
        {
            case SlotValue s:
                sb.Append("Slot(").Append(s.Slot).Append(')');
                break;
            case ConstValue c:
                sb.Append("Const(").Append(c.Value?.ToString() ?? "null").Append(')');
                break;
            case FieldValue f:
                RenderValue(f.Target, sb);
                sb.Append('.').Append(f.MemberName);
                break;
            case ArithValue a:
                sb.Append("Arith(").Append(a.Op).Append(',');
                RenderValue(a.Left, sb);
                sb.Append(',');
                RenderValue(a.Right, sb);
                sb.Append(')');
                break;
            case ListEmptyValue:
                sb.Append("[]");
                break;
            case ListConsValue cons:
                sb.Append("Cons(");
                RenderValue(cons.Head, sb);
                sb.Append(',');
                RenderValue(cons.Tail, sb);
                sb.Append(')');
                break;
            default:
                sb.Append(value.GetType().Name);
                break;
        }
    }
}

// ─── PredicateCallInlining ────────────────────────────────────────────────────

/// <summary>
/// Inlines eligible non-recursive, non-tabled, single-block callee plans at their call sites.
/// Any call that does not satisfy all eligibility criteria is left as a <see cref="CallInstr"/>
/// and falls back to normal predicate-invocation state-machine execution.
/// </summary>
public sealed class PredicateCallInlining : IPlanOptimization
{
    /// <summary>Default maximum argument count above which inlining is skipped.</summary>
    public const int DefaultMaxArgumentCount = 8;

    private readonly IReadOnlyDictionary<string, PlanProgram> _calleePrograms;
    private readonly int _maxArgumentCount;

    public string Name => nameof(PredicateCallInlining);

    /// <summary>Initializes an instance with no registered callee plans (all calls fall back).</summary>
    public PredicateCallInlining()
        : this(new Dictionary<string, PlanProgram>(StringComparer.Ordinal)) { }

    /// <summary>
    /// Initializes an instance with a set of known callee plans.
    /// The dictionary key must be the fully qualified type name and arity in the form
    /// <c>global::Namespace.TypeName/arity</c>, matching the output of
    /// <see cref="GetCalleeKey"/>.
    /// </summary>
    public PredicateCallInlining(
        IReadOnlyDictionary<string, PlanProgram> calleePrograms,
        int maxArgumentCount = DefaultMaxArgumentCount)
    {
        _calleePrograms = calleePrograms;
        _maxArgumentCount = maxArgumentCount;
    }

    /// <summary>
    /// Returns the lookup key for a <see cref="CallInstr"/> that matches the keys
    /// expected by the callee-programs dictionary.
    /// </summary>
    public static string GetCalleeKey(INamedTypeSymbol predicateType, int arity) =>
        $"{predicateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}/{arity}";

    public PlanOptimizationResult Optimize(PlanProgram program, PlanOptimizationContext context)
    {
        if (!context.Options.EnablePredicateCallInlining)
            return new PlanOptimizationResult(program, ImmutableArray<PlanOptimizationChange>.Empty);

        int nextFreshSlot = PlanAnalysis.NextAnonymousSlot(program);
        var changes = ImmutableArray.CreateBuilder<PlanOptimizationChange>();
        PlanProgram optimized = PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, ref nextFreshSlot, context, changes));
        return new PlanOptimizationResult(optimized, changes.ToImmutable());
    }

    private PlanBlock TransformBlock(
        PlanBlock block,
        ref int nextFreshSlot,
        PlanOptimizationContext context,
        ImmutableArray<PlanOptimizationChange>.Builder changes)
    {
        bool changed = false;
        var instructions = new List<PlanInstruction>(block.Instructions.Count);

        foreach (PlanInstruction instruction in block.Instructions)
        {
            if (instruction is CallInstr call)
            {
                InlineDecision decision = EvaluateInlineDecision(call, context);
                if (decision.CanInline
                    && TryBuildInlinedInstructions(call, decision, ref nextFreshSlot, out IReadOnlyList<PlanInstruction>? inlined))
                {
                    instructions.AddRange(inlined!);
                    changes.Add(new PlanOptimizationChange(
                        Name,
                        PlanChangeKind.InlinedPredicateCall,
                        $"{call.PredicateType.Name}/{call.Arity}",
                        $"instruction-count={decision.EstimatedInstructionCount}"));
                    changed = true;
                    continue;
                }

                changes.Add(new PlanOptimizationChange(
                    Name,
                    PlanChangeKind.SkippedCandidate,
                    $"{call.PredicateType.Name}/{call.Arity}",
                    decision.RejectReason?.ToString() ?? nameof(InlineRejectReason.SlotMappingFailure)));
            }

            instructions.Add(instruction);
        }

        return changed ? block with { Instructions = instructions } : block;
    }

    private InlineDecision EvaluateInlineDecision(CallInstr call, PlanOptimizationContext context)
    {
        string predicateName = call.PredicateType.Name;

        if (context.Options.MaxInlineDepth < 1)
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.TooDeep, 0);

        if (call.IsTabledCall)
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.Tabled, 0);

        if (call.ArgumentSlots.Count > _maxArgumentCount)
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.TooLarge, call.ArgumentSlots.Count);

        string key = GetCalleeKey(call.PredicateType, call.Arity);
        if (!_calleePrograms.TryGetValue(key, out PlanProgram? callee))
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.UnknownPredicate, 0);

        if (callee.Metadata?.RecursiveCalls.Count > 0)
        {
            bool directlyRecursive = callee.Metadata.RecursiveCalls.Any(recursiveCall =>
                string.Equals(recursiveCall.CallingPredicateName, recursiveCall.TargetPredicateName, StringComparison.Ordinal));
            return new InlineDecision(
                false,
                predicateName,
                call.Arity,
                directlyRecursive ? InlineRejectReason.Recursive : InlineRejectReason.MutuallyRecursive,
                callee.Entry.Instructions.Count);
        }

        if (callee.Blocks.Count > 0)
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.MultipleBodies, callee.Entry.Instructions.Count);

        if (callee.Entry.Terminator is not SucceedTerm)
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.WouldChangeBacktracking, callee.Entry.Instructions.Count);

        if (callee.Entry.Instructions.Any(instruction => instruction is NotInstr))
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.NegationBoundary, callee.Entry.Instructions.Count);

        if (callee.Entry.Instructions.Any(IsNonInlinableInstruction))
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.UnsupportedInstruction, callee.Entry.Instructions.Count);

        if (callee.Entry.Instructions.Count > context.Options.MaxInlineInstructionCount)
            return new InlineDecision(false, predicateName, call.Arity, InlineRejectReason.TooLarge, callee.Entry.Instructions.Count);

        // A call site occupies one instruction before inlining, so growth is measured
        // relative to replacing that single CallInstr with the callee body.
        int growthPercent = callee.Entry.Instructions.Count <= 1
            ? 0
            : (callee.Entry.Instructions.Count - 1) * 100;
        if (growthPercent > context.Options.MaxGeneratedInstructionGrowthPercent)
        {
            return new InlineDecision(
                false,
                predicateName,
                call.Arity,
                InlineRejectReason.GrowthLimitExceeded,
                callee.Entry.Instructions.Count);
        }

        return new InlineDecision(true, predicateName, call.Arity, null, callee.Entry.Instructions.Count);
    }

    private bool TryBuildInlinedInstructions(
        CallInstr call,
        InlineDecision decision,
        ref int nextFreshSlot,
        out IReadOnlyList<PlanInstruction>? inlined)
    {
        inlined = null;

        string key = GetCalleeKey(call.PredicateType, call.Arity);
        if (!_calleePrograms.TryGetValue(key, out PlanProgram? callee))
            return false;

        if (call.Arity > call.ArgumentSlots.Count)
            return false;

        var slotMap = new Dictionary<int, int>();
        for (int i = 0; i < call.Arity && i < call.ArgumentSlots.Count; i++)
            slotMap[i] = call.ArgumentSlots[i];

        foreach (PlanInstruction instr in callee.Entry.Instructions)
        {
            foreach (int slot in CollectWriteSlots(instr))
            {
                if (!slotMap.ContainsKey(slot))
                    slotMap[slot] = nextFreshSlot++;
            }
        }

        inlined = callee.Entry.Instructions
            .Select(instr => RemapInstruction(instr, slotMap))
            .ToList();

        return true;
    }

    private static bool IsNonInlinableInstruction(PlanInstruction instruction) =>
        instruction is LoopBindInstr
            or IndexInitInstr
            or IndexIncrInstr
            or CallInstr
            or NotInstr;

    private static IEnumerable<int> CollectWriteSlots(PlanInstruction instruction)
    {
        return instruction switch
        {
            UnifyInstr unify => CollectDirectSlotIds(unify.Left).Concat(CollectDirectSlotIds(unify.Right)),
            AssignInstr assign => [assign.Slot],
            _ => []
        };
    }

    private static IEnumerable<int> CollectDirectSlotIds(PlanValue value)
    {
        if (value is SlotValue slot)
            yield return slot.Slot;
    }

    private static PlanInstruction RemapInstruction(
        PlanInstruction instruction,
        IReadOnlyDictionary<int, int> slotMap)
    {
        return instruction switch
        {
            UnifyInstr unify => unify with
            {
                Left = RemapValue(unify.Left, slotMap),
                Right = RemapValue(unify.Right, slotMap)
            },
            ConstraintInstr constraint => constraint with
            {
                Arguments = constraint.Arguments
                    .Select(arg => RemapValue(arg, slotMap))
                    .ToList()
            },
            AssignInstr assign => assign with
            {
                Slot = RemapSlot(assign.Slot, slotMap),
                Value = RemapValue(assign.Value, slotMap)
            },
            CompInstr comp => comp with
            {
                Left = RemapValue(comp.Left, slotMap),
                Right = RemapValue(comp.Right, slotMap)
            },
            _ => instruction
        };
    }

    private static PlanValue RemapValue(PlanValue value, IReadOnlyDictionary<int, int> slotMap)
    {
        return value switch
        {
            SlotValue slot => slot with { Slot = RemapSlot(slot.Slot, slotMap) },
            FieldValue field => field with { Target = RemapValue(field.Target, slotMap) },
            ArithValue arith => arith with
            {
                Left = RemapValue(arith.Left, slotMap),
                Right = RemapValue(arith.Right, slotMap)
            },
            ListConsValue cons => cons with
            {
                Head = RemapValue(cons.Head, slotMap),
                Tail = RemapValue(cons.Tail, slotMap)
            },
            _ => value
        };
    }

    private static int RemapSlot(int slot, IReadOnlyDictionary<int, int> slotMap) =>
        slotMap.TryGetValue(slot, out int mapped) ? mapped : slot;
}
