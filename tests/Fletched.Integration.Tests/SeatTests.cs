using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

/// <summary>A seat on a classroom grid, identified by (Row, Col) and occupied by a student number.</summary>
[Fact]
public readonly partial record struct Seat(int Row, int Col, int Student);

// ── Predicates ────────────────────────────────────────────────────────────────

/// <summary>
/// Returns every (topStudent, bottomStudent) pair where the top seat is directly
/// above the bottom seat (same column, topRow + 1 == bottomRow).
/// Exercises arithmetic in unification: <c>top.Row + 1 == bottom.Row</c>.
/// </summary>
[Predicate]
public readonly partial record struct VerticalNeighbors
{
    [PredicateBody]
    public static LogicExpr<bool> Body(
        TerminalVar<int> topStudent,
        TerminalVar<int> bottomStudent) =>
        Logic.With<Seat, Seat>((top, bottom) =>
            top.Student == topStudent &&
            bottom.Student == bottomStudent &&
            top.Row + 1 == bottom.Row &&
            top.Col == bottom.Col);
}

/// <summary>
/// Returns every seat whose student number is ≤ 8.
/// Exercises the &lt;= comparison operator inside a With body.
/// </summary>
[Predicate]
public readonly partial record struct LowNumberedSeat
{
    [PredicateBody]
    public static LogicExpr<bool> Body(
        TerminalVar<int> row,
        TerminalVar<int> col,
        TerminalVar<int> student) =>
        Logic.With<Seat>(s =>
            s.Row == row &&
            s.Col == col &&
            s.Student == student &&
            s.Student <= 8);
}

