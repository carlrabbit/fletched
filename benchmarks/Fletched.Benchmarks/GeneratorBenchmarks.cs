using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using Fletched.Roslyn.Emitters;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fletched.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory(nameof(Fletched.Core.Performance.BenchmarkCategory.Generator))]
public class GeneratorBenchmarks
{
    private static readonly IReadOnlyList<MetadataReference> References = CollectReferences();

    [Params("SmallFacts", "JoinQuery", "PredicateInvocation", "Negation", "RecursiveQuery", "MagicSetQuery", "LargeModule")]
    public string Scenario { get; set; } = string.Empty;

    [Benchmark]
    public GeneratorPerformanceResult RunGenerator()
    {
        string source = ScenarioSources[Scenario];
        string typeName = ScenarioPredicateTypes[Scenario];
        return RunScenario(Scenario, source, typeName);
    }

    private static GeneratorPerformanceResult RunScenario(string scenario, string source, string typeName)
    {
        var total = Stopwatch.StartNew();
        var syntaxSw = Stopwatch.StartNew();
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        syntaxSw.Stop();

        var semanticSw = Stopwatch.StartNew();
        var compilation = CSharpCompilation.Create(
            $"Benchmark_{scenario}",
            [syntaxTree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
        INamedTypeSymbol? predicateSymbol = compilation.GetTypeByMetadataName(typeName);
        semanticSw.Stop();

        if (predicateSymbol is null)
        {
            total.Stop();
            return EmptyResult(scenario, total.Elapsed);
        }

        var dslSw = Stopwatch.StartNew();
        var reporter = new DiagnosticReporter();
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        PredicateModel? model = analyzer.Analyze(predicateSymbol);
        dslSw.Stop();

        if (model is null)
        {
            total.Stop();
            return EmptyResult(scenario, total.Elapsed) with { DiagnosticCount = reporter.Diagnostics.Count };
        }

        var loweringSw = Stopwatch.StartNew();
        var callGraph = PredicateCallGraph.Create([model]);
        var lowerer = new IrLowerer(reporter);
        PlanProgram? plan = lowerer.Lower(model, callGraph);
        loweringSw.Stop();

        if (plan is null)
        {
            total.Stop();
            return EmptyResult(scenario, total.Elapsed) with { DiagnosticCount = reporter.Diagnostics.Count };
        }

        TimeSpan planning = TimeSpan.Zero;
        TimeSpan recursivePlanning = TimeSpan.Zero;

        var optimizationSw = Stopwatch.StartNew();
        var optimizer = new OptimizationPipeline();
        PlanProgram optimizedPlan = optimizer.Run(plan);
        optimizationSw.Stop();

        var emissionSw = Stopwatch.StartNew();
        var emitter = new PredicateEmitter(model, optimizedPlan, generateLegacyNames: true);
        string emitted = emitter.Emit();
        emissionSw.Stop();

        total.Stop();

        int lineCount = emitted.Split('\n').Length;
        long bytes = Encoding.UTF8.GetByteCount(emitted);
        int memberCount = emitted.Split(" record ", StringSplitOptions.None).Length - 1;

        return new GeneratorPerformanceResult(
            scenario,
            total.Elapsed,
            syntaxSw.Elapsed,
            semanticSw.Elapsed,
            dslSw.Elapsed,
            loweringSw.Elapsed,
            planning,
            recursivePlanning,
            optimizationSw.Elapsed,
            emissionSw.Elapsed,
            GeneratedFileCount: 1,
            GeneratedLineCount: lineCount,
            GeneratedByteCount: bytes,
            GeneratedMemberCount: memberCount,
            DiagnosticCount: reporter.Diagnostics.Count);
    }

    private static GeneratorPerformanceResult EmptyResult(string scenario, TimeSpan total) =>
        new(
            scenario,
            total,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            0,
            0,
            0,
            0,
            0);

    private static readonly IReadOnlyDictionary<string, string> ScenarioPredicateTypes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SmallFacts"] = "BenchScenarios.SmallFactsPredicate",
            ["JoinQuery"] = "BenchScenarios.JoinPredicate",
            ["PredicateInvocation"] = "BenchScenarios.CallerPredicate",
            ["Negation"] = "BenchScenarios.NegationPredicate",
            ["RecursiveQuery"] = "BenchScenarios.RecursivePredicate",
            ["MagicSetQuery"] = "BenchScenarios.MagicSetPredicate",
            ["LargeModule"] = "BenchScenarios.LargeModulePredicate",
        };

    private static readonly IReadOnlyDictionary<string, string> ScenarioSources =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SmallFacts"] = CommonHeader + """
                [Fact] public partial record struct User(string Login);
                [Predicate] public partial record struct SmallFactsPredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
                        Logic.With<User>(user => user.Login == login);
                }
                """,
            ["JoinQuery"] = CommonHeader + """
                [Fact] public partial record struct User(string Login, string City);
                [Fact] public partial record struct City(string Name, string Region);
                [Predicate] public partial record struct JoinPredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> login, TerminalVar<string> region) =>
                        Logic.With<User>(u => Logic.With<City>(c => u.Login == login && u.City == c.Name && c.Region == region));
                }
                """,
            ["PredicateInvocation"] = CommonHeader + """
                [Fact] public partial record struct User(string Login);
                [Predicate] public partial record struct CalleePredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> login) => Logic.With<User>(u => u.Login == login);
                }
                [Predicate] public partial record struct CallerPredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> login) => CalleePredicate(login);
                }
                """,
            ["Negation"] = CommonHeader + """
                [Fact] public partial record struct User(string Login, bool Active);
                [Predicate] public partial record struct NegationPredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
                        Logic.With<User>(u => u.Login == login && Logic.Not(Logic.With<User>(x => x.Login == login && x.Active == false)));
                }
                """,
            ["RecursiveQuery"] = CommonHeader + """
                [Fact] public partial record struct Edge(string Parent, string Child);
                [Predicate] public partial record struct RecursivePredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
                        Logic.With<Edge>(e => e.Parent == parent && e.Child == child) ||
                        Logic.With<string>(middle => Logic.With<Edge>(e => e.Parent == parent && e.Child == middle) && RecursivePredicate(middle, child));
                }
                """,
            ["MagicSetQuery"] = CommonHeader + """
                [Fact] public partial record struct Edge(string Parent, string Child);
                [Predicate] public partial record struct MagicSetPredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> child) =>
                        Logic.With<string>(root => root == "node-0" && MagicSetStep(root, child));
                }
                [Predicate] public partial record struct MagicSetStep
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
                        Logic.With<Edge>(e => e.Parent == parent && e.Child == child) ||
                        Logic.With<string>(middle => Logic.With<Edge>(e => e.Parent == parent && e.Child == middle) && MagicSetStep(middle, child));
                }
                """,
            ["LargeModule"] = CommonHeader + """
                [Fact] public partial record struct Item(string Id, string Bucket);
                [Fact] public partial record struct Bucket(string Name, string Group);
                [Predicate] public partial record struct LargeModulePredicate
                {
                    [PredicateBody]
                    public static LogicExpr<bool> Body(TerminalVar<string> id, TerminalVar<string> group) =>
                        Logic.With<Item>(i => Logic.With<Bucket>(b => i.Id == id && i.Bucket == b.Name && b.Group == group));
                }
                """,
        };

    private const string CommonHeader = """
        using Fletched.Core;
        namespace BenchScenarios;
        """;

    private static IReadOnlyList<MetadataReference> CollectReferences()
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
                continue;

            if (!seen.Add(assembly.Location))
                continue;

            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return references;
    }
}
