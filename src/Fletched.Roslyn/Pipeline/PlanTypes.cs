using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

// ─── Plan IR types used by the generator (string-based type refs) ──────────

/// <summary>Base plan value using string type names (generator-internal).</summary>
public abstract record PlanValue;
public record SlotValue(int Slot, string TypeName) : PlanValue;
public record ConstValue(object? Value, string TypeName) : PlanValue;
public record FieldValue(PlanValue Target, string MemberName, string TypeName) : PlanValue;

/// <summary>Arithmetic expression value (+, - in DSL).</summary>
public record ArithValue(ArithOp Op, PlanValue Left, PlanValue Right) : PlanValue;

/// <summary>Empty logical list value.</summary>
public record ListEmptyValue(string ElementTypeName) : PlanValue;

/// <summary>Cons cell logical list value.</summary>
public record ListConsValue(PlanValue Head, PlanValue Tail, string ElementTypeName) : PlanValue;

/// <summary>Base plan instruction.</summary>
public abstract record PlanInstruction;
public record UnifyInstr(PlanValue Left, PlanValue Right) : PlanInstruction;
public record ConstraintInstr(IMethodSymbol Method, IReadOnlyList<PlanValue> Arguments) : PlanInstruction;
public record AssignInstr(int Slot, PlanValue Value) : PlanInstruction;

public readonly record struct SlotId(int Value)
{
    public override string ToString() => $"s{Value}";
}

public readonly record struct PlanInstructionId(string Value)
{
    public override string ToString() => Value;
}

public sealed record FactIndexCandidate(
    string FactType,
    string IndexName,
    FactIndexKindModel Kind,
    ImmutableArray<string> Members,
    ImmutableArray<SlotId> BoundInputs,
    ImmutableArray<PlanInstructionId> SatisfiedConstraints,
    int Score,
    string Reason);

public enum FactAccessPathKind
{
    FullScan,
    EqualityIndex,
    CompositeEqualityIndex,
    RangeIndex
}

public sealed record FactAccessPath(
    string FactType,
    FactAccessPathKind Kind,
    string? IndexName,
    ImmutableArray<string> Members,
    ImmutableArray<SlotId> BoundInputs,
    ImmutableArray<PlanInstructionId> ResidualConstraints,
    string Reason);

public sealed record EqualityLookupPart(string MemberName, PlanValue Key);

public sealed record RangeLookupSpec(
    string MemberName,
    PlanValue? Lower,
    bool LowerInclusive,
    PlanValue? Upper,
    bool UpperInclusive);

public sealed record SkippedFactIndexCandidate(
    FactIndexCandidate Candidate,
    string Reason);

/// <summary>Metadata for a loop that can use an advanced fact index.</summary>
public sealed record IndexedLookupSpec(
    string IndexName,
    string AccessorFieldName,
    bool IsImplicit,
    FactAccessPathKind AccessPathKind,
    ImmutableArray<string> Members,
    ImmutableArray<EqualityLookupPart> EqualityParts,
    RangeLookupSpec? Range,
    ImmutableArray<string> BoundInputNames,
    ImmutableArray<string> SatisfiedConstraintTexts,
    ImmutableArray<string> ResidualConstraintTexts,
    ImmutableArray<SkippedFactIndexCandidate> SkippedCandidates,
    string Reason)
{
    public string MemberName => Members.IsDefaultOrEmpty ? string.Empty : Members[0];

    public PlanValue Key => EqualityParts.IsDefaultOrEmpty
        ? throw new InvalidOperationException("Lookup does not contain an equality key.")
        : EqualityParts[0].Key;
}

public readonly record struct Adornment(string Pattern)
{
    public static Adornment FromBoundArguments(IEnumerable<bool> isBound) =>
        new(string.Concat(isBound.Select(bound => bound ? 'b' : 'f')));

    public bool HasBoundArguments => Pattern.IndexOf('b') >= 0;

    public bool IsAllFree => Pattern.All(marker => marker == 'f');

    public override string ToString() => Pattern;
}

/// <summary>Comparison instruction (!=, &lt;, &gt;, &lt;=, &gt;= in DSL).</summary>
public record CompInstr(CompOp Op, PlanValue Left, PlanValue Right) : PlanInstruction;

/// <summary>Loop-specific instructions (generator-internal).</summary>
public record IndexInitInstr(string IndexVar,
    /// <summary>
    /// The fact type being scanned. Provided to the emitter so it can include the
    /// type name in <c>IExecutionObserver.OnFactScan</c> observer callbacks.
    /// </summary>
    ITypeSymbol FactType,
    IndexedLookupSpec? IndexedLookup = null) : PlanInstruction;
public record LoopBindInstr(int Slot, string IndexVar, ITypeSymbol FactType, IndexedLookupSpec? IndexedLookup = null) : PlanInstruction;
public record IndexIncrInstr(string IndexVar) : PlanInstruction;

/// <summary>Call to another predicate — iterates its results and binds argument slots.</summary>
public record CallInstr(
    INamedTypeSymbol PredicateType,
    IReadOnlyList<int> ArgumentSlots,
    int Arity,
    bool IsTabledCall = false) : PlanInstruction;

/// <summary>Negation-as-failure instruction. Succeeds iff the subgoal produces no solutions.</summary>
public record NotInstr(IReadOnlyList<PlanInstruction> SubGoalInstructions) : PlanInstruction;

