using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Emitters;

internal static class SourceSymbolHelpers
{
    public static string GetNamespace(ISymbol symbol)
    {
        INamespaceSymbol ns = symbol.ContainingNamespace;
        return ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }

    public static IReadOnlyList<INamedTypeSymbol> GetContainingTypes(ISymbol symbol)
    {
        var containingTypes = new List<INamedTypeSymbol>();
        for (INamedTypeSymbol? current = symbol.ContainingType; current is not null; current = current.ContainingType)
            containingTypes.Add(current);

        containingTypes.Reverse();
        return containingTypes;
    }

    public static IReadOnlyList<INamedTypeSymbol> GetContainingTypesIncludingSelf(INamedTypeSymbol symbol)
    {
        var containingTypes = GetContainingTypes(symbol).ToList();
        containingTypes.Add(symbol);
        return containingTypes;
    }

    public static INamedTypeSymbol? GetModuleRoot(ISymbol symbol)
    {
        for (INamedTypeSymbol? current = symbol as INamedTypeSymbol ?? symbol.ContainingType;
             current is not null;
             current = current.ContainingType)
        {
            if (HasAttribute(current, "Fletched.Core.ModuleAttribute", "ModuleAttribute"))
                return current;
        }

        return null;
    }

    public static string GetContextTypeName(ISymbol symbol)
    {
        INamedTypeSymbol? moduleRoot = GetModuleRoot(symbol);
        return moduleRoot is null
            ? "global::Fletched.Core.Runtime.EngineContext"
            : $"{moduleRoot.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.EngineContext";
    }

    public static string GetQualifiedSiblingTypeName(INamedTypeSymbol symbol, string siblingTypeName)
    {
        if (symbol.ContainingType is not null)
            return $"{symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{siblingTypeName}";

        string ns = GetNamespace(symbol);
        return string.IsNullOrEmpty(ns)
            ? $"global::{siblingTypeName}"
            : $"global::{ns}.{siblingTypeName}";
    }

    public static string GetHintName(ISymbol symbol, string suffix)
    {
        string display = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = new string(display
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '_')
            .ToArray());

        return $"{sanitized}_{suffix}";
    }

    public static string GetTypeDeclaration(INamedTypeSymbol type)
    {
        var modifiers = new List<string>();
        string accessibility = type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => string.Empty,
        };

        if (!string.IsNullOrEmpty(accessibility))
            modifiers.Add(accessibility);

        if (type.IsStatic)
        {
            modifiers.Add("static");
            modifiers.Add("partial");
            modifiers.Add("class");
            return $"{string.Join(" ", modifiers)} {type.Name}{GetTypeParameterList(type)}";
        }

        if (type.IsAbstract && type.TypeKind == TypeKind.Class)
            modifiers.Add("abstract");

        if (type.IsSealed && type.TypeKind == TypeKind.Class)
            modifiers.Add("sealed");

        modifiers.Add("partial");

        if (type.IsRecord)
        {
            modifiers.Add("record");
            modifiers.Add(type.TypeKind == TypeKind.Struct ? "struct" : "class");
        }
        else
        {
            modifiers.Add(type.TypeKind switch
            {
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                _ => "class",
            });
        }

        return $"{string.Join(" ", modifiers)} {type.Name}{GetTypeParameterList(type)}";
    }

    public static void OpenDeclarationScope(EmitContext ctx, ISymbol symbol)
    {
        string ns = GetNamespace(symbol);
        if (!string.IsNullOrEmpty(ns))
        {
            ctx.AppendLine($"namespace {ns};");
            ctx.AppendLine();
        }

        foreach (INamedTypeSymbol containingType in GetContainingTypes(symbol))
        {
            ctx.AppendLine(GetTypeDeclaration(containingType));
            ctx.AppendLine("{");
            ctx.IndentLevel++;
        }
    }

    public static void CloseDeclarationScope(EmitContext ctx, ISymbol symbol)
    {
        foreach (INamedTypeSymbol _ in GetContainingTypes(symbol).Reverse())
        {
            ctx.IndentLevel--;
            ctx.AppendLine("}");
        }
    }

    public static bool HasAttribute(ISymbol symbol, string fullyQualifiedMetadataName, string shortName)
    {
        return symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == fullyQualifiedMetadataName ||
            attribute.AttributeClass?.Name == shortName);
    }

    private static string GetTypeParameterList(INamedTypeSymbol type)
    {
        return type.TypeParameters.Length == 0
            ? string.Empty
            : $"<{string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name))}>";
    }
}
