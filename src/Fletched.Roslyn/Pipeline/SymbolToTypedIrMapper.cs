using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Fletched.Core.IR;

namespace Fletched.Roslyn.Pipeline;

/// <summary>
/// First pipeline stage: maps a Roslyn type declaration to a <see cref="TypedSymbol"/> typed IR node.
/// Returns <see langword="null"/> when the type is not annotated with <c>[Fact]</c> or <c>[Predicate]</c>.
/// </summary>
public static class SymbolToTypedIrMapper
{
    private const string FactAttributeName = "FactAttribute";
    private const string PredicateAttributeName = "PredicateAttribute";

    /// <summary>
    /// Attempts to produce a <see cref="TypedSymbol"/> for the syntax node in <paramref name="ctx"/>.
    /// Returns <see langword="null"/> when the node is not a <c>[Fact]</c> or <c>[Predicate]</c> type.
    /// </summary>
    public static TypedSymbol? Map(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, cancellationToken) is not INamedTypeSymbol symbol)
            return null;

        TypedSymbolKind? kind = GetKind(symbol);
        if (kind is null)
            return null;

        IReadOnlyList<TypedField> fields = ExtractFields(symbol);
        return new TypedSymbol(
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString(),
            kind.Value,
            fields);
    }

    private static TypedSymbolKind? GetKind(INamedTypeSymbol symbol)
    {
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            string? name = attr.AttributeClass?.Name;
            if (name == FactAttributeName) return TypedSymbolKind.Fact;
            if (name == PredicateAttributeName) return TypedSymbolKind.Predicate;
        }

        return null;
    }

    private static IReadOnlyList<TypedField> ExtractFields(INamedTypeSymbol symbol)
    {
        List<TypedField> fields = [];

        foreach (ISymbol member in symbol.GetMembers())
        {
            if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public } prop)
                fields.Add(new TypedField(prop.Name, prop.Type.ToDisplayString()));
            else if (member is IFieldSymbol { DeclaredAccessibility: Accessibility.Public } field)
                fields.Add(new TypedField(field.Name, field.Type.ToDisplayString()));
        }

        return fields;
    }
}