/// <summary>Base plan terminator.</summary>
public abstract record PlanTerminator;
public record GotoTerm(string TargetLabel) : PlanTerminator;
public record ChoiceTerm(string NextLabel, string AlternativeLabel, int TrailSlot) : PlanTerminator;
public record SucceedTerm() : PlanTerminator;
public record FailTerm() : PlanTerminator;
public record LoopCheckTerm(string BodyLabel, string FailLabel, string IndexVar, ITypeSymbol FactType, IndexedLookupSpec? IndexedLookup = null) : PlanTerminator;

/// <summary>A labelled block of instructions.</summary>
public record PlanBlock(string Label, IReadOnlyList<PlanInstruction> Instructions, PlanTerminator Terminator);

/// <summary>Full execution plan for a predicate.</summary>
public record PlanProgram(
    PlanBlock Entry,
    IReadOnlyList<PlanBlock> Blocks,
    IReadOnlyDictionary<VariableSymbol, int> SlotMap,
    RecursivePlanMetadata? Metadata = null);

public enum RecursiveAccessPathKind
{
    FullFactScan,
    IndexedFactLookup,
    MagicSourceLookup,
    TableLookup
}

public sealed record RecursiveAccessPathPlan(
    string Label,
    RecursiveAccessPathKind Kind,
    string TargetName);

public sealed record RecursiveCallPlan(
    string CallingPredicateName,
    string TargetPredicateName,
    Adornment Adornment,
    bool IsTabledCall,
    bool IsInsideNegation,
    string? BlockLabel);

public sealed record MagicPredicatePlan(
    string PredicateName,
    Adornment Adornment,
    IReadOnlyList<int> BoundArgumentIndices)
{
    public string MagicPredicateName => $"Magic_{PredicateName}_{Adornment.Pattern}";
}

public sealed record MagicSeedPlan(
    string CallingPredicateName,
    string TargetPredicateName,
    Adornment Adornment,
    IReadOnlyList<int> BoundArgumentIndices,
    string? BlockLabel);

public sealed record MagicModifiedRulePlan(
    string PredicateName,
    Adornment Adornment,
    string MagicPredicateName);

public sealed record MagicPropagationRulePlan(
    string CallingPredicateName,
    string TargetPredicateName,
    Adornment Adornment,
    IReadOnlyList<int> BoundArgumentIndices,
    string? BlockLabel);

public sealed record RecursivePlanMetadata(
    Adornment EntryAdornment,
    IReadOnlyList<RecursiveCallPlan> RecursiveCalls,
    IReadOnlyList<MagicPredicatePlan> MagicPredicates,
    IReadOnlyList<MagicSeedPlan> MagicSeeds,
    IReadOnlyList<MagicModifiedRulePlan> ModifiedRules,
    IReadOnlyList<MagicPropagationRulePlan> PropagationRules,
    IReadOnlyList<RecursiveAccessPathPlan> AccessPaths);

// ─── Optimization contract types ────────────────────────────────────────────

public enum PlanChangeKind
{
    RemovedInstruction,
    ReorderedConjunction,
    SelectedIndex,
    HoistedConstraint,
    InlinedPredicateCall,
    SkippedCandidate,
    SimplifiedUnification,
    RemovedUnreachableBlock,
    RemovedDeadBinding,
    SpecializedLoop
}

public sealed record PlanOptimizationChange(
    string Pass,
    PlanChangeKind Kind,
    string Target,
    string Reason);

public sealed record PlanOptimizationResult(
    PlanProgram Program,
    ImmutableArray<PlanOptimizationChange> Changes);

public sealed record PlanOptimizationPassTrace(
    string PassName,
    string InputHash,
    string OutputHash,
    ImmutableArray<PlanOptimizationChange> Changes);

public sealed record PlanOptimizationTrace(
    ImmutableArray<PlanOptimizationPassTrace> Passes);

public sealed record OptimizationOptions
{
    public bool EnablePredicateCallInlining { get; init; } = true;
    public bool EnableDeadBindingElimination { get; init; } = true;
    public bool EnableLoopSpecialization { get; init; } = true;

    public int MaxInlineInstructionCount { get; init; } = 32;
    public int MaxInlineDepth { get; init; } = 2;
    public int MaxGeneratedInstructionGrowthPercent { get; init; } = 150;

    public bool EmitOptimizationTrace { get; init; } = false;
}

public sealed class PlanOptimizationContext
{
    public OptimizationOptions Options { get; init; } = new();
}

public enum InlineRejectReason
{
    Recursive,
    MutuallyRecursive,
    Tabled,
    TooLarge,
    TooDeep,
    GrowthLimitExceeded,
    MultipleBodies,
    UnsupportedInstruction,
    NegationBoundary,
    UnknownPredicate,
    WouldChangeProjection,
    WouldChangeBacktracking,
    SlotMappingFailure
}

public sealed record InlineDecision(
    bool CanInline,
    string PredicateName,
    int Arity,
    InlineRejectReason? RejectReason,
    int EstimatedInstructionCount);

public sealed record InstructionEffects(
    ImmutableHashSet<int> Reads,
    ImmutableHashSet<int> Writes,
    bool MayFail,
    bool MayProduceMultipleResults,
    bool IsNegationBoundary,
    bool IsTableBoundary,
    bool RequiresGroundInputs);
