using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Fletched.Roslyn.Emitters;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class PlanExplanationTests
{
    [Test]
    public async Task PlanningExplanationBuilder_Build_PopulatesMajorSections()
    {
        const string source = """
            using Fletched.Core;

            [Fact]
            public partial record struct Toaster(string Name, string Size);

            [Fact]
            public partial record struct Bread(string Brand, string Size);

            [Predicate]
            public partial record struct MakBread
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> size, TerminalVar<string> brand) =>
                    Logic.With<Bread>(b => b.Size == size && b.Brand == brand);
            }

            [Predicate]
            public partial record struct RightSizedBread
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> toaster, TerminalVar<string> brand) =>
                    Logic.With<Toaster>(t =>
                        Logic.With<Bread>(b =>
                            t.Name == toaster &&
                            t.Size == b.Size &&
                            MakBread(b.Size, brand)));
            }
            """;

        (PredicateModel model, PlanProgram plan, PlanOptimizationTrace trace, DiagnosticReporter reporter) = BuildOptimizedPlan(source, "RightSizedBread");

        var builder = new PlanningExplanationBuilder();
        PlanExplanation explanation = builder.Build(model, plan, trace, reporter.Diagnostics);

        await Assert.That(explanation.Query.PredicateName).IsEqualTo("RightSizedBread");
        await Assert.That(explanation.Semantic.Variables.Length).IsGreaterThan(0);
        await Assert.That(explanation.Ir.Nodes.Length).IsGreaterThan(0);
        await Assert.That(explanation.PlannedIr.Blocks.Length).IsGreaterThan(0);
        await Assert.That(explanation.RecursivePlanning.AccessPaths.Length).IsGreaterThanOrEqualTo(0);
        await Assert.That(explanation.Optimization.Passes.Length).IsGreaterThan(0);
        await Assert.That(explanation.CodeEmission.Members.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task PlanExplanationRenderer_Outputs_AreDeterministicAndJsonIsValid()
    {
        const string source = """
            using Fletched.Core;

            [Fact]
            public partial record struct User(string Login);

            [Predicate]
            public partial record struct ActiveUser
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> login) =>
                    Logic.With<User>(u => u.Login == login);
            }
            """;

        (PredicateModel model, PlanProgram plan, PlanOptimizationTrace trace, DiagnosticReporter reporter) = BuildOptimizedPlan(source, "ActiveUser");
        var builder = new PlanningExplanationBuilder();
        PlanExplanation explanation = builder.Build(model, plan, trace, reporter.Diagnostics);

        string plain1 = builder.RenderPlainText(explanation);
        string plain2 = builder.RenderPlainText(explanation);
        string markdown1 = builder.RenderMarkdown(explanation);
        string markdown2 = builder.RenderMarkdown(explanation);
        string json1 = builder.RenderJson(explanation);
        string json2 = builder.RenderJson(explanation);

        await Assert.That(plain1).IsEqualTo(plain2);
        await Assert.That(markdown1).IsEqualTo(markdown2);
        await Assert.That(json1).IsEqualTo(json2);

        using JsonDocument document = JsonDocument.Parse(json1);
        await Assert.That(document.RootElement.TryGetProperty("Query", out _)).IsTrue();
        await Assert.That(document.RootElement.TryGetProperty("Semantic", out _)).IsTrue();
        await Assert.That(document.RootElement.TryGetProperty("PlannedIr", out _)).IsTrue();
    }

    [Test]
    public async Task PlanningExplanationBuilder_Build_DoesNotAffectGeneratedSource()
    {
        const string source = """
            using Fletched.Core;

            [Fact]
            public partial record struct Product(string Sku);

            [Predicate]
            public partial record struct ProductBySku
            {
                [PredicateBody]
                public static LogicExpr<bool> Body(TerminalVar<string> sku) =>
                    Logic.With<Product>(p => p.Sku == sku);
            }
            """;

        (PredicateModel model, PlanProgram plan, _, DiagnosticReporter reporter) = BuildOptimizedPlan(source, "ProductBySku");
        string before = new PredicateEmitter(model, plan, generateLegacyNames: true).Emit();

        var builder = new PlanningExplanationBuilder();
        _ = builder.Build(model, plan, optimizationTrace: null, reporter.Diagnostics);

        string after = new PredicateEmitter(model, plan, generateLegacyNames: true).Emit();
        await Assert.That(after).IsEqualTo(before);
    }

    private static (PredicateModel Model, PlanProgram Plan, PlanOptimizationTrace Trace, DiagnosticReporter Reporter) BuildOptimizedPlan(
        string source,
        string predicateName)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        var reporter = new DiagnosticReporter();
        INamedTypeSymbol predicateType = compilation.GetTypeByMetadataName(predicateName)
            ?? compilation.GlobalNamespace
                .GetTypeMembers()
                .First(symbol => string.Equals(symbol.Name, predicateName, StringComparison.Ordinal));

        SyntaxReference? syntaxReference = predicateType.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference is null)
            throw new InvalidOperationException($"Could not locate syntax for '{predicateName}'.");

        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        PredicateModel model = analyzer.Analyze(predicateType)
            ?? throw new InvalidOperationException($"Could not analyze predicate '{predicateName}'.");

        PredicateCallGraph callGraph = PredicateCallGraph.Create([model]);
        var lowerer = new IrLowerer(reporter);
        PlanProgram? lowered = lowerer.Lower(model, callGraph);
        if (lowered is null)
            throw new InvalidOperationException("Lowering failed.");

        var pipeline = new OptimizationPipeline();
        (PlanProgram optimized, PlanOptimizationTrace trace) = pipeline.RunWithTrace(
            lowered,
            new PlanOptimizationContext
            {
                Options = new OptimizationOptions
                {
                    EmitOptimizationTrace = true
                }
            });

        return (model, optimized, trace, reporter);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            if (string.IsNullOrWhiteSpace(assembly.Location) || !seen.Add(assembly.Location))
                continue;

            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return CSharpCompilation.Create(
            "PlanExplanationTestsAssembly",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}
