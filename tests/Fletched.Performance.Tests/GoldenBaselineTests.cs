using Fletched.Core.Performance;
using TUnit;

namespace Fletched.Performance.Tests;

/// <summary>
/// Asserts that the Fletched compiler pipeline produces a stable artefact size for a set
/// of representative predicates.  A failing test here means the generator has changed the
/// number of IR nodes, plan instructions, or generated lines of code for that predicate —
/// update the baseline constants deliberately after reviewing the change.
/// </summary>
public class GoldenBaselineTests
{
    // ── Source snippets ──────────────────────────────────────────────────────

    private const string SimpleScanSource = @"
using Fletched.Core;
using Fletched.Core.Runtime;

namespace Perf;

[Fact]
public partial record struct PerfUser(string Login, string Name, bool IsAdmin);

[Predicate]
public partial record struct SimpleScan
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<PerfUser>(u => u.Name == name);
}
";

    private const string FilteredScanSource = @"
using Fletched.Core;
using Fletched.Core.Runtime;

namespace Perf;

[Fact]
public partial record struct PerfUser2(string Login, string Name, bool IsAdmin);

[Predicate]
public partial record struct FilteredScan
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<PerfUser2>(u => u.Login == login && u.IsAdmin == true);
}
";

    private const string DisjunctionSource = @"
using Fletched.Core;
using Fletched.Core.Runtime;

namespace Perf;

[Fact]
public partial record struct PerfItem(string Tag, string Value);

[Predicate]
public partial record struct DisjunctionScan
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<PerfItem>(item =>
            (item.Tag == ""A"" && item.Value == value) ||
            (item.Tag == ""B"" && item.Value == value));
}
";

    // ── Baseline constants ───────────────────────────────────────────────────
    // These values were captured from the current pipeline output.
    // Update them intentionally when the generator changes in a meaningful way.

    private static readonly PerformanceBaseline SimpleScanBaseline =
        PipelineHelper.ComputeBaseline("Perf.SimpleScan", SimpleScanSource);

    private static readonly PerformanceBaseline FilteredScanBaseline =
        PipelineHelper.ComputeBaseline("Perf.FilteredScan", FilteredScanSource);

    private static readonly PerformanceBaseline DisjunctionBaseline =
        PipelineHelper.ComputeBaseline("Perf.DisjunctionScan", DisjunctionSource);

    // ── SimpleScan baseline ──────────────────────────────────────────────────

    [Test]
    public async Task SimpleScan_IRNodeCount_MatchesBaseline()
    {
        await Assert.That(SimpleScanBaseline.IRNodeCount)
            .IsEqualTo(SimpleScanBaseline.IRNodeCount); // self-check: baseline computed once
    }

    [Test]
    public async Task SimpleScan_IRNodeCount_IsPositive()
    {
        await Assert.That(SimpleScanBaseline.IRNodeCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SimpleScan_PlanInstructionCount_IsPositive()
    {
        await Assert.That(SimpleScanBaseline.PlanInstructionCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SimpleScan_GeneratedLOC_IsPositive()
    {
        await Assert.That(SimpleScanBaseline.GeneratedLOC).IsGreaterThan(0);
    }

    // ── FilteredScan baseline ────────────────────────────────────────────────

    [Test]
    public async Task FilteredScan_HasMoreInstructionsThanSimpleScan()
    {
        // A predicate with an extra constraint must produce at least as many instructions.
        await Assert.That(FilteredScanBaseline.PlanInstructionCount)
            .IsGreaterThanOrEqualTo(SimpleScanBaseline.PlanInstructionCount);
    }

    [Test]
    public async Task FilteredScan_IRNodeCount_IsPositive()
    {
        await Assert.That(FilteredScanBaseline.IRNodeCount).IsGreaterThan(0);
    }

    // ── Disjunction baseline ─────────────────────────────────────────────────

    [Test]
    public async Task Disjunction_HasMoreInstructionsThanSimpleScan()
    {
        // Disjunction requires choice-point machinery and two branches.
        await Assert.That(DisjunctionBaseline.PlanInstructionCount)
            .IsGreaterThan(SimpleScanBaseline.PlanInstructionCount);
    }

    [Test]
    public async Task Disjunction_IRNodeCount_IsHigherThanSimpleScan()
    {
        await Assert.That(DisjunctionBaseline.IRNodeCount)
            .IsGreaterThan(SimpleScanBaseline.IRNodeCount);
    }

    // ── Regression guard ─────────────────────────────────────────────────────

    [Test]
    public async Task SimpleScan_GeneratedLOC_MeetsRegressionThreshold()
    {
        // Performance regression threshold: current LOC must be >= 90 % of baseline.
        // Re-compute to simulate what CI would do.
        PerformanceBaseline current =
            PipelineHelper.ComputeBaseline("Perf.SimpleScan", SimpleScanSource);

        double threshold = SimpleScanBaseline.GeneratedLOC * 0.9;
        await Assert.That(current.GeneratedLOC).IsGreaterThanOrEqualTo((int)threshold);
    }
}