/// <summary>
/// Returns every (neighbor, student) pair from the seat table that satisfies the
/// seating-compatibility rules (expressed as disjunctions inside a With body):
/// <list type="bullet">
///   <item>student == 3  ⟹  neighbor &lt; 9</item>
///   <item>student == 5  ⟹  neighbor &lt; 9</item>
///   <item>student == 2  ⟹  neighbor ≠ 3</item>
///   <item>student == 10 ⟹  neighbor &gt; 8</item>
/// </list>
/// Exercises disjunctions (||) inside a With body.
/// </summary>
[Predicate]
public readonly partial record struct CompatiblePair
{
    [PredicateBody]
    public static LogicExpr<bool> Body(
        TerminalVar<int> neighbor,
        TerminalVar<int> student) =>
        Logic.With<Seat, Seat>((s1, s2) =>
            s1.Student == neighbor &&
            s2.Student == student &&
            (s2.Student != 3 || s1.Student < 9) &&
            (s2.Student != 5 || s1.Student < 9) &&
            (s2.Student != 2 || s1.Student != 3) &&
            (s2.Student != 10 || s1.Student > 8));
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class SeatTests
{
    // 4×4 seating arrangement — all compatibility constraints are satisfied:
    //
    //  Row\Col  1    2    3    4
    //  Row 1:   3    5    1    2
    //  Row 2:   4    6    7    8
    //  Row 3:  16   11   12    9
    //  Row 4:  14   10   15   13
    //
    // Vertical neighbours (top above bottom, same column):
    //   (3,4)  (5,6)  (1,7)  (2,8)   ← rows 1–2
    //   (4,16) (6,11) (7,12) (8,9)   ← rows 2–3
    //   (16,14)(11,10)(12,15)(9,13)  ← rows 3–4  → 12 pairs total
    //
    // Low-numbered seats (student ≤ 8): students 1–8 → 8 seats
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.Seats = new FactTable<Seat>(new[]
        {
            new Seat(1, 1,  3), new Seat(1, 2,  5), new Seat(1, 3,  1), new Seat(1, 4,  2),
            new Seat(2, 1,  4), new Seat(2, 2,  6), new Seat(2, 3,  7), new Seat(2, 4,  8),
            new Seat(3, 1, 16), new Seat(3, 2, 11), new Seat(3, 3, 12), new Seat(3, 4,  9),
            new Seat(4, 1, 14), new Seat(4, 2, 10), new Seat(4, 3, 15), new Seat(4, 4, 13),
        });
        return ctx;
    }

    // ── VerticalNeighbors ─────────────────────────────────────────────────────

    [Test]
    public async Task VerticalNeighbors_Returns12Pairs()
    {
        EngineContext ctx = BuildContext();
        List<VerticalNeighborsResult> results =
            await default(VerticalNeighbors).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(12);
    }

    [Test]
    public async Task VerticalNeighbors_Row1To2_ContainsExpectedPairs()
    {
        EngineContext ctx = BuildContext();
        List<VerticalNeighborsResult> results =
            await default(VerticalNeighbors).ExecuteAsync(ctx).ToListAsync();

        // Student 3 (row 1, col 1) sits directly above student 4 (row 2, col 1).
        bool threeAboveFour = results.Any(r => r.topStudent == 3 && r.bottomStudent == 4);
        await Assert.That(threeAboveFour).IsTrue();

        // Student 5 (row 1, col 2) sits directly above student 6 (row 2, col 2).
        bool fiveAboveSix = results.Any(r => r.topStudent == 5 && r.bottomStudent == 6);
        await Assert.That(fiveAboveSix).IsTrue();
    }

    [Test]
    public async Task VerticalNeighbors_NonAdjacentPair_IsNotReturned()
    {
        EngineContext ctx = BuildContext();
        // Students 3 (row 1) and 16 (row 3) are NOT vertically adjacent.
        bool threeAboveSixteen = (await default(VerticalNeighbors).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.topStudent == 3 && r.bottomStudent == 16);

        await Assert.That(threeAboveSixteen).IsFalse();
    }

    // ── LowNumberedSeat ───────────────────────────────────────────────────────

    [Test]
    public async Task LowNumberedSeat_Returns8Seats()
    {
        EngineContext ctx = BuildContext();
        List<LowNumberedSeatResult> results =
            await default(LowNumberedSeat).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(8);
    }

    [Test]
    public async Task LowNumberedSeat_ContainsStudent1()
    {
        EngineContext ctx = BuildContext();
        bool hasStudent1 = (await default(LowNumberedSeat).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.student == 1);

        await Assert.That(hasStudent1).IsTrue();
    }

    [Test]
    public async Task LowNumberedSeat_DoesNotContainStudent9()
    {
        EngineContext ctx = BuildContext();
        bool hasStudent9 = (await default(LowNumberedSeat).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.student == 9);

        await Assert.That(hasStudent9).IsFalse();
    }

    // ── CompatiblePair ────────────────────────────────────────────────────────

    [Test]
    public async Task CompatiblePair_Student3WithNeighbor5_IsCompatible()
    {
        EngineContext ctx = BuildContext();
        // student=3 requires neighbor<9; neighbor=5 satisfies this.
        bool compatible = (await default(CompatiblePair).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.neighbor == 5 && r.student == 3);

        await Assert.That(compatible).IsTrue();
    }

    [Test]
    public async Task CompatiblePair_Student3WithNeighbor16_IsIncompatible()
    {
        EngineContext ctx = BuildContext();
        // student=3 requires neighbor<9; neighbor=16 violates this.
        bool compatible = (await default(CompatiblePair).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.neighbor == 16 && r.student == 3);

        await Assert.That(compatible).IsFalse();
    }

    [Test]
    public async Task CompatiblePair_Student2WithNeighbor3_IsIncompatible()
    {
        EngineContext ctx = BuildContext();
        // student=2 requires neighbor≠3; neighbor=3 violates this.
        bool compatible = (await default(CompatiblePair).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.neighbor == 3 && r.student == 2);

        await Assert.That(compatible).IsFalse();
    }

    [Test]
    public async Task CompatiblePair_Student10WithNeighbor5_IsIncompatible()
    {
        EngineContext ctx = BuildContext();
        // student=10 requires neighbor>8; neighbor=5 violates this.
        bool compatible = (await default(CompatiblePair).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.neighbor == 5 && r.student == 10);

        await Assert.That(compatible).IsFalse();
    }

    [Test]
    public async Task CompatiblePair_Student10WithNeighbor11_IsCompatible()
    {
        EngineContext ctx = BuildContext();
        // student=10 requires neighbor>8; neighbor=11 satisfies this.
        bool compatible = (await default(CompatiblePair).ExecuteAsync(ctx).ToListAsync())
            .Any(r => r.neighbor == 11 && r.student == 10);

        await Assert.That(compatible).IsTrue();
    }
}
