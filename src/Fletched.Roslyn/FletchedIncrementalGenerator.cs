using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Fletched.Abstractions;
using Fletched.Core.Models;
using Fletched.Emitters;
using Fletched.Features;
using Fletched.Roslyn.Mapping;

namespace Fletched.Roslyn;

[Generator]
public sealed class FletchedIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<TypeModel> typeModels = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => (INamedTypeSymbol?)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(s => s is not null)
            .Select((symbol, _) => TypeModelMapper.Map(symbol!));

        foreach (IFeatureModule feature in FeatureRegistry.All)
        {
            IncrementalValuesProvider<GenerationRequest> requests = feature.BuildPipeline(context, typeModels);
            context.RegisterSourceOutput(requests, Execute);
        }
    }

    private static void Execute(SourceProductionContext ctx, GenerationRequest request)
    {
        ICodeEmitter emitter = EmitterRegistry.Get(request.Feature);
        string source = emitter.Emit(request);

        ctx.AddSource($"{request.Target.Namespace}.{request.Target.Name}.{request.Feature}.g.cs", source);
    }
}
