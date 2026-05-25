using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fletched.Roslyn.Emitters;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

public enum FactIndexKindModel
{
    Equality = 0,
    Range = 1
}

internal sealed record FactIndexDeclaration(
    string Name,
    string FieldName,
    FactIndexKindModel Kind,
    ImmutableArray<string> Members,
    ImmutableArray<ISymbol> MemberSymbols,
    ImmutableArray<ITypeSymbol> MemberTypes,
    bool Unique,
    bool IsImplicit,
    int DeclarationOrder,
    Location? Location)
{
    public bool IsCompositeEquality => Kind == FactIndexKindModel.Equality && Members.Length > 1;
}

internal static class FactIndexModel
{
    public static ImmutableArray<FactIndexDeclaration> GetIndexes(
        INamedTypeSymbol factType,
        DiagnosticReporter? reporter = null)
    {
        ImmutableArray<FactIndexDeclaration> declared = GetDeclaredIndexes(factType, reporter);
        return declared.Length > 0 ? declared : GetImplicitSingleMemberIndexes(factType);
    }

    private static ImmutableArray<FactIndexDeclaration> GetDeclaredIndexes(
        INamedTypeSymbol factType,
        DiagnosticReporter? reporter)
    {
        var declarations = new List<FactIndexDeclaration>();
        var seenDeclarations = new Dictionary<string, AttributeData>(StringComparer.Ordinal);
        var seenNames = new Dictionary<string, AttributeData>(StringComparer.Ordinal);

        IEnumerable<AttributeData> attributes = factType.GetAttributes()
            .Where(attribute =>
                attribute.AttributeClass?.ToDisplayString() == "Fletched.Core.FactIndexAttribute" ||
                attribute.AttributeClass?.Name == "FactIndexAttribute");

        int order = 0;
        foreach (AttributeData attribute in attributes)
        {
            ImmutableArray<string> members = ExtractMembers(attribute);
            if (members.Length == 0)
            {
                reporter?.Error(
                    DiagnosticsCatalog.UnknownIndexMember,
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    factType.Name,
                    string.Empty,
                    "declare at least one instance field or property name",
                    "add one or more readable instance members to the FactIndex attribute");
                continue;
            }

            FactIndexKindModel kind = ExtractKind(attribute);
            string? explicitName = ExtractName(attribute);
            bool unique = ExtractUnique(attribute);

            ImmutableArray<ISymbol>.Builder memberSymbols = ImmutableArray.CreateBuilder<ISymbol>(members.Length);
            ImmutableArray<ITypeSymbol>.Builder memberTypes = ImmutableArray.CreateBuilder<ITypeSymbol>(members.Length);
            bool hasMemberError = false;
            foreach (string memberName in members)
            {
                ISymbol? member = factType.GetMembers().FirstOrDefault(symbol => string.Equals(symbol.Name, memberName, StringComparison.Ordinal));
                if (member is null)
                {
                    reporter?.Error(
                        DiagnosticsCatalog.UnknownIndexMember,
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                        factType.Name,
                        memberName,
                        "member was not found on the fact type",
                        "fix the member name or remove the declaration");
                    hasMemberError = true;
                    continue;
                }

                if (!TryGetReadableMemberType(member, out ITypeSymbol? memberType, out string invalidReason))
                {
                    reporter?.Error(
                        DiagnosticsCatalog.InvalidIndexMember,
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                        factType.Name,
                        memberName,
                        invalidReason,
                        "use a readable instance field or property");
                    hasMemberError = true;
                    continue;
                }

                memberSymbols.Add(member);
                memberTypes.Add(memberType!);
            }

            if (hasMemberError)
                continue;

            if (kind == FactIndexKindModel.Range && members.Length != 1)
            {
                reporter?.Error(
                    DiagnosticsCatalog.UnsupportedCompositeRangeIndex,
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    factType.Name,
                    string.Join(", ", members),
                    "range indexes in this milestone support exactly one member",
                    "split the declaration into single-member range indexes");
                continue;
            }

            if (kind == FactIndexKindModel.Range && !SupportsRange(memberTypes[0]))
            {
                reporter?.Error(
                    DiagnosticsCatalog.UnsupportedRangeIndexType,
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    factType.Name,
                    members[0],
                    $"{memberTypes[0].ToDisplayString()} is not comparable",
                    "use a member type that implements IComparable or remove the range declaration");
                continue;
            }

            string resolvedName = explicitName ?? BuildStableName(factType, members, kind);
            string fieldName = BuildFieldName(explicitName, members);
            string declarationKey = $"{kind}:{string.Join("|", members)}";
            if (seenDeclarations.ContainsKey(declarationKey))
            {
                reporter?.Error(
                    DiagnosticsCatalog.DuplicateIndexDeclaration,
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    factType.Name,
                    string.Join(", ", members),
                    "the same index kind and members were already declared",
                    "remove the duplicate declaration");
                continue;
            }

            if (seenNames.TryGetValue(resolvedName, out AttributeData? existingName))
            {
                reporter?.Error(
                    DiagnosticsCatalog.IndexNameCollision,
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    factType.Name,
                    resolvedName,
                    "two index declarations resolve to the same generated name",
                    "set a distinct Name value on one of the declarations");
                continue;
            }

            seenDeclarations[declarationKey] = attribute;
            seenNames[resolvedName] = attribute;
            declarations.Add(new FactIndexDeclaration(
                resolvedName,
                fieldName,
                kind,
                members,
                memberSymbols.ToImmutable(),
                memberTypes.ToImmutable(),
                unique,
                IsImplicit: false,
                DeclarationOrder: order++,
                Location: attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()));
        }

        return declarations.OrderBy(declaration => declaration.DeclarationOrder).ToImmutableArray();
    }

