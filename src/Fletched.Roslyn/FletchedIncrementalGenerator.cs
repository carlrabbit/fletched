using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Fletched.Roslyn.Pipeline;
using Fletched.Roslyn.Emitters;

namespace Fletched.Roslyn;

[Generator]
public sealed class FletchedIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ── [Fact] types ───────────────────────────────────────────────────
        IncrementalValuesProvider<INamedTypeSymbol> factTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Fletched.Core.FactAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Where(s => s is not null)!;

        context.RegisterSourceOutput(factTypes, (spc, factType) =>
        {
            var emitter = new FactEmitter(factType);
            string ns = factType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : factType.ContainingNamespace.ToDisplayString();

            spc.AddSource(
                $"{factType.Name}_Proxy.g.cs",
                emitter.EmitProxy(ns));

            spc.AddSource(
                $"{factType.Name}_EngineContext.g.cs",
                emitter.EmitEngineContextProperty(ns));
        });

        // ── [Predicate] types ─────────────────────────────────────────────
        IncrementalValuesProvider<INamedTypeSymbol> predicateTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Fletched.Core.PredicateAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Where(s => s is not null)!;

        IncrementalValuesProvider<(INamedTypeSymbol Type, Microsoft.CodeAnalysis.SemanticModel SemanticModel)>
            predicatesWithModel = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Fletched.Core.PredicateAttribute",
                    predicate: static (node, _) => node is TypeDeclarationSyntax,
                    transform: static (ctx, _) => ((INamedTypeSymbol)ctx.TargetSymbol, ctx.SemanticModel))
                .Where(x => x.Item1 is not null)!;

        context.RegisterSourceOutput(predicatesWithModel, (spc, pair) =>
        {
            (INamedTypeSymbol predicateType, Microsoft.CodeAnalysis.SemanticModel semanticModel) = pair;

            var reporter = new DiagnosticReporter();
            var analyzer = new SemanticAnalyzer(semanticModel, reporter);

            PredicateModel? model = analyzer.Analyze(predicateType);

            // Report diagnostics
            foreach (Diagnostic d in reporter.Diagnostics)
                spc.ReportDiagnostic(d);

            if (model is null || reporter.HasErrors) return;

            var lowerer = new IrLowerer(reporter);
            PlanProgram? plan = lowerer.Lower(model);

            foreach (Diagnostic d in reporter.Diagnostics)
                spc.ReportDiagnostic(d);

            if (plan is null || reporter.HasErrors) return;

            var optimizer = new OptimizationPipeline();
            plan = optimizer.Run(plan);

            string ns = predicateType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : predicateType.ContainingNamespace.ToDisplayString();

            var predicateEmitter = new PredicateEmitter(model, plan);
            string source = predicateEmitter.Emit(ns);

            spc.AddSource($"{predicateType.Name}.g.cs", source);
        });
    }
}
