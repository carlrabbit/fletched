using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Fletched.Core.Performance;
using Fletched.Roslyn.Pipeline;

namespace Fletched.Performance.Tests;

/// <summary>
/// Compiles a source snippet containing a single <c>[Predicate]</c> type and runs the
/// Fletched pipeline to produce a <see cref="PerformanceBaseline"/>.
/// </summary>
internal static class PipelineHelper
{
    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Compiles <paramref name="source"/> (which must declare exactly one
    /// <c>[Predicate]</c> type), runs the full Fletched pipeline and returns the
    /// measured baseline.
    /// </summary>
    public static PerformanceBaseline ComputeBaseline(string predicateName, string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);

        // Locate the predicate type symbol
        INamedTypeSymbol? predicateSymbol = compilation.GetTypeByMetadataName(predicateName)
            ?? compilation.GlobalNamespace
                .GetNamespaceMembers()
                .SelectMany(ns => ns.GetTypeMembers())
                .Concat(compilation.GlobalNamespace.GetTypeMembers())
                .FirstOrDefault(t => t.Name == predicateName.Split('.').Last());

        if (predicateSymbol is null)
            throw new InvalidOperationException($"Type '{predicateName}' not found in compilation.");

        // Find the syntax tree that contains the predicate and get its semantic model.
        SyntaxTree? tree = compilation.SyntaxTrees
            .FirstOrDefault(t => t.GetRoot().ToString().Contains(predicateName.Split('.').Last()));
        if (tree is null)
            throw new InvalidOperationException("Could not find syntax tree for predicate.");

        Microsoft.CodeAnalysis.SemanticModel semanticModel = compilation.GetSemanticModel(tree);

        var reporter = new DiagnosticReporter();
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        PredicateModel? model = analyzer.Analyze(predicateSymbol);

        if (model is null || reporter.HasErrors)
        {
            string errors = string.Join("; ", reporter.Diagnostics.Select(d => d.GetMessage()));
            throw new InvalidOperationException($"SemanticAnalyzer failed: {errors}");
        }

        int irNodeCount = CountSemanticExprNodes(model.Body);

        var lowerer = new IrLowerer(reporter);
        PlanProgram? plan = lowerer.Lower(model);

        if (plan is null || reporter.HasErrors)
        {
            string errors = string.Join("; ", reporter.Diagnostics.Select(d => d.GetMessage()));
            throw new InvalidOperationException($"IrLowerer failed: {errors}");
        }

        var optimizer = new OptimizationPipeline();
        plan = optimizer.Run(plan);

        int planInstructionCount = CountPlanInstructions(plan);

        var emitter = new Fletched.Roslyn.Emitters.PredicateEmitter(model, plan, generateLegacyNames: true);
        string generatedSource = emitter.Emit();
        int generatedLoc = generatedSource.Split('\n').Length;

        return new PerformanceBaseline(
            PredicateName: model.Name,
            IRNodeCount: irNodeCount,
            PlanInstructionCount: planInstructionCount,
            GeneratedLOC: generatedLoc);
    }

    // ── Counting helpers ─────────────────────────────────────────────────────

    /// <summary>Recursively counts all <see cref="SemanticExpr"/> nodes in a tree.</summary>
    public static int CountSemanticExprNodes(SemanticExpr expr)
    {
        return 1 + expr switch
        {
            VarExpr or ConstExpr or ListEmptyExpr => 0,
            FieldExpr f => CountSemanticExprNodes(f.Target),
            UnifyExpr u => CountSemanticExprNodes(u.Left) + CountSemanticExprNodes(u.Right),
            ConjExpr c => c.Parts.Sum(CountSemanticExprNodes),
            DisjExpr d => CountSemanticExprNodes(d.Left) + CountSemanticExprNodes(d.Right),
            ConstraintExpr c => c.Arguments.Sum(CountSemanticExprNodes),
            WithExpr w => CountSemanticExprNodes(w.Body),
            CallExpr c => c.Arguments.Sum(CountSemanticExprNodes),
            CompExpr c => CountSemanticExprNodes(c.Left) + CountSemanticExprNodes(c.Right),
            ArithExpr a => CountSemanticExprNodes(a.Left) + CountSemanticExprNodes(a.Right),
            ListConsExpr lc => CountSemanticExprNodes(lc.Head) + CountSemanticExprNodes(lc.Tail),
            _ => 0
        };
    }

    /// <summary>Counts total instructions across all blocks of a <see cref="PlanProgram"/>.</summary>
    public static int CountPlanInstructions(PlanProgram plan) =>
        new[] { plan.Entry }.Concat(plan.Blocks).Sum(b => b.Instructions.Count);

    /// <summary>Counts total blocks (entry + all other blocks) in a <see cref="PlanProgram"/>.</summary>
    public static int CountPlanBlocks(PlanProgram plan) =>
        1 + plan.Blocks.Count;

    // ── Roslyn compilation ───────────────────────────────────────────────────

    private static CSharpCompilation CreateCompilation(string source)
    {
        var references = CollectMetadataReferences();
        var syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        return CSharpCompilation.Create(
            "PerfTestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static IReadOnlyList<MetadataReference> CollectMetadataReferences()
    {
        var refs = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Include all assemblies already loaded in the current AppDomain.
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
