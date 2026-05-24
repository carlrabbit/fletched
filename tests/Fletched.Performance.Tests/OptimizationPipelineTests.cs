using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fletched.Core;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class OptimizationPipelineTests
{
    [Test]
    public async Task RemoveRedundantUnify_ConstantMismatch_RewritesBlockToFail()
    {
        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new UnifyInstr(new ConstValue(1, "int"), new ConstValue(2, "int")),
                    new AssignInstr(0, new ConstValue(3, "int"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new RemoveRedundantUnify().Apply(program);

        await Assert.That(optimized.Entry.Instructions.Count).IsEqualTo(0);
        await Assert.That(optimized.Entry.Terminator).IsTypeOf<FailTerm>();
    }

    [Test]
    public async Task ReorderConjunction_IndependentComparison_MovesAheadOfUnify()
    {
        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new UnifyInstr(new SlotValue(0, "int"), new ConstValue(1, "int")),
                    new CompInstr(CompOp.NotEqual, new SlotValue(1, "int"), new ConstValue(0, "int"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new ReorderConjunction().Apply(program);

        await Assert.That(optimized.Entry.Instructions[0]).IsTypeOf<CompInstr>();
        await Assert.That(optimized.Entry.Instructions[1]).IsTypeOf<UnifyInstr>();
    }

    [Test]
    public async Task ConstraintHoisting_DependentComparison_DoesNotCrossProducer()
    {
        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new UnifyInstr(new SlotValue(0, "int"), new ConstValue(1, "int")),
                    new CompInstr(CompOp.NotEqual, new SlotValue(0, "int"), new ConstValue(0, "int"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new ConstraintHoisting().Apply(program);

        await Assert.That(optimized.Entry.Instructions[0]).IsTypeOf<UnifyInstr>();
        await Assert.That(optimized.Entry.Instructions[1]).IsTypeOf<CompInstr>();
    }

    [Test]
    public async Task TempHoisting_RepeatedFieldRead_IntroducesTemporarySlot()
    {
        var userSlot = new SlotValue(0, "global::Example.User");
        var nameField = new FieldValue(userSlot, "Name", "string");

        PlanProgram program = CreateProgram(
            new PlanBlock(
                "entry",
                [
                    new CompInstr(CompOp.NotEqual, nameField, new ConstValue("root", "string")),
                    new UnifyInstr(nameField, new ConstValue("admin", "string"))
                ],
                new SucceedTerm()));

        PlanProgram optimized = new TempHoisting().Apply(program);

        await Assert.That(optimized.Entry.Instructions.Count).IsEqualTo(3);
        await Assert.That(optimized.Entry.Instructions[0]).IsTypeOf<AssignInstr>();

        var assign = (AssignInstr)optimized.Entry.Instructions[0];
        var comparison = (CompInstr)optimized.Entry.Instructions[1];
        var unify = (UnifyInstr)optimized.Entry.Instructions[2];

        await Assert.That(assign.Value).IsEqualTo(nameField);
        await Assert.That(comparison.Left).IsEqualTo(new SlotValue(assign.Slot, "string"));
        await Assert.That(unify.Left).IsEqualTo(new SlotValue(assign.Slot, "string"));
    }

    // ── PredicateCallInlining tests ──────────────────────────────────────────

    [Test]
    public async Task PredicateCallInlining_EligibleSingleBlockCallee_ReplacesCallWithRemappedInstructions()
    {
        // Callee plan: Unify(Slot(0), Const("admin")), SucceedTerm  (one parameter, slot 0)
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct SimpleCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "admin";
            }
            """, "SimpleCallee");

        PlanProgram calleePlan = CreateProgram(
            new PlanBlock(
                "callee_entry",
                [new UnifyInstr(new SlotValue(0, "string"), new ConstValue("admin", "string"))],
                new SucceedTerm()));

        // Caller plan: CallInstr with argSlots=[5]  (slot 5 in caller maps to callee param slot 0)
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock(
                "entry",
                [new CallInstr(calleeSymbol, [5], 1, IsTabledCall: false)],
                new SucceedTerm()));

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        // CallInstr replaced with callee's remapped UnifyInstr
        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<UnifyInstr>();

        var unify = (UnifyInstr)result.Entry.Instructions[0];
        // Callee slot 0 → caller argSlots[0] = 5
        await Assert.That(unify.Left).IsEqualTo(new SlotValue(5, "string"));
        await Assert.That(unify.Right).IsEqualTo(new ConstValue("admin", "string"));
    }

    [Test]
    public async Task PredicateCallInlining_EligibleCallee_RemapsAdditionalSlotsToFreshSlots()
    {
        // Callee plan: slot 0 = parameter, slot 1 = additional (assigned from Const)
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct AssignCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "AssignCallee");

        PlanProgram calleePlan = CreateProgram(
            new PlanBlock(
                "callee_entry",
                [
                    new AssignInstr(1, new ConstValue("temp", "string")),
                    new UnifyInstr(new SlotValue(0, "string"), new SlotValue(1, "string"))
                ],
                new SucceedTerm()));

        // Caller uses slot 3 as the argument; slots 0..2 are already taken
        PlanProgram callerPlan = new PlanProgram(
            new PlanBlock(
                "entry",
                [new CallInstr(calleeSymbol, [3], 1, IsTabledCall: false)],
                new SucceedTerm()),
            [],
            new Dictionary<VariableSymbol, int>());

        // Manually set the slot map so NextAnonymousSlot returns 4 (one past the max in use)
        callerPlan = callerPlan with
        {
            SlotMap = new Dictionary<VariableSymbol, int>
            {
                [new VariableSymbol("x", null!, VariableKind.Terminal)] = 3
            }
        };

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(2);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<AssignInstr>();
        await Assert.That(result.Entry.Instructions[1]).IsTypeOf<UnifyInstr>();

        // The assign must write to a fresh slot (≥ 4)
        var assign = (AssignInstr)result.Entry.Instructions[0];
        await Assert.That(assign.Slot).IsGreaterThanOrEqualTo(4);

        // The unify must reference slot 3 (caller arg) and the fresh assign slot
        var unify = (UnifyInstr)result.Entry.Instructions[1];
        await Assert.That(unify.Left).IsEqualTo(new SlotValue(3, "string"));
        await Assert.That(unify.Right).IsEqualTo(new SlotValue(assign.Slot, "string"));
    }

    [Test]
    public async Task PredicateCallInlining_TabledCall_LeavesCallInstrUnchanged()
    {
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct TabledCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "TabledCallee");

        PlanProgram calleePlan = CreateProgram(
            new PlanBlock("callee_entry", [], new SucceedTerm()));

        // Mark the call as tabled
        var tabledCall = new CallInstr(calleeSymbol, [0], 1, IsTabledCall: true);
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock("entry", [tabledCall], new SucceedTerm()));

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        // CallInstr must remain (tabled calls are never inlined)
        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<CallInstr>();
    }

    [Test]
    public async Task PredicateCallInlining_RecursiveCallee_LeavesCallInstrUnchanged()
    {
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct RecursiveCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "RecursiveCallee");

        // Callee plan with recursive metadata (simulates a recursive predicate)
        var recursiveCall = new RecursiveCallPlan(
            "RecursiveCallee", "RecursiveCallee",
            Adornment.FromBoundArguments([false]),
            IsTabledCall: false, IsInsideNegation: false, BlockLabel: null);

        var metadata = new RecursivePlanMetadata(
            EntryAdornment: Adornment.FromBoundArguments([false]),
            RecursiveCalls: [recursiveCall],
            MagicPredicates: [],
            MagicSeeds: [],
            ModifiedRules: [],
            PropagationRules: [],
            AccessPaths: []);

        PlanProgram calleePlan = new PlanProgram(
            new PlanBlock("callee_entry", [], new SucceedTerm()),
            [],
            new Dictionary<VariableSymbol, int>(),
            metadata);

        var call = new CallInstr(calleeSymbol, [0], 1, IsTabledCall: false);
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock("entry", [call], new SucceedTerm()));

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        // Recursive callee must not be inlined
        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<CallInstr>();
    }

    [Test]
    public async Task PredicateCallInlining_MultiBlockCallee_LeavesCallInstrUnchanged()
    {
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct MultiBlockCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "MultiBlockCallee");

        // Callee plan with multiple blocks (simulates disjunction)
        PlanProgram calleePlan = new PlanProgram(
            new PlanBlock("callee_entry", [], new GotoTerm("callee_alt")),
            [new PlanBlock("callee_alt", [], new SucceedTerm())],
            new Dictionary<VariableSymbol, int>());

        var call = new CallInstr(calleeSymbol, [0], 1, IsTabledCall: false);
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock("entry", [call], new SucceedTerm()));

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        // Multi-block callee must not be inlined
        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<CallInstr>();
    }

    [Test]
    public async Task PredicateCallInlining_CalleeWithLoopInstruction_LeavesCallInstrUnchanged()
    {
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct LoopCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "LoopCallee");

        ITypeSymbol stringType = GetSpecialType(SpecialType.System_String);

        // Callee contains a loop instruction (non-deterministic — multiple results)
        PlanProgram calleePlan = CreateProgram(
            new PlanBlock(
                "callee_entry",
                [new LoopBindInstr(0, "idx", stringType, null)],
                new SucceedTerm()));

        var call = new CallInstr(calleeSymbol, [0], 1, IsTabledCall: false);
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock("entry", [call], new SucceedTerm()));

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        // Loop-containing callee must not be inlined
        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<CallInstr>();
    }

    [Test]
    public async Task PredicateCallInlining_NoCalleePlansRegistered_LeavesAllCallsUnchanged()
    {
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct UnregisteredCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "UnregisteredCallee");

        var call = new CallInstr(calleeSymbol, [0], 1, IsTabledCall: false);
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock("entry", [call], new SucceedTerm()));

        // Default constructor: no callee plans registered — all fall back
        var pass = new PredicateCallInlining();

        PlanProgram result = pass.Apply(callerPlan);

        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<CallInstr>();
    }

    [Test]
    public async Task PredicateCallInlining_CallInsideNegation_NotInstrSubgoalIsUnchanged()
    {
        INamedTypeSymbol calleeSymbol = GetPredicateSymbol("""
            using Fletched.Core;
            [Predicate]
            public partial record struct NegCallee
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "x";
            }
            """, "NegCallee");

        PlanProgram calleePlan = CreateProgram(
            new PlanBlock(
                "callee_entry",
                [new UnifyInstr(new SlotValue(0, "string"), new ConstValue("x", "string"))],
                new SucceedTerm()));

        // CallInstr inside a NotInstr subgoal
        var callInNotInstr = new CallInstr(calleeSymbol, [0], 1, IsTabledCall: false);
        var notInstr = new NotInstr([callInNotInstr]);
        PlanProgram callerPlan = CreateProgram(
            new PlanBlock("entry", [notInstr], new SucceedTerm()));

        string key = PredicateCallInlining.GetCalleeKey(calleeSymbol, 1);
        var pass = new PredicateCallInlining(
            new Dictionary<string, PlanProgram>(StringComparer.Ordinal) { [key] = calleePlan });

        PlanProgram result = pass.Apply(callerPlan);

        // NotInstr must be present and its sub-goal must contain the original CallInstr unchanged
        await Assert.That(result.Entry.Instructions.Count).IsEqualTo(1);
        await Assert.That(result.Entry.Instructions[0]).IsTypeOf<NotInstr>();
        var resultNot = (NotInstr)result.Entry.Instructions[0];
        await Assert.That(resultNot.SubGoalInstructions.Count).IsEqualTo(1);
        await Assert.That(resultNot.SubGoalInstructions[0]).IsTypeOf<CallInstr>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PlanProgram CreateProgram(params PlanBlock[] blocks)
    {
        return new PlanProgram(
            blocks[0],
            blocks.Skip(1).ToList(),
            new Dictionary<VariableSymbol, int>());
    }

    private static INamedTypeSymbol GetPredicateSymbol(string source, string typeName)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        return compilation.GetTypeByMetadataName(typeName)
            ?? compilation.GlobalNamespace
                .GetTypeMembers()
                .FirstOrDefault(t => t.Name == typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found.");
    }

    private static ITypeSymbol GetSpecialType(SpecialType specialType)
    {
        CSharpCompilation compilation = CreateCompilation("class C {}");
        return compilation.GetSpecialType(specialType);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source, new CSharpParseOptions(LanguageVersion.Preview));

        List<MetadataReference> references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => (MetadataReference)MetadataReference.CreateFromFile(assembly.Location))
            .ToList();

        return CSharpCompilation.Create(
            "OptimizationPipelineTestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }
}
