using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Fletched.Core;
using Fletched.Core.Runtime;

namespace Fletched.Benchmarks;

[Fact]
public partial record struct BenchParentEdge(string Parent, string Child);

[Fact]
public partial record struct BenchTreeEdge(string Parent, string Child);

[Fact]
public partial record struct BenchCounterEdge(int Value, int Next);

[Fact]
public partial record struct BenchNumber(int Value);

[Predicate]
public partial record struct BenchParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<BenchParentEdge>(edge => edge.Parent == parent && edge.Child == child);
}

[Predicate]
public partial record struct BenchAncestor
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        BenchParent(parent, child) ||
        BenchAncestorStep(parent, child);
}

[Predicate]
public partial record struct BenchAncestorStep
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<string>(middle =>
            BenchParent(parent, middle) &&
            BenchAncestor(middle, child));
}

[Predicate]
public partial record struct BenchTreeParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<BenchTreeEdge>(edge => edge.Parent == parent && edge.Child == child);
}

[Predicate]
public partial record struct BenchTreeDescendant
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        BenchTreeParent(parent, child) ||
        BenchTreeDescendantStep(parent, child);
}

[Predicate]
public partial record struct BenchTreeDescendantStep
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<string>(middle =>
            BenchTreeParent(parent, middle) &&
            BenchTreeDescendant(middle, child));
}

[Predicate]
public partial record struct BenchEven
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<BenchNumber>(number => number.Value == 0 && number.Value == value) ||
        Logic.With<BenchCounterEdge>(edge => edge.Value == value && BenchOdd(edge.Next));
}

[Predicate]
public partial record struct BenchOdd
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<BenchCounterEdge>(edge => edge.Value == value && BenchEven(edge.Next));
}

[Predicate]
public partial record struct BenchEvenValues
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<BenchNumber>(number => number.Value == value && BenchEven(value));
}

[MemoryDiagnoser]
[BenchmarkCategory(nameof(Fletched.Core.Performance.BenchmarkCategory.Execution))]
public class RecursivePredicateBenchmarks
{
    [Params(10, 100, 1_000)]
    public int LinearEdgeCount { get; set; }

    [Params(2, 3, 4)]
    public int BranchingFactor { get; set; }

    [Params(5, 4)]
    public int TreeDepth { get; set; }

    [Params(32)]
    public int MutualRecursionMaxValue { get; set; }

    private EngineContext _linearContext = null!;
    private string _linearRoot = string.Empty;
    private string _linearLeaf = string.Empty;

    private EngineContext _treeContext = null!;

    private EngineContext _mutualContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        _linearContext = BuildLinearContext(LinearEdgeCount, out _linearRoot, out _linearLeaf);
        _treeContext = BuildTreeContext(BranchingFactor, TreeDepth);
        _mutualContext = BuildMutualRecursionContext(MutualRecursionMaxValue);

        ValidateScenarioCounts();
    }

    [Benchmark(Description = "RPB-001 Linear ancestor traversal")]
    public int LinearAncestorTraversal_ResultCount() =>
        default(BenchAncestor).Execute(_linearContext).Count();

    [Benchmark(Description = "RPB-002 Branching tree traversal")]
    public int BranchingTreeTraversal_ResultCount() =>
        default(BenchTreeDescendant).Execute(_treeContext).Count();

    [Benchmark(Description = "RPB-003 No-result recursive query")]
    public int NoResultRecursiveQuery_ResultCount() =>
        default(BenchAncestor).Execute(_linearContext)
            .Count(result => result.parent == _linearRoot && result.child == "missing-node");

    [Benchmark(Description = "RPB-004 Mutual recursion baseline")]
    public int MutualRecursion_ResultCount() =>
        default(BenchEvenValues).Execute(_mutualContext).Count();

    private void ValidateScenarioCounts()
    {
        int linearCount = default(BenchAncestor).Execute(_linearContext).Count();
        int expectedLinearCount = LinearEdgeCount * (LinearEdgeCount + 1) / 2;
        Ensure(linearCount == expectedLinearCount,
            $"Unexpected linear recursive result count: expected {expectedLinearCount}, actual {linearCount}.");

        int treeCount = default(BenchTreeDescendant).Execute(_treeContext).Count();
        int expectedTreeCount = ExpectedTreeAncestorPairs(BranchingFactor, TreeDepth);
        Ensure(treeCount == expectedTreeCount,
            $"Unexpected tree recursive result count: expected {expectedTreeCount}, actual {treeCount}.");

        int noResultCount = default(BenchAncestor).Execute(_linearContext)
            .Count(result => result.parent == _linearRoot && result.child == "missing-node");
        Ensure(noResultCount == 0,
            $"No-result recursive scenario returned {noResultCount} results.");

        int mutualCount = default(BenchEvenValues).Execute(_mutualContext).Count();
        int expectedMutualCount = MutualRecursionMaxValue / 2 + 1;
        Ensure(mutualCount == expectedMutualCount,
            $"Unexpected mutual recursion result count: expected {expectedMutualCount}, actual {mutualCount}.");
    }

    private static EngineContext BuildLinearContext(int edgeCount, out string root, out string leaf)
    {
        var edges = new BenchParentEdge[edgeCount];
        for (int i = 0; i < edgeCount; i++)
            edges[i] = new BenchParentEdge($"node-{i}", $"node-{i + 1}");

        var ctx = new EngineContext();
        ctx.BenchParentEdges = new FactTable<BenchParentEdge>(edges);
        root = "node-0";
        leaf = $"node-{edgeCount}";
        return ctx;
    }

    private static EngineContext BuildTreeContext(int branchingFactor, int depth)
    {
        var edges = new List<BenchTreeEdge>();
        var currentLevel = new List<string> { "root" };

        for (int level = 0; level < depth; level++)
        {
            var nextLevel = new List<string>();
            foreach (string parent in currentLevel)
            {
                for (int child = 0; child < branchingFactor; child++)
                {
                    string childId = $"{parent}-{level}-{child}";
                    edges.Add(new BenchTreeEdge(parent, childId));
                    nextLevel.Add(childId);
                }
            }

            currentLevel = nextLevel;
        }

        var ctx = new EngineContext();
        ctx.BenchTreeEdges = new FactTable<BenchTreeEdge>(edges.ToArray());
        return ctx;
    }

    private static EngineContext BuildMutualRecursionContext(int maxValue)
    {
        var counterEdges = new List<BenchCounterEdge>();
        var numbers = new List<BenchNumber>();

        for (int i = 0; i <= maxValue; i++)
        {
            numbers.Add(new BenchNumber(i));
            if (i > 0)
                counterEdges.Add(new BenchCounterEdge(i, i - 1));
        }

        var ctx = new EngineContext();
        ctx.BenchCounterEdges = new FactTable<BenchCounterEdge>(counterEdges.ToArray());
        ctx.BenchNumbers = new FactTable<BenchNumber>(numbers.ToArray());
        return ctx;
    }

    private static int ExpectedTreeAncestorPairs(int branchingFactor, int depth)
    {
        long total = 0;
        for (int level = 0; level <= depth; level++)
        {
            long nodesAtLevel = Pow(branchingFactor, level);
            int subtreeDepth = depth - level;
            long subtreeNodes = (Pow(branchingFactor, subtreeDepth + 1) - 1) / (branchingFactor - 1);
            long descendantsPerNode = subtreeNodes - 1;
            total += nodesAtLevel * descendantsPerNode;
        }

        return (int)total;
    }

    private static long Pow(int value, int exponent)
    {
        long result = 1;
        for (int i = 0; i < exponent; i++)
            result *= value;

        return result;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
