using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

/// <summary>A named number sequence stored as a logical list.</summary>
[Fact]
public partial record struct NumberSequence(string Name, LogicList<int> Numbers);

// ── Predicates ────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the name of every sequence whose list is exactly empty.
/// </summary>
[Predicate]
public partial record struct EmptySequence
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<NumberSequence>(ns =>
            ns.Name == name &&
            ns.Numbers == Logic.List<int>()
        );
}

/// <summary>
/// Returns the name of every sequence whose list is exactly <c>[1]</c>.
/// </summary>
[Predicate]
public partial record struct SingletonOneSequence
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<NumberSequence>(ns =>
            ns.Name == name &&
            ns.Numbers == Logic.List(1)
        );
}

/// <summary>
/// Returns the name of every sequence whose list is exactly <c>[1, 2]</c>.
/// </summary>
[Predicate]
public partial record struct PairSequence
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<NumberSequence>(ns =>
            ns.Name == name &&
            ns.Numbers == Logic.List(1, 2)
        );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class ListTests
{
    // Data set:
    //   ("empty-seq",  [])
    //   ("one-seq",    [1])
    //   ("two-seq",    [1, 2])
    //   ("three-seq",  [1, 2, 3])
    //   ("other-seq",  [5])
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.NumberSequences = new FactTable<NumberSequence>(new[]
        {
            new NumberSequence("empty-seq",  LogicList<int>.Create()),
            new NumberSequence("one-seq",    LogicList<int>.Create(1)),
            new NumberSequence("two-seq",    LogicList<int>.Create(1, 2)),
            new NumberSequence("three-seq",  LogicList<int>.Create(1, 2, 3)),
            new NumberSequence("other-seq",  LogicList<int>.Create(5)),
        });
        return ctx;
    }

    // ── EmptySequence ─────────────────────────────────────────────────────────

    [Test]
    public async Task EmptySequence_ReturnsOnlyEmptyList()
    {
        EngineContext ctx = BuildContext();
        List<EmptySequenceResult> results = await default(EmptySequence).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("empty-seq");
    }

    [Test]
    public async Task EmptySequence_EmptyTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.NumberSequences = new FactTable<NumberSequence>(System.Array.Empty<NumberSequence>());

        List<EmptySequenceResult> results = await default(EmptySequence).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── SingletonOneSequence ──────────────────────────────────────────────────

    [Test]
    public async Task SingletonOneSequence_ReturnsOnlySingletonOne()
    {
        EngineContext ctx = BuildContext();
        List<SingletonOneSequenceResult> results = await default(SingletonOneSequence).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("one-seq");
    }

    [Test]
    public async Task SingletonOneSequence_DoesNotReturnEmpty()
    {
        EngineContext ctx = BuildContext();
        bool hasEmpty = (await default(SingletonOneSequence).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.name == "empty-seq");

        await Assert.That(hasEmpty).IsFalse();
    }

    [Test]
    public async Task SingletonOneSequence_DoesNotReturnLongerList()
    {
        EngineContext ctx = BuildContext();
        bool hasTwoSeq = (await default(SingletonOneSequence).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.name == "two-seq");

        await Assert.That(hasTwoSeq).IsFalse();
    }

    [Test]
    public async Task SingletonOneSequence_DoesNotReturnDifferentElement()
    {
        EngineContext ctx = BuildContext();
        // "other-seq" is [5], not [1] — should not match
        bool hasOtherSeq = (await default(SingletonOneSequence).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.name == "other-seq");

        await Assert.That(hasOtherSeq).IsFalse();
    }

    // ── PairSequence ──────────────────────────────────────────────────────────

    [Test]
    public async Task PairSequence_ReturnsOnlyTwoSeq()
    {
        EngineContext ctx = BuildContext();
        List<PairSequenceResult> results = await default(PairSequence).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("two-seq");
    }

    [Test]
    public async Task PairSequence_DoesNotReturnThreeSeq()
    {
        EngineContext ctx = BuildContext();
        bool hasThreeSeq = (await default(PairSequence).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.name == "three-seq");

        await Assert.That(hasThreeSeq).IsFalse();
    }
}

// ── Sync (IEnumerable<T>) API coverage ───────────────────────────────────────

public class ListTests_Execute
{
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.NumberSequences = new FactTable<NumberSequence>(new[]
        {
            new NumberSequence("empty-seq",  new LogicListEmpty<int>()),
            new NumberSequence("one-seq",    new LogicListCons<int>(1, new LogicListEmpty<int>())),
            new NumberSequence("two-seq",    new LogicListCons<int>(1, new LogicListCons<int>(2, new LogicListEmpty<int>()))),
            new NumberSequence("three-seq",  new LogicListCons<int>(1, new LogicListCons<int>(2, new LogicListCons<int>(3, new LogicListEmpty<int>())))),
            new NumberSequence("other-seq",  new LogicListCons<int>(5, new LogicListEmpty<int>())),
        });
        return ctx;
    }

    [Test]
    public async Task EmptySequence_ReturnsOnlyEmptyList()
    {
        EngineContext ctx = BuildContext();
        List<EmptySequenceResult> results = default(EmptySequence).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("empty-seq");
    }

    [Test]
    public async Task SingletonOneSequence_ReturnsOnlySingletonOne()
    {
        EngineContext ctx = BuildContext();
        List<SingletonOneSequenceResult> results = default(SingletonOneSequence).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("one-seq");
    }

    [Test]
    public async Task PairSequence_ReturnsOnlyTwoSeq()
    {
        EngineContext ctx = BuildContext();
        List<PairSequenceResult> results = default(PairSequence).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].name).IsEqualTo("two-seq");
    }
}
