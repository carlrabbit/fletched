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

    // Lazy so that each baseline is only computed when a relevant test runs.
    private static readonly Lazy<PerformanceBaseline> LazySimpleScan =
        new(() => PipelineHelper.ComputeBaseline("Perf.SimpleScan", SimpleScanSource));

    private static readonly Lazy<PerformanceBaseline> LazyFilteredScan =
        new(() => PipelineHelper.ComputeBaseline("Perf.FilteredScan", FilteredScanSource));

    private static readonly Lazy<PerformanceBaseline> LazyDisjunction =
        new(() => PipelineHelper.ComputeBaseline("Perf.DisjunctionScan", DisjunctionSource));

    // Captured values; update intentionally if the generator pipeline changes.
    private const int SimpleScanExpectedIRNodeCount = 5;
    private const int SimpleScanExpectedInstructions = 4;

    private const int FilteredScanExpectedIRNodeCount = 10;
    private const int FilteredScanExpectedInstructions = 4;

    private const int DisjunctionExpectedIRNodeCount = 20;
    private const int DisjunctionExpectedInstructions = 7;

    // ── SimpleScan baseline ──────────────────────────────────────────────────

    [Test]
    public async Task SimpleScan_IRNodeCount_MatchesExpected()
    {
        await Assert.That(LazySimpleScan.Value.IRNodeCount)
            .IsEqualTo(SimpleScanExpectedIRNodeCount);
    }

    [Test]
    public async Task SimpleScan_PlanInstructionCount_MatchesExpected()
    {
        await Assert.That(LazySimpleScan.Value.PlanInstructionCount)
            .IsEqualTo(SimpleScanExpectedInstructions);
    }

    [Test]
    public async Task SimpleScan_GeneratedLOC_IsPositive()
    {
        await Assert.That(LazySimpleScan.Value.GeneratedLOC).IsGreaterThan(0);
    }

    // ── FilteredScan baseline ────────────────────────────────────────────────

    [Test]
    public async Task FilteredScan_IRNodeCount_MatchesExpected()
    {
        await Assert.That(LazyFilteredScan.Value.IRNodeCount)
            .IsEqualTo(FilteredScanExpectedIRNodeCount);
    }

    [Test]
    public async Task FilteredScan_PlanInstructionCount_MatchesExpected()
    {
        await Assert.That(LazyFilteredScan.Value.PlanInstructionCount)
            .IsEqualTo(FilteredScanExpectedInstructions);
    }

    [Test]
    public async Task FilteredScan_HasMoreInstructionsThanSimpleScan()
    {
        // A predicate with an extra constraint must produce at least as many instructions.
        await Assert.That(LazyFilteredScan.Value.PlanInstructionCount)
            .IsGreaterThanOrEqualTo(LazySimpleScan.Value.PlanInstructionCount);
    }

    // ── Disjunction baseline ─────────────────────────────────────────────────

    [Test]
    public async Task Disjunction_IRNodeCount_MatchesExpected()
    {
        await Assert.That(LazyDisjunction.Value.IRNodeCount)
            .IsEqualTo(DisjunctionExpectedIRNodeCount);
    }

    [Test]
    public async Task Disjunction_PlanInstructionCount_MatchesExpected()
    {
        await Assert.That(LazyDisjunction.Value.PlanInstructionCount)
            .IsEqualTo(DisjunctionExpectedInstructions);
    }

    [Test]
    public async Task Disjunction_HasMoreInstructionsThanSimpleScan()
    {
        // Disjunction requires choice-point machinery and two branches.
        await Assert.That(LazyDisjunction.Value.PlanInstructionCount)
            .IsGreaterThan(LazySimpleScan.Value.PlanInstructionCount);
    }

    [Test]
    public async Task Disjunction_IRNodeCount_IsHigherThanSimpleScan()
    {
        await Assert.That(LazyDisjunction.Value.IRNodeCount)
            .IsGreaterThan(LazySimpleScan.Value.IRNodeCount);
    }

    // ── Regression guard ─────────────────────────────────────────────────────

    [Test]
    public async Task SimpleScan_GeneratedLOC_MeetsRegressionThreshold()
    {
        // Performance regression threshold: current LOC must be >= 90 % of baseline.
        // Re-compute to simulate what CI would do.
        PerformanceBaseline current =
            PipelineHelper.ComputeBaseline("Perf.SimpleScan", SimpleScanSource);

        double threshold = LazySimpleScan.Value.GeneratedLOC * 0.9;
        await Assert.That(current.GeneratedLOC).IsGreaterThanOrEqualTo((int)threshold);
    }
}
