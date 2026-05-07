using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Fletched.Roslyn.Pipeline;

namespace Fletched.Benchmarks;

/// <summary>
/// Benchmarks the Fletched source-generator pipeline stages:
/// semantic analysis, IR lowering, plan optimisation, and code generation.
/// Measures build-time cost per <c>[Predicate]</c> type.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(nameof(Fletched.Core.Performance.BenchmarkCategory.Generator))]
public class GeneratorBench
{
    // ── Benchmark sources ─────────────────────────────────────────────────────

    private const string SimpleScanSource = @"
using Fletched.Core;
using Fletched.Core.Runtime;
namespace BenchNs;
[Fact]  public partial record struct BenchUser(string Login, string Name, bool IsAdmin);
[Predicate]
public partial record struct BenchUserNames {
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<BenchUser>(u => u.Name == name);
}";

    private const string ConjunctionSource = @"
using Fletched.Core;
using Fletched.Core.Runtime;
namespace BenchNs;
[Fact]  public partial record struct BenchProduct(string Sku, string Category, int Price);
[Predicate]
public partial record struct BenchElectronics {
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> sku) =>
        Logic.With<BenchProduct>(p => p.Sku == sku && p.Category == ""Electronics"" && p.Price > 100);
}";

    private const string DisjunctionSource = @"
using Fletched.Core;
using Fletched.Core.Runtime;
namespace BenchNs;
[Fact]  public partial record struct BenchTag(string Key, string Value);
[Predicate]
public partial record struct BenchTagLookup {
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<BenchTag>(t =>
            (t.Key == ""A"" && t.Value == value) ||
            (t.Key == ""B"" && t.Value == value));
}";

    // ── Cached Roslyn references ──────────────────────────────────────────────

    private static readonly IReadOnlyList<MetadataReference> References = CollectReferences();

    // ── Benchmark methods ─────────────────────────────────────────────────────

    [Benchmark(Description = "Simple scan: IR + plan + emit")]
    public string SimpleScan_Build_IR_And_Plan() =>
        RunPipeline(SimpleScanSource, "BenchNs.BenchUserNames");

    [Benchmark(Description = "Conjunction chain: IR + plan + emit")]
    public string Conjunction_Build_IR_And_Plan() =>
        RunPipeline(ConjunctionSource, "BenchNs.BenchElectronics");

    [Benchmark(Description = "Disjunction: IR + plan + emit")]
    public string Disjunction_Build_IR_And_Plan() =>
        RunPipeline(DisjunctionSource, "BenchNs.BenchTagLookup");

    // ── Pipeline runner ───────────────────────────────────────────────────────

    private static string RunPipeline(string source, string typeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));

        var compilation = CSharpCompilation.Create(
            "BenchAssembly", new[] { syntaxTree }, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        INamedTypeSymbol? predicateSymbol = compilation.GetTypeByMetadataName(typeName);
        if (predicateSymbol is null) return string.Empty;

        Microsoft.CodeAnalysis.SemanticModel semanticModel =
            compilation.GetSemanticModel(syntaxTree);

        var reporter = new DiagnosticReporter();
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        PredicateModel? model = analyzer.Analyze(predicateSymbol);
        if (model is null) return string.Empty;

        var lowerer = new IrLowerer(reporter);
        PlanProgram? plan = lowerer.Lower(model);
        if (plan is null) return string.Empty;

        var optimizer = new OptimizationPipeline();
        plan = optimizer.Run(plan);

        var emitter = new Fletched.Roslyn.Emitters.PredicateEmitter(model, plan, generateLegacyNames: true);
        return emitter.Emit();
    }

    private static IReadOnlyList<MetadataReference> CollectReferences()
    {
        var refs = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            string loc = asm.Location;
            if (string.IsNullOrEmpty(loc) || !seen.Add(loc)) continue;
            refs.Add(MetadataReference.CreateFromFile(loc));
        }

        return refs;
    }
}