    private static ImmutableArray<FactIndexDeclaration> GetImplicitSingleMemberIndexes(INamedTypeSymbol factType)
    {
        int order = 0;
        return factType.GetMembers()
            .Where(member => TryGetReadableMemberType(member, out _, out _))
            .Select(member => new FactIndexDeclaration(
                BuildStableName(factType, ImmutableArray.Create(member.Name), FactIndexKindModel.Equality),
                BuildFieldName(explicitName: null, ImmutableArray.Create(member.Name)),
                FactIndexKindModel.Equality,
                ImmutableArray.Create(member.Name),
                ImmutableArray.Create(member),
                ImmutableArray.Create(GetMemberType(member)!),
                Unique: false,
                IsImplicit: true,
                DeclarationOrder: order++,
                Location: member.Locations.FirstOrDefault()))
            .ToImmutableArray();
    }

    public static string BuildStableName(INamedTypeSymbol factType, ImmutableArray<string> members, FactIndexKindModel kind)
    {
        string typePrefix = BuildTypePrefix(factType);
        string memberPart = kind == FactIndexKindModel.Range
            ? $"{members[0]}.Range"
            : string.Join("_", members);
        return $"{typePrefix}.{memberPart}";
    }

    private static string BuildTypePrefix(INamedTypeSymbol factType)
    {
        var segments = new List<string>();
        INamedTypeSymbol? moduleRoot = SourceSymbolHelpers.GetModuleRoot(factType);
        if (moduleRoot is not null)
            segments.Add(moduleRoot.Name);

        IEnumerable<INamedTypeSymbol> containers = SourceSymbolHelpers.GetContainingTypes(factType)
            .Where(type => moduleRoot is null || !SymbolEqualityComparer.Default.Equals(type, moduleRoot));

        segments.AddRange(containers.Select(type => type.Name));
        segments.Add(factType.Name);
        return string.Join(".", segments);
    }

    public static string BuildFieldName(string? explicitName, ImmutableArray<string> members)
    {
        if (!string.IsNullOrWhiteSpace(explicitName))
            return SanitizeIdentifier(explicitName!);

        return members.Length switch
        {
            0 => "ByIndex",
            1 => $"By{members[0]}",
            _ => $"By{string.Join("And", members)}"
        };
    }

    public static string GetIndexClassName(INamedTypeSymbol factType) => $"{factType.Name}FactIndexes";

    public static string GetIndexClassQualifiedName(INamedTypeSymbol factType) =>
        SourceSymbolHelpers.GetQualifiedSiblingTypeName(factType, GetIndexClassName(factType));

    private static ImmutableArray<string> ExtractMembers(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return ImmutableArray<string>.Empty;

        TypedConstant membersArg = attribute.ConstructorArguments[0];
        if (membersArg.Kind != TypedConstantKind.Array)
            return ImmutableArray<string>.Empty;

        return membersArg.Values
            .Select(value => value.Value as string)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToImmutableArray();
    }

    private static FactIndexKindModel ExtractKind(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> entry in attribute.NamedArguments)
        {
            string key = entry.Key;
            TypedConstant value = entry.Value;
            if (key == "Kind" && value.Value is int raw)
                return (FactIndexKindModel)raw;
        }

        return FactIndexKindModel.Equality;
    }

    private static string? ExtractName(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> entry in attribute.NamedArguments)
        {
            string key = entry.Key;
            TypedConstant value = entry.Value;
            if (key == "Name")
                return value.Value as string;
        }

        return null;
    }

    private static bool ExtractUnique(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> entry in attribute.NamedArguments)
        {
            string key = entry.Key;
            TypedConstant value = entry.Value;
            if (key == "Unique" && value.Value is bool unique)
                return unique;
        }

        return false;
    }

    private static bool TryGetReadableMemberType(ISymbol member, out ITypeSymbol? memberType, out string reason)
    {
        switch (member)
        {
            case IPropertySymbol property when property.IsStatic:
                memberType = null;
                reason = "static properties are not valid fact index members";
                return false;

            case IPropertySymbol { GetMethod: null }:
                memberType = null;
                reason = "write-only properties are not readable";
                return false;

            case IPropertySymbol property:
                memberType = property.Type;
                reason = string.Empty;
                return true;

            case IFieldSymbol field when field.IsStatic:
                memberType = null;
                reason = "static fields are not valid fact index members";
                return false;

            case IFieldSymbol field:
                memberType = field.Type;
                reason = string.Empty;
                return true;

            default:
                memberType = null;
                reason = "member is not a field or property";
                return false;
        }
    }

    private static ITypeSymbol? GetMemberType(ISymbol member) =>
        member switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null
        };

    private static bool SupportsRange(ITypeSymbol type)
    {
        string comparableName = "System.IComparable";
        string genericComparableName = "System.IComparable<T>";
        return type.AllInterfaces.Any(iface =>
                iface.ToDisplayString() == comparableName ||
                (iface.OriginalDefinition?.ToDisplayString() == genericComparableName))
            || type.ToDisplayString() switch
            {
                "int" or "long" or "float" or "double" or "decimal" or "string" or "System.DateTime" or "System.DateOnly" or "System.TimeOnly" => true,
                _ => false
            };
    }

    private static string SanitizeIdentifier(string value)
    {
        var chars = value.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray();
        if (chars.Length == 0)
            return "ByIndex";

        string identifier = new(chars);
        return char.IsDigit(identifier[0]) ? $"_{identifier}" : identifier;
    }
}
