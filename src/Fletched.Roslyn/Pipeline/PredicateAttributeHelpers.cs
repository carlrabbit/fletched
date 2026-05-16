using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

internal static class PredicateAttributeHelpers
{
    private const string TabledAttributeMetadataName = "Fletched.Core.TabledAttribute";

    public static bool IsTabledPredicate(INamedTypeSymbol predicateType)
    {
        return predicateType.GetAttributes()
            .Any(attribute => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                is $"global::{TabledAttributeMetadataName}");
    }
}
