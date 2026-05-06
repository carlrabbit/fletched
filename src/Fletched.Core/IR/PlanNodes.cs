using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fletched.Core.IR;

// ─── PlanValue hierarchy ───────────────────────────────────────────────────

/// <summary>Base class for values in the execution plan.</summary>
public abstract record PlanValue;

/// <summary>References a typed slot (logical variable) by index.</summary>
public record SlotValue(int Slot, Type Type) : PlanValue;

/// <summary>A compile-time constant value.</summary>
public record ConstValue(object? Value, Type Type) : PlanValue;

/// <summary>Field access on another plan value.</summary>
public record FieldValue(PlanValue Target, MemberInfo Member, Type Type) : PlanValue;

// ─── PlanInstruction hierarchy ─────────────────────────────────────────────

/// <summary>Base class for instructions in an execution plan block.</summary>
public abstract record PlanInstruction;

/// <summary>Unification of two plan values.</summary>
public record UnifyInstr(PlanValue Left, PlanValue Right) : PlanInstruction;

/// <summary>Boolean constraint check — fail if method returns false.</summary>
public record ConstraintInstr(MethodInfo Method, IReadOnlyList<PlanValue> Arguments) : PlanInstruction;

/// <summary>Direct slot assignment (no unification — used for loop binding).</summary>
public record AssignInstr(int Slot, PlanValue Value) : PlanInstruction;

/// <summary>Call to another predicate.</summary>
public record CallInstr(Type PredicateType, IReadOnlyList<int> ArgumentSlots, int Arity) : PlanInstruction;

/// <summary>Built-in AllDistinct constraint: fails if any two bound slots hold equal values.</summary>
public record AllDistinctInstr(IReadOnlyList<int> Slots, Type ElementType) : PlanInstruction;

// ─── PlanTerminator hierarchy ──────────────────────────────────────────────

/// <summary>Base class for block terminators.</summary>
public abstract record PlanTerminator;

/// <summary>Unconditional jump to a labelled block.</summary>
public record GotoTerm(string TargetLabel) : PlanTerminator;

/// <summary>
/// Non-deterministic choice: push a choice point for <see cref="AlternativeLabel"/>
/// then fall through to <see cref="NextLabel"/>.
/// </summary>
public record ChoiceTerm(string NextLabel, string AlternativeLabel, int TrailSlot) : PlanTerminator;

/// <summary>Succeed — yield a result and backtrack.</summary>
public record SucceedTerm() : PlanTerminator;

/// <summary>Hard failure — backtrack immediately.</summary>
public record FailTerm() : PlanTerminator;

/// <summary>Loop iteration check: advance or fail when exhausted.</summary>
public record LoopCheckTerm(string BodyLabel, string FailLabel, int IndexSlot, int LengthSlot) : PlanTerminator;

// ─── Plan structures ───────────────────────────────────────────────────────

/// <summary>A single labelled block of instructions with exactly one terminator.</summary>
public record PlanBlock(string Label, IReadOnlyList<PlanInstruction> Instructions, PlanTerminator Terminator);

/// <summary>The full execution plan for a predicate.</summary>
public record PlanProgram(PlanBlock Entry, IReadOnlyList<PlanBlock> Blocks);

// ─── Data source structures ────────────────────────────────────────────────

/// <summary>Base class for data sources (loops over fact tables).</summary>
public abstract record PlanDataSource;

/// <summary>Full sequential scan over a fact table.</summary>
public record FullScanSource(Type FactType) : PlanDataSource;

/// <summary>Index-based lookup when a key field is already bound.</summary>
public record IndexedSource(Type FactType, MemberInfo KeyMember, int KeySlot) : PlanDataSource;
