using System.Linq;
using Fletched.Roslyn.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fletched.Roslyn.Pipeline;

public sealed class SourceSymbolValidator
{
    private readonly DiagnosticReporter _reporter;

    public SourceSymbolValidator(DiagnosticReporter reporter)
    {
        _reporter = reporter;
    }

    public bool ValidateFactType(INamedTypeSymbol factType)
    {
        ValidateContainingTypes(factType);
        if (HasInvalidModuleAncestor(factType))
            return false;

        if (factType is { IsRecord: true, TypeKind: TypeKind.Struct } && IsPartial(factType))
        {
            _ = FactIndexModel.GetIndexes(factType, _reporter);
            return !_reporter.HasErrors;
        }

        _reporter.Error(
            DiagnosticsCatalog.InvalidFactType,
            factType.Locations.FirstOrDefault(),
            factType.Name);
        return false;
    }

    public bool ValidatePredicateType(INamedTypeSymbol predicateType)
    {
        ValidateContainingTypes(predicateType);
        if (HasInvalidModuleAncestor(predicateType))
            return false;

        if (predicateType is { IsRecord: true, TypeKind: TypeKind.Struct } && IsPartial(predicateType))
            return !_reporter.HasErrors;

        _reporter.Error(
            DiagnosticsCatalog.InvalidPredicateType,
            predicateType.Locations.FirstOrDefault(),
            predicateType.Name);
        return false;
    }

    public void ValidateTabledPredicateOptions(INamedTypeSymbol predicateType)
    {
        AttributeData? tabledAttribute = predicateType.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.Name == "TabledAttribute");
        if (tabledAttribute is null)
            return;

        if (tabledAttribute.ConstructorArguments.Length == 0)
            return;

        TypedConstant modeArg = tabledAttribute.ConstructorArguments[0];
        if (modeArg.Value is int mode && mode == 1)
        {
            _reporter.Error(
                DiagnosticsCatalog.UnsupportedSubsumptiveTabling,
                predicateType.Locations.FirstOrDefault());
        }
    }

    public bool ValidateModuleType(INamedTypeSymbol moduleType)
    {
        ValidateContainingTypes(moduleType);

        if (moduleType.TypeKind == TypeKind.Class &&
            moduleType.IsStatic &&
            IsPartial(moduleType))
        {
            return !_reporter.HasErrors;
        }

        _reporter.Error(
            DiagnosticsCatalog.InvalidModuleType,
            moduleType.Locations.FirstOrDefault(),
            moduleType.Name);
        return false;
    }

    private void ValidateContainingTypes(INamedTypeSymbol symbol)
    {
        foreach (INamedTypeSymbol containingType in SourceSymbolHelpers.GetContainingTypes(symbol))
        {
            if (IsPartial(containingType))
                continue;

            _reporter.Error(
                DiagnosticsCatalog.InvalidContainingType,
                containingType.Locations.FirstOrDefault(),
                containingType.Name);
        }
    }

    private static bool HasInvalidModuleAncestor(INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? moduleRoot = SourceSymbolHelpers.GetModuleRoot(symbol);
        return moduleRoot is not null &&
            !(moduleRoot.TypeKind == TypeKind.Class && moduleRoot.IsStatic && IsPartial(moduleRoot));
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .All(typeDeclaration => typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }
}
