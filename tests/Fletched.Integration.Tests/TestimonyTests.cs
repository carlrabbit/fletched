using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

/// <summary>The three participants in the testimony scenario.</summary>
public enum Witness { A, B, C }

/// <summary>The kind of claim a witness makes about another person.</summary>
public enum ClaimKind { Friend, Enemy, Stranger, OutOfTown, InTown }

/// <summary>
/// A single piece of testimony: <see cref="Who"/> claims that <see cref="About"/>
/// has the relationship or status described by <see cref="Kind"/>.
/// </summary>
[Fact]
public partial record struct Testimony(Witness Who, ClaimKind Kind, Witness About);

// ── Predicates ────────────────────────────────────────────────────────────────

/// <summary>
/// Finds ordered pairs of testimonies (<c>who1</c>, <c>who2</c>) that make
/// contradictory claims about the same person (<c>about</c>).
/// Two claims are contradictory when:
/// <list type="bullet">
///   <item>one says Friend and the other says Enemy (about the same person)</item>
///   <item>one says Friend and the other says Stranger</item>
///   <item>one says Enemy and the other says Stranger</item>
///   <item>one says OutOfTown and the other says InTown</item>
/// </list>
/// </summary>
[Predicate]
public partial record struct InconsistentPair
{
    [PredicateBody]
    public static LogicExpr<bool> Body(
        TerminalVar<Witness> who1,
        TerminalVar<Witness> who2,
        TerminalVar<Witness> about) =>
        Logic.With<Testimony, Testimony>((t1, t2) =>
            t1.Who   == who1  &&
            t2.Who   == who2  &&
            t1.About == about &&
            t2.About == about &&
            (
                (t1.Kind == ClaimKind.Friend   && t2.Kind == ClaimKind.Enemy)    ||
                (t1.Kind == ClaimKind.Friend   && t2.Kind == ClaimKind.Stranger) ||
                (t1.Kind == ClaimKind.Enemy    && t2.Kind == ClaimKind.Stranger) ||
                (t1.Kind == ClaimKind.OutOfTown && t2.Kind == ClaimKind.InTown)
            )
        );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Integration tests for the testimony scenario.
/// Each test is limited to 5 minutes via <see cref="TUnit.Core.TimeoutAttribute"/>.
/// </summary>
[Timeout(300_000)]
public class TestimonyTests
{
    // Testimony dataset:
    //
    //   A claims: B is a Friend, C is an Enemy
    //   B claims: B is OutOfTown, B is a Stranger
    //   C claims: C is InTown, A is InTown, B is InTown
    //
    // Ordered pairs of inconsistent testimonies:
    //   (A, B, B): A says Friend(B), B says Stranger(B)  → Friend ↔ Stranger for B
    //   (B, C, B): B says OutOfTown(B), C says InTown(B) → OutOfTown ↔ InTown for B
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.Testimonys = new FactTable<Testimony>(new[]
        {
            new Testimony(Witness.A, ClaimKind.Friend,    Witness.B),
            new Testimony(Witness.A, ClaimKind.Enemy,     Witness.C),
            new Testimony(Witness.B, ClaimKind.OutOfTown, Witness.B),
            new Testimony(Witness.B, ClaimKind.Stranger,  Witness.B),
            new Testimony(Witness.C, ClaimKind.InTown,    Witness.C),
            new Testimony(Witness.C, ClaimKind.InTown,    Witness.A),
            new Testimony(Witness.C, ClaimKind.InTown,    Witness.B),
        });
        return ctx;
    }

    // ── InconsistentPair — result count ───────────────────────────────────────

