using System.Collections.Generic;
using System.Linq;
using Fletched.Roslyn.Emitters;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fletched.Roslyn;

[Generator]
public sealed class FletchedIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ── [Module] types ─────────────────────────────────────────────────
        IncrementalValuesProvider<INamedTypeSymbol> moduleTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Fletched.Core.ModuleAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Where(s => s is not null)!;

        context.RegisterSourceOutput(moduleTypes, (spc, moduleType) =>
        {
            var reporter = new DiagnosticReporter();
            var validator = new SourceSymbolValidator(reporter);
            validator.ValidateModuleType(moduleType);

            ReportDiagnostics(spc, reporter);
        });

        // ── [Fact] types ───────────────────────────────────────────────────
        IncrementalValuesProvider<INamedTypeSymbol> factTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Fletched.Core.FactAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Where(s => s is not null)!;

        context.RegisterSourceOutput(factTypes, (spc, factType) =>
        {
            var reporter = new DiagnosticReporter();
            var validator = new SourceSymbolValidator(reporter);
            validator.ValidateFactType(factType);
            ReportDiagnostics(spc, reporter);
            if (reporter.HasErrors)
                return;

            var emitter = new FactEmitter(factType);
            spc.AddSource(
                SourceSymbolHelpers.GetHintName(factType, "Proxy.g.cs"),
                emitter.EmitProxy());

            spc.AddSource(
                SourceSymbolHelpers.GetHintName(factType, "EngineContext.g.cs"),
                emitter.EmitEngineContextProperty());

            string indexesSource = emitter.EmitIndexes();
            if (!string.IsNullOrWhiteSpace(indexesSource))
            {
                spc.AddSource(
                    SourceSymbolHelpers.GetHintName(factType, "Indexes.g.cs"),
                    indexesSource);
            }
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
            var validator = new SourceSymbolValidator(reporter);
            validator.ValidatePredicateType(predicateType);
            validator.ValidateTabledPredicateOptions(predicateType);
            ReportDiagnostics(spc, reporter);
            if (reporter.HasErrors)
                return;

            var analyzer = new SemanticAnalyzer(semanticModel, reporter);

            IReadOnlyList<PredicateModel> models = analyzer.AnalyzeAll(predicateType);

            IReadOnlyList<PredicateModel> compilationModels = CollectCompilationPredicateModels(
                semanticModel.Compilation,
                predicateType,
                models);
            PredicateCallGraph callGraph = PredicateCallGraph.Create(compilationModels);
            PredicateRecursionValidator.ReportMutualNegativeCycles(callGraph, models, reporter);
            PredicateRecursionValidator.ReportUnsupportedTabledMutualRecursion(callGraph, models, reporter);

            // Report diagnostics
            foreach (Diagnostic d in reporter.Diagnostics)
                spc.ReportDiagnostic(d);

            if (models.Count == 0 || reporter.HasErrors) return;

            bool generateLegacyNames = models.Count == 1;

            foreach (PredicateModel model in models)
            {
                var lowerer = new IrLowerer(reporter);
                PlanProgram? plan = lowerer.Lower(model, callGraph);

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

    private static void ReportDiagnostics(SourceProductionContext context, DiagnosticReporter reporter)
    {
        foreach (Diagnostic diagnostic in reporter.Diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    private static IReadOnlyList<PredicateModel> CollectCompilationPredicateModels(
        Compilation compilation,
        INamedTypeSymbol currentPredicateType,
        IReadOnlyList<PredicateModel> currentModels)
    {
        var models = new List<PredicateModel>();

        foreach (INamedTypeSymbol predicateType in EnumeratePredicateTypes(compilation.GlobalNamespace))
        {
            if (SymbolEqualityComparer.Default.Equals(predicateType, currentPredicateType))
            {
                models.AddRange(currentModels);
                continue;
            }

            SyntaxReference? syntaxReference = predicateType.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference is null)
                continue;

            Microsoft.CodeAnalysis.SemanticModel semanticModel = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            var reporter = new DiagnosticReporter();
            var analyzer = new SemanticAnalyzer(semanticModel, reporter);
            models.AddRange(analyzer.AnalyzeAll(predicateType));
        }

        return models;
    }

    private static IEnumerable<INamedTypeSymbol> EnumeratePredicateTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (INamedTypeSymbol type in EnumerateTypes(namespaceSymbol))
        {
            if (type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "PredicateAttribute"))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol container)
    {
        foreach (INamedTypeSymbol type in container.GetTypeMembers())
        {
            yield return type;

            foreach (INamedTypeSymbol nestedType in EnumerateTypes(type))
                yield return nestedType;
        }

        if (container is INamespaceSymbol namespaceSymbol)
        {
            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (INamedTypeSymbol nestedType in EnumerateTypes(nestedNamespace))
                    yield return nestedType;
            }
        }
    }
}
