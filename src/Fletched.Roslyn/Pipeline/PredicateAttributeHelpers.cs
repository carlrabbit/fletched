using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

internal static class PredicateAttributeHelpers
{
    private const string FullyQualifiedTabledAttributeName = "global::Fletched.Core.TabledAttribute";

    public static bool IsTabledPredicate(INamedTypeSymbol predicateType)
    {
        return predicateType.GetAttributes()
            .Any(attribute => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == FullyQualifiedTabledAttributeName);
    }
}