    [Test]
    public async Task InconsistentPair_ReturnsExactlyTwoPairs(CancellationToken ct)
    {
        EngineContext ctx = BuildContext();
        List<InconsistentPairResult> results =
            default(InconsistentPair).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    // ── InconsistentPair — Friend / Stranger conflict ─────────────────────────

    [Test]
    public async Task InconsistentPair_A_says_FriendB_B_says_StrangerB_IsFound(CancellationToken ct)
    {
        EngineContext ctx = BuildContext();
        // A says B is a Friend; B says B is a Stranger → Friend ↔ Stranger about B
        bool found = default(InconsistentPair).Execute(ctx)
            .Any(r => r.who1  == Witness.A &&
                      r.who2  == Witness.B &&
                      r.about == Witness.B);

        await Assert.That(found).IsTrue();
    }

    // ── InconsistentPair — OutOfTown / InTown conflict ────────────────────────

    [Test]
    public async Task InconsistentPair_B_says_OutOfTownB_C_says_InTownB_IsFound(CancellationToken ct)
    {
        EngineContext ctx = BuildContext();
        // B says B is OutOfTown; C says B is InTown → OutOfTown ↔ InTown about B
        bool found = default(InconsistentPair).Execute(ctx)
            .Any(r => r.who1  == Witness.B &&
                      r.who2  == Witness.C &&
                      r.about == Witness.B);

        await Assert.That(found).IsTrue();
    }

    // ── InconsistentPair — non-conflicting pairs are absent ───────────────────

    [Test]
    public async Task InconsistentPair_SameWitnessBothSides_IsNeverReturned(CancellationToken ct)
    {
        EngineContext ctx = BuildContext();
        // A witness is never inconsistent with themselves in this dataset.
        bool selfConflict = default(InconsistentPair).Execute(ctx)
            .Any(r => r.who1 == r.who2);

        await Assert.That(selfConflict).IsFalse();
    }

    [Test]
    public async Task InconsistentPair_NoConflictAboutC(CancellationToken ct)
    {
        EngineContext ctx = BuildContext();
        // No two witnesses make contradictory claims about person C in this dataset.
        bool aboutC = default(InconsistentPair).Execute(ctx)
            .Any(r => r.about == Witness.C);

        await Assert.That(aboutC).IsFalse();
    }

    [Test]
    public async Task InconsistentPair_NoConflictAboutA(CancellationToken ct)
    {
        EngineContext ctx = BuildContext();
        // No two witnesses make contradictory claims about person A in this dataset.
        bool aboutA = default(InconsistentPair).Execute(ctx)
            .Any(r => r.about == Witness.A);

        await Assert.That(aboutA).IsFalse();
    }

    // ── InconsistentPair — empty table ────────────────────────────────────────

    [Test]
    public async Task InconsistentPair_EmptyTable_ReturnsNoResults(CancellationToken ct)
    {
        var ctx = new EngineContext();
        ctx.Testimonys = new FactTable<Testimony>(System.Array.Empty<Testimony>());

        List<InconsistentPairResult> results =
            default(InconsistentPair).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── InconsistentPair — dataset without any contradictions ─────────────────

    [Test]
    public async Task InconsistentPair_AllWitnessesAgree_ReturnsNoResults(CancellationToken ct)
    {
        var ctx = new EngineContext();
        // All witnesses agree that B is InTown — no contradictions.
        ctx.Testimonys = new FactTable<Testimony>(new[]
        {
            new Testimony(Witness.A, ClaimKind.InTown, Witness.B),
            new Testimony(Witness.B, ClaimKind.InTown, Witness.B),
            new Testimony(Witness.C, ClaimKind.InTown, Witness.B),
        });

        List<InconsistentPairResult> results =
            default(InconsistentPair).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── InconsistentPair — every conflict type appears at least once ──────────

    [Test]
    public async Task InconsistentPair_FriendEnemyConflict_IsDetected(CancellationToken ct)
    {
        var ctx = new EngineContext();
        ctx.Testimonys = new FactTable<Testimony>(new[]
        {
            new Testimony(Witness.A, ClaimKind.Friend, Witness.C),
            new Testimony(Witness.B, ClaimKind.Enemy,  Witness.C),
        });

        List<InconsistentPairResult> results =
            default(InconsistentPair).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].about).IsEqualTo(Witness.C);
    }

    [Test]
    public async Task InconsistentPair_EnemyStrangerConflict_IsDetected(CancellationToken ct)
    {
        var ctx = new EngineContext();
        ctx.Testimonys = new FactTable<Testimony>(new[]
        {
            new Testimony(Witness.A, ClaimKind.Enemy,    Witness.C),
            new Testimony(Witness.B, ClaimKind.Stranger, Witness.C),
        });

        List<InconsistentPairResult> results =
            default(InconsistentPair).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].about).IsEqualTo(Witness.C);
    }
}

