using System;
using System.Collections.Generic;
using System.Linq;

namespace Fletched.Roslyn.Pipeline;

/// <summary>Marker interface for optimization passes over a <see cref="PlanProgram"/>.</summary>
public interface IPlanOptimization
{
    PlanProgram Apply(PlanProgram program);
}

internal readonly record struct AccessSet(
    int[] Reads,
    int[] Writes);

internal static class PlanAnalysis
{
    public const string FailLabel = "Fail";

    public static IReadOnlyList<PlanBlock> AllBlocks(PlanProgram program) =>
        new[] { program.Entry }.Concat(program.Blocks).ToList();

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
            {
                indegree[successor]--;
            }
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
    public PlanProgram Apply(PlanProgram program)
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

        return result.Count == 0
            ? program
            : new PlanProgram(result[0], result.Skip(1).ToList(), program.SlotMap, program.Metadata);
    }
}

public sealed class RemoveRedundantUnify : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) =>
        PlanAnalysis.RewriteBlocks(program, TransformBlock);

    private static PlanBlock TransformBlock(PlanBlock block)
    {
        var instructions = new List<PlanInstruction>(block.Instructions.Count);
        PlanTerminator terminator = block.Terminator;

        foreach (PlanInstruction instruction in block.Instructions)
        {
            if (!PlanAnalysis.TryEvaluateInstruction(instruction, out bool alwaysSucceeds, out bool alwaysFails))
            {
                instructions.Add(instruction);
                continue;
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
    public PlanProgram Apply(PlanProgram program)
    {
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(program))
            _ = PlanAnalysis.AnalyzeBlock(block);

        return program;
    }
}

/// <summary>Reorders conjunction instructions: constraints first, then bound unifications, loops last.</summary>
public sealed class ReorderConjunction : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program) =>
        PlanAnalysis.RewriteBlocks(program, TransformBlock);

    private static PlanBlock TransformBlock(PlanBlock block)
    {
        if (block.Instructions.Any(IsBarrierInstruction))
            return block;

        IReadOnlyList<PlanInstruction> reordered = PlanAnalysis.ReorderInstructions(block.Instructions, GetPriority);
        return ReferenceEquals(reordered, block.Instructions)
            ? block
            : block with { Instructions = reordered };
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
    public PlanProgram Apply(PlanProgram program) =>
        PlanAnalysis.RewriteBlocks(program, TransformBlock);

    private static PlanBlock TransformBlock(PlanBlock block)
    {
        if (block.Instructions.Any(instruction => instruction is CallInstr or NotInstr))
            return block;

        IReadOnlyList<PlanInstruction> reordered = PlanAnalysis.ReorderInstructions(block.Instructions, GetPriority);
        return ReferenceEquals(reordered, block.Instructions)
            ? block
            : block with { Instructions = reordered };
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
    public PlanProgram Apply(PlanProgram program) =>
        PlanAnalysis.RewriteBlocks(program, TransformBlock);

    private static PlanBlock TransformBlock(PlanBlock block)
    {
        if (block.Instructions.Count < 2)
            return block;

        List<PlanInstruction> reordered = block.Instructions.ToList();
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
        }

        return block with { Instructions = reordered };
    }
}

/// <summary>Removes unreachable blocks and dead instructions after a provable failure.</summary>
public sealed class DeadCodeElimination : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program)
    {
        PlanProgram simplified = PlanAnalysis.RewriteBlocks(program, TrimDeadInstructions);

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        CollectReachable(simplified.Entry.Label, simplified, reachable);

        List<PlanBlock> live = PlanAnalysis.AllBlocks(simplified)
            .Where(block => reachable.Contains(block.Label))
            .ToList();

        return live.Count == 0
            ? simplified
            : new PlanProgram(live[0], live.Skip(1).ToList(), simplified.SlotMap, simplified.Metadata);
    }

    private static PlanBlock TrimDeadInstructions(PlanBlock block)
    {
        var instructions = new List<PlanInstruction>(block.Instructions.Count);
        PlanTerminator terminator = block.Terminator;

        foreach (PlanInstruction instruction in block.Instructions)
        {
            if (PlanAnalysis.TryEvaluateInstruction(instruction, out _, out bool alwaysFails) && alwaysFails)
            {
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

        PlanBlock? block = PlanAnalysis.AllBlocks(program).FirstOrDefault(candidate => candidate.Label == label);
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
    public PlanProgram Apply(PlanProgram program)
    {
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(program))
        {
            _ = block.Instructions
                .OfType<LoopBindInstr>()
                .Select(bind => bind.Slot)
                .ToList();
        }

        return program;
    }
}

/// <summary>Deduplicates repeated field reads in read-only instruction segments using temporaries.</summary>
public sealed class TempHoisting : IPlanOptimization
{
    public PlanProgram Apply(PlanProgram program)
    {
        int nextSlot = PlanAnalysis.NextAnonymousSlot(program);
        return PlanAnalysis.RewriteBlocks(program, block => TransformBlock(block, ref nextSlot));
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
        new ReorderConjunction(),
        new IndexSelection(),
        new ConstraintHoisting(),
        new DeadCodeElimination(),
        new LoopSpecialization(),
        new TempHoisting(),
    ];

    public PlanProgram Run(PlanProgram program)
    {
        foreach (IPlanOptimization pass in _passes)
            program = pass.Apply(program);

        return program;
    }
}
