using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

[Fact]
public partial record struct RecursiveSeed(int Value);

[Fact]
public partial record struct RecursiveStep(int Value, int Next);

[Predicate]
public partial record struct RecursiveEven
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<RecursiveSeed>(seed => seed.Value == 0 && seed.Value == value) ||
        Logic.With<RecursiveStep>(step => step.Value == value && RecursiveOdd(step.Next));
}

[Predicate]
public partial record struct RecursiveOdd
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<RecursiveStep>(step => step.Value == value && RecursiveEven(step.Next));
}

[Predicate]
public partial record struct RecursiveEvenValues
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<RecursiveSeed>(seed => seed.Value == value && RecursiveEven(value));
}

[Predicate]
public partial record struct RecursiveLoop
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        RecursiveLoopStep(value);
}

[Predicate]
public partial record struct RecursiveLoopStep
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        RecursiveLoop(value);
}

[Predicate]
public partial record struct RecursiveValues
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<RecursiveSeed>(seed => seed.Value == value);
}

[Predicate]
public partial record struct RecursiveLoopOrValue
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        RecursiveLoop(value) || RecursiveValues(value);
}

[Predicate]
public partial record struct RecursiveNotLoop
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        RecursiveValues(value) &&
        Logic.Not(RecursiveLoop(value));
}

public class RecursiveSafetyTests
{
    private static EngineContext BuildSeedContext()
    {
        var ctx = new EngineContext();
        ctx.RecursiveSeeds = new FactTable<RecursiveSeed>(new[]
        {
            new RecursiveSeed(0),
            new RecursiveSeed(1),
            new RecursiveSeed(2),
            new RecursiveSeed(3),
            new RecursiveSeed(4),
        });
        ctx.RecursiveSteps = new FactTable<RecursiveStep>(new[]
        {
            new RecursiveStep(1, 0),
            new RecursiveStep(2, 1),
            new RecursiveStep(3, 2),
            new RecursiveStep(4, 3),
        });
        return ctx;
    }

    [Test]
    public async Task DepthGuard_Disabled_AllowsProductiveRecursion()
    {
        EngineContext ctx = BuildSeedContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, null);

        List<int> values = default(RecursiveEvenValues).Execute(ctx)
            .Select(r => r.value)
            .ToList();

        await Assert.That(values.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task DepthGuard_FiniteLimit_AllowsShallowRecursion()
    {
        EngineContext ctx = BuildSeedContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, 8);

        bool hasAnyResult = default(RecursiveEvenValues).Execute(ctx).Any();

        await Assert.That(hasAnyResult).IsTrue();
    }

    [Test]
    public async Task DepthGuard_FiniteLimit_RejectsExcessiveRecursion()
    {
        EngineContext ctx = BuildSeedContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, 4);

        RecursiveDepthExceededException? thrown = null;
        try
        {
            _ = default(RecursiveLoop).Execute(ctx).ToList();
        }
        catch (RecursiveDepthExceededException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(thrown!.DiagnosticId).IsEqualTo(RecursiveDepthExceededException.RecursiveDepthExceededDiagnosticId);
        await Assert.That(thrown.MaxDepth).IsEqualTo(4);
    }

    [Test]
    public async Task GuardViolation_DoesNotBecomeLogicalFailure()
    {
        EngineContext ctx = BuildSeedContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, 3);

        await Assert.That(() =>
            default(RecursiveLoopOrValue).Execute(ctx).ToList())
            .Throws<RecursiveDepthExceededException>();
    }

    [Test]
    public async Task GuardViolationInsideNot_DoesNotBecomeSuccess()
    {
        EngineContext ctx = BuildSeedContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, 3);

        RecursiveDepthExceededException? thrown = null;
        try
        {
            _ = default(RecursiveNotLoop).Execute(ctx).ToList();
        }
        catch (RecursiveDepthExceededException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(thrown!.DiagnosticId).IsEqualTo(RecursiveDepthExceededException.RecursiveGuardInsideNegationDiagnosticId);
        await Assert.That(thrown.IsInsideNegation).IsTrue();
    }

    [Test]
    public async Task GuardViolation_PreservesCallerVisibleStateConsistency()
    {
        EngineContext ctx = BuildSeedContext();
        RecursionGuard.SetMaxRecursionDepth(ctx, 3);

        try
        {
            _ = default(RecursiveLoop).Execute(ctx).ToList();
        }
        catch (RecursiveDepthExceededException)
        {
        }

        List<int> values = default(RecursiveValues).Execute(ctx)
            .Select(r => r.value)
            .ToList();

        await Assert.That(RecursionGuard.GetCurrentDepth(ctx)).IsEqualTo(0);
        await Assert.That(values.Count).IsEqualTo(5);
        await Assert.That(values[0]).IsEqualTo(0);
        await Assert.That(values[1]).IsEqualTo(1);
        await Assert.That(values[2]).IsEqualTo(2);
        await Assert.That(values[3]).IsEqualTo(3);
        await Assert.That(values[4]).IsEqualTo(4);
    }
}
