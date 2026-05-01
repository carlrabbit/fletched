using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Fletched.Core.IR;
using Fletched.Emitters;
using Fletched.Roslyn.Pipeline;

namespace Fletched.Roslyn;

[Generator]
public sealed class FletchedIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Stage 1: Collect types marked with [Fact] or [Predicate] and map to Typed IR.
        IncrementalValuesProvider<TypedSymbol> typedSymbols = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, ct) => SymbolToTypedIrMapper.Map(ctx, ct))
            .Where(s => s is not null)
            .Select((s, _) => s!);

        // Stage 2: Transform Typed IR to Planned IR.
        IncrementalValuesProvider<(TypedSymbol Typed, PlanProgram Plan)> plannedTypes = typedSymbols
            .Select(static (typed, _) => (Typed: typed, Plan: TypedIrToPlanIrMapper.Map(typed)));

        // Stage 3: Transform Planned IR to code using IEmitter implementations.
        context.RegisterSourceOutput(plannedTypes, Execute);
    }

    private static void Execute(SourceProductionContext ctx, (TypedSymbol Typed, PlanProgram Plan) pair)
    {
        IEmitter emitter = pair.Typed.Kind switch
        {
            TypedSymbolKind.Fact => new FactAccessEmitter(),
            TypedSymbolKind.Predicate => new StateEmitter(),
            _ => throw new InvalidOperationException($"Unknown symbol kind: {pair.Typed.Kind}")
        };

        foreach (PlanBlock block in pair.Plan.Blocks)
        {
            string source = emitter.Emit(block);
            if (!string.IsNullOrWhiteSpace(source))
                ctx.AddSource($"{pair.Typed.Namespace}.{pair.Typed.Name}.{block.Label}.g.cs", source);
        }
    }
}

