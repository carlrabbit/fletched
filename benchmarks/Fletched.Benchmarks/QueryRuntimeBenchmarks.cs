using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Fletched.Core;
using Fletched.Core.Runtime;

namespace Fletched.Benchmarks;

[Predicate]
public partial record struct BenchPeopleScan
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> id) =>
        Logic.With<BenchPerson>(person => person.Id == id);
}

[Predicate]
public partial record struct BenchPeopleByCity
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> id, TerminalVar<string> city) =>
        Logic.With<BenchPerson>(person => person.City == city && person.Id == id);
}

[Predicate]
public partial record struct BenchPeopleCityJoin
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> id, TerminalVar<string> region) =>
        Logic.With<BenchPerson>(person =>
            Logic.With<BenchCity>(city =>
                person.Id == id &&
                person.City == city.Name &&
                city.Region == region));
}

[MemoryDiagnoser]
[BenchmarkCategory(nameof(Fletched.Core.Performance.BenchmarkCategory.Execution))]
public class QueryRuntimeBenchmarks
{
    private EngineContext _ctx = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int FactCount { get; set; }

    [Params(0, 1, 10, 100)]
    public int SelectivityPercent { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _ctx = BenchmarkData.CreatePeopleContext(FactCount, cityCount: 100, edgeCount: FactCount / 2, selectivitySeed: SelectivityPercent);
    }

    [Benchmark]
    public int SimpleFactScan()
    {
        var options = new QueryExecutionOptions { Metrics = new QueryMetrics() };
        return default(BenchPeopleScan).Execute(_ctx, options: options).Count();
    }

    [Benchmark]
    public int IndexedFactLookup()
    {
        var options = new QueryExecutionOptions { Metrics = new QueryMetrics() };
        string city = SelectivityPercent == 0 ? "city-missing" : $"city-{(SelectivityPercent % 100):D2}";
        return default(BenchPeopleByCity).Execute(_ctx, options: options)
            .Count(result => result.city == city);
    }

    [Benchmark]
    public int TwoSourceJoin()
    {
        var options = new QueryExecutionOptions { Metrics = new QueryMetrics() };
        return default(BenchPeopleCityJoin).Execute(_ctx, options: options).Count();
    }

    [Benchmark]
    public int RecursiveTabledQuery()
    {
        var options = new QueryExecutionOptions { Metrics = new QueryMetrics() };
        return default(BenchTabledAncestor).Execute(_ctx, options: options).Count();
    }

    [Benchmark]
    public async Task<int> AsyncExecuteOverhead()
    {
        var options = new QueryExecutionOptions { Metrics = new QueryMetrics() };
        int count = 0;
        await foreach (var _ in default(BenchPeopleScan).ExecuteAsync(_ctx, options: options))
            count++;
        return count;
    }
}
