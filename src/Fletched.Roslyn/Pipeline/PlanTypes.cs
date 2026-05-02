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

/// <summary>Base plan instruction.</summary>
public abstract record PlanInstruction;
public record UnifyInstr(PlanValue Left, PlanValue Right) : PlanInstruction;
public record ConstraintInstr(IMethodSymbol Method, IReadOnlyList<PlanValue> Arguments) : PlanInstruction;
public record AssignInstr(int Slot, PlanValue Value) : PlanInstruction;

/// <summary>Loop-specific instructions (generator-internal).</summary>
public record IndexInitInstr(string IndexVar) : PlanInstruction;
public record LoopBindInstr(int Slot, string IndexVar, ITypeSymbol FactType) : PlanInstruction;
public record IndexIncrInstr(string IndexVar) : PlanInstruction;

/// <summary>Base plan terminator.</summary>
public abstract record PlanTerminator;
public record GotoTerm(string TargetLabel) : PlanTerminator;
public record ChoiceTerm(string NextLabel, string AlternativeLabel, int TrailSlot) : PlanTerminator;
public record SucceedTerm() : PlanTerminator;
public record FailTerm() : PlanTerminator;
public record LoopCheckTerm(string BodyLabel, string FailLabel, string IndexVar, ITypeSymbol FactType) : PlanTerminator;

/// <summary>A labelled block of instructions.</summary>
public record PlanBlock(string Label, IReadOnlyList<PlanInstruction> Instructions, PlanTerminator Terminator);

/// <summary>Full execution plan for a predicate.</summary>
public record PlanProgram(
    PlanBlock Entry,
    IReadOnlyList<PlanBlock> Blocks,
    IReadOnlyDictionary<VariableSymbol, int> SlotMap);
