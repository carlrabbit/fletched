using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

internal static class PredicateAttributeHelpers
{
    private const string FullyQualifiedTabledAttributeName = "global::Fletched.Core.TabledAttribute";

    /// <summary>
    /// Determines whether a predicate has the canonical <c>[Tabled]</c> marker.
    /// Uses fully-qualified symbol display comparison to avoid namespace ambiguity and keep
    /// consistent detection behavior across semantic and call-graph validation paths.
    /// </summary>
    public static bool IsTabledPredicate(INamedTypeSymbol predicateType)
    {
        return predicateType.GetAttributes()
            .Any(attribute => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == FullyQualifiedTabledAttributeName);
    }
}
