using System;
using System.Collections.Generic;
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

/// <summary>Metadata for a loop that can use an index on a fact member.</summary>
public sealed record IndexedLookupSpec(string MemberName, PlanValue Key);

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
public record CallInstr(INamedTypeSymbol PredicateType, IReadOnlyList<int> ArgumentSlots) : PlanInstruction;

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
    IReadOnlyDictionary<VariableSymbol, int> SlotMap);
