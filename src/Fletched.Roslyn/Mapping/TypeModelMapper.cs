using Microsoft.CodeAnalysis;
using Fletched.Core.Models;

namespace Fletched.Roslyn.Mapping;

/// <summary>Maps Roslyn <see cref="INamedTypeSymbol"/> instances to <see cref="TypeModel"/> records.</summary>
public static class TypeModelMapper
{
    /// <summary>Creates a <see cref="TypeModel"/> from the given named type symbol.</summary>
    public static TypeModel Map(INamedTypeSymbol symbol) =>
        new(symbol.Name, symbol.ContainingNamespace.ToDisplayString());
}
