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
            spc.AddSource(
                SourceSymbolHelpers.GetHintName(factType, "Proxy.g.cs"),
                emitter.EmitProxy());

            spc.AddSource(
                SourceSymbolHelpers.GetHintName(factType, "EngineContext.g.cs"),
                emitter.EmitEngineContextProperty());
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

            IReadOnlyList<PredicateModel> models = analyzer.AnalyzeAll(predicateType);

            // Report diagnostics
            foreach (Diagnostic d in reporter.Diagnostics)
                spc.ReportDiagnostic(d);

            if (models.Count == 0 || reporter.HasErrors) return;

            bool generateLegacyNames = models.Count == 1;

            foreach (PredicateModel model in models)
            {
                var lowerer = new IrLowerer(reporter);
                PlanProgram? plan = lowerer.Lower(model);

                foreach (Diagnostic d in reporter.Diagnostics)
                    spc.ReportDiagnostic(d);

                if (plan is null || reporter.HasErrors) return;

                var optimizer = new OptimizationPipeline();
                plan = optimizer.Run(plan);

                var predicateEmitter = new PredicateEmitter(model, plan, generateLegacyNames);
                string source = predicateEmitter.Emit();
                string hintName = generateLegacyNames
                    ? SourceSymbolHelpers.GetHintName(predicateType, "g.cs")
                    : SourceSymbolHelpers.GetHintName(predicateType, $"Arity{model.Arity}.g.cs");

                spc.AddSource(hintName, source);

                var asyncEmitter = new PredicateEmitterAsync(model, plan, generateLegacyNames);
                string asyncSource = asyncEmitter.Emit();
                string asyncHintName = generateLegacyNames
                    ? SourceSymbolHelpers.GetHintName(predicateType, "Async.g.cs")
                    : SourceSymbolHelpers.GetHintName(predicateType, $"Arity{model.Arity}.Async.g.cs");

                spc.AddSource(asyncHintName, asyncSource);
            }
        });
    }
}
