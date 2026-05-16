using System;
using System.Collections.Generic;
using System.Linq;
using Fletched.Roslyn.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fletched.Roslyn.Pipeline;

/// <summary>
/// Traverses the Roslyn syntax/semantic tree for a [PredicateBody] method
/// and builds a <see cref="PredicateModel"/>.
/// </summary>
public sealed class SemanticAnalyzer
{
    private readonly Microsoft.CodeAnalysis.SemanticModel _semanticModel;
    private readonly DiagnosticReporter _reporter;
    private readonly Dictionary<string, VariableSymbol> _scope = new();
    private readonly HashSet<string> _scopedVariableNames = new(StringComparer.Ordinal);
    private INamedTypeSymbol? _currentPredicateType;
    private int _currentPredicateArity;

    public SemanticAnalyzer(
        Microsoft.CodeAnalysis.SemanticModel semanticModel,
        DiagnosticReporter reporter)
    {
        _semanticModel = semanticModel;
        _reporter = reporter;
    }

    public PredicateModel? Analyze(INamedTypeSymbol predicateType)
    {
        return AnalyzeAll(predicateType).FirstOrDefault();
    }

    public IReadOnlyList<PredicateModel> AnalyzeAll(INamedTypeSymbol predicateType)
    {
        List<IMethodSymbol> bodyMethods = GetPredicateBodyMethods(predicateType);
        if (bodyMethods.Count == 0)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody,
                predicateType.Locations.FirstOrDefault(),
                "No method marked with [PredicateBody] found");
            return [];
        }

        foreach (IGrouping<int, IMethodSymbol> overloadGroup in bodyMethods.GroupBy(m => m.Parameters.Length))
        {
            if (!overloadGroup.Skip(1).Any()) continue;

            foreach (IMethodSymbol duplicateBody in overloadGroup)
            {
                _reporter.Error(
                    DiagnosticsCatalog.InvalidPredicateBody,
                    duplicateBody.Locations.FirstOrDefault(),
                    $"Multiple [PredicateBody] methods with arity {overloadGroup.Key} are not allowed");
            }
        }

        var models = new List<PredicateModel>();
        foreach (IMethodSymbol bodyMethod in bodyMethods.OrderBy(m => m.Parameters.Length))
        {
            _scope.Clear();
            _scopedVariableNames.Clear();
            PredicateModel? model = AnalyzeBody(predicateType, bodyMethod);
            if (model is not null)
                models.Add(model);
        }

        return models;
    }

    private PredicateModel? AnalyzeBody(INamedTypeSymbol predicateType, IMethodSymbol bodyMethod)
    {
        _currentPredicateType = predicateType;
        _currentPredicateArity = bodyMethod.Parameters.Length;

        // Validate return type is LogicExpr<bool>
        if (!IsLogicExprBool(bodyMethod.ReturnType))
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody,
                bodyMethod.Locations.FirstOrDefault(),
                "[PredicateBody] method must return LogicExpr<bool>");
            return null;
        }

        // Build parameters: TerminalVar<T> → VariableKind.Terminal
        var parameters = new List<VariableSymbol>();
        foreach (IParameterSymbol param in bodyMethod.Parameters)
        {
            ITypeSymbol paramType = param.Type;
            if (paramType is INamedTypeSymbol named &&
                named.Name == "TerminalVar" &&
                named.TypeArguments.Length == 1)
            {
                ITypeSymbol innerType = named.TypeArguments[0];
                var sym = new VariableSymbol(param.Name, innerType, VariableKind.Terminal);
                parameters.Add(sym);
                _scope[param.Name] = sym;
            }
            else
            {
                _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody,
                    param.Locations.FirstOrDefault(),
                    $"Parameter '{param.Name}' must be TerminalVar<T>");
            }
        }

        if (_reporter.HasErrors) return null;

        // Get the method syntax and analyze the body expression
        SyntaxReference? syntaxRef = bodyMethod.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef is null)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody,
                bodyMethod.Locations.FirstOrDefault(),
                "Could not obtain syntax for predicate body");
            return null;
        }

        ExpressionSyntax? bodyExpr = GetBodyExpression(syntaxRef.GetSyntax());
        if (bodyExpr is null)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody,
                bodyMethod.Locations.FirstOrDefault(),
                "Predicate body must be a single expression");
            return null;
        }

        ITypeSymbol boolType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);
        SemanticExpr? body = AnalyzeExpr(bodyExpr, boolType);
        if (body is null) return null;

        if (body is NotExpr standaloneNot)
            ValidateNegationGrounding([standaloneNot], bodyExpr.GetLocation());

        return new PredicateModel(predicateType.Name, predicateType, bodyMethod, parameters, body);
    }

    private static List<IMethodSymbol> GetPredicateBodyMethods(INamedTypeSymbol predicateType)
    {
        return predicateType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.Name == "PredicateBodyAttribute"))
            .ToList();
    }

    private static ExpressionSyntax? GetBodyExpression(SyntaxNode node)
    {
        return node switch
        {
            MethodDeclarationSyntax m when m.ExpressionBody is not null =>
                m.ExpressionBody.Expression,
            MethodDeclarationSyntax m when m.Body?.Statements.Count == 1 &&
                m.Body.Statements[0] is ReturnStatementSyntax ret =>
                ret.Expression,
            _ => null
        };
    }

    private SemanticExpr? AnalyzeExpr(ExpressionSyntax syntax, ITypeSymbol? expectedType)
    {
        // Resolve compile-time constants early (handles enum member access like Person.A,
        // qualified names like ClaimKind.Friend, and other constant expressions).
        // LiteralExpressionSyntax is excluded because it is handled explicitly below and
        // the constant-value path would produce identical results.
        if (syntax is not LiteralExpressionSyntax)
        {
            Microsoft.CodeAnalysis.Optional<object?> maybeConst = _semanticModel.GetConstantValue(syntax);
            if (maybeConst.HasValue)
            {
                TypeInfo typeInfo = _semanticModel.GetTypeInfo(syntax);
                ITypeSymbol constType = typeInfo.Type
                    ?? _semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
                return new ConstExpr(maybeConst.Value, constType);
            }
        }

        switch (syntax)
        {
            case BinaryExpressionSyntax bin:
                return AnalyzeBinary(bin);

            case InvocationExpressionSyntax inv:
                return AnalyzeInvocation(inv);

            case MemberAccessExpressionSyntax mem:
                return AnalyzeMemberAccess(mem);

            case IdentifierNameSyntax ident:
                return AnalyzeIdentifier(ident);

            case LiteralExpressionSyntax lit:
                return AnalyzeLiteral(lit);

            case ParenthesizedExpressionSyntax paren:
                return AnalyzeExpr(paren.Expression, expectedType);

            case CastExpressionSyntax cast:
                // Allow implicit cast — DSL uses implicit operator from T to LogicExpr<T>
                return AnalyzeExpr(cast.Expression, expectedType);

            default:
                _reporter.Error(DiagnosticsCatalog.UnsupportedExpression,
                    syntax.GetLocation(),
                    syntax.Kind().ToString());
                return null;
        }
    }

    private SemanticExpr? AnalyzeBinary(BinaryExpressionSyntax bin)
    {
        ITypeSymbol boolType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);

        switch (bin.Kind())
        {
            case SyntaxKind.EqualsExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, null);
                SemanticExpr? right = AnalyzeExpr(bin.Right, null);
                if (left is null || right is null) return null;
                return new UnifyExpr(left, right);
            }

            case SyntaxKind.LogicalAndExpression:
            {
                // Flatten conjunctions
                SemanticExpr? left = AnalyzeExpr(bin.Left, boolType);
                SemanticExpr? right = AnalyzeExpr(bin.Right, boolType);
                if (left is null || right is null) return null;
                var parts = new List<SemanticExpr>();
                FlattenConj(left, parts);
                FlattenConj(right, parts);
                ValidateNegationGrounding(parts, bin.GetLocation());
                return new ConjExpr(parts, boolType);
            }

            case SyntaxKind.LogicalOrExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, boolType);
                SemanticExpr? right = AnalyzeExpr(bin.Right, boolType);
                if (left is null || right is null) return null;
                return new DisjExpr(left, right, boolType);
            }

            // Handle & and | operators (used when DSL uses && and || through true/false overloads)
            case SyntaxKind.BitwiseAndExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, boolType);
                SemanticExpr? right = AnalyzeExpr(bin.Right, boolType);
                if (left is null || right is null) return null;
                var parts = new List<SemanticExpr>();
                FlattenConj(left, parts);
                FlattenConj(right, parts);
                ValidateNegationGrounding(parts, bin.GetLocation());
                return new ConjExpr(parts, boolType);
            }

            case SyntaxKind.BitwiseOrExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, boolType);
                SemanticExpr? right = AnalyzeExpr(bin.Right, boolType);
                if (left is null || right is null) return null;
                return new DisjExpr(left, right, boolType);
            }

            // ── Comparison operators ───────────────────────────────────────────
            case SyntaxKind.NotEqualsExpression:
            case SyntaxKind.LessThanExpression:
            case SyntaxKind.GreaterThanExpression:
            case SyntaxKind.LessThanOrEqualExpression:
            case SyntaxKind.GreaterThanOrEqualExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, null);
                SemanticExpr? right = AnalyzeExpr(bin.Right, null);
                if (left is null || right is null) return null;
                CompOp op = bin.Kind() switch
                {
                    SyntaxKind.NotEqualsExpression => CompOp.NotEqual,
                    SyntaxKind.LessThanExpression => CompOp.LessThan,
                    SyntaxKind.GreaterThanExpression => CompOp.GreaterThan,
                    SyntaxKind.LessThanOrEqualExpression => CompOp.LessThanOrEqual,
                    _ => CompOp.GreaterThanOrEqual,
                };
                return new CompExpr(op, left, right, boolType);
            }

            // ── Arithmetic operators ───────────────────────────────────────────
            case SyntaxKind.AddExpression:
            case SyntaxKind.SubtractExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, null);
                SemanticExpr? right = AnalyzeExpr(bin.Right, null);
                if (left is null || right is null) return null;
                ArithOp op = bin.Kind() == SyntaxKind.AddExpression ? ArithOp.Add : ArithOp.Subtract;
                return new ArithExpr(op, left, right);
            }

            default:
                _reporter.Error(DiagnosticsCatalog.UnsupportedExpression,
                    bin.GetLocation(),
                    bin.Kind().ToString());
                return null;
        }
    }

    private static void FlattenConj(SemanticExpr expr, List<SemanticExpr> parts)
    {
        if (expr is ConjExpr conj)
            foreach (SemanticExpr part in conj.Parts)
                FlattenConj(part, parts);
        else
            parts.Add(expr);
    }

    private void ValidateNegationGrounding(IReadOnlyList<SemanticExpr> parts, Location location)
    {
        var grounded = new HashSet<VariableSymbol>(_scope.Values.Where(v => v.Kind == VariableKind.Source));

        for (int i = 0; i < parts.Count; i++)
        {
            SemanticExpr part = parts[i];
            switch (part)
            {
                case UnifyExpr unify:
                {
                    bool leftGround = IsGround(unify.Left, grounded);
                    bool rightGround = IsGround(unify.Right, grounded);

                    if (leftGround)
                        AddVars(unify.Right, grounded);
                    if (rightGround)
                        AddVars(unify.Left, grounded);
                    break;
                }

                case CallExpr call:
                {
                    foreach (SemanticExpr arg in call.Arguments)
                        AddVars(arg, grounded);
                    break;
                }

                case NotExpr not:
                {
                    var notVariables = new HashSet<VariableSymbol>(CollectVariables(not.Goal));
                    var ungroundedInNot = new HashSet<VariableSymbol>();
                    foreach (VariableSymbol variable in notVariables)
                    {
                        if (!grounded.Contains(variable))
                        {
                            _reporter.Error(DiagnosticsCatalog.UngroundedNegation, location, variable.Name);
                            ungroundedInNot.Add(variable);
                        }
                    }

                    var variablesUsedAfterNot = new HashSet<VariableSymbol>();
                    for (int j = i + 1; j < parts.Count; j++)
                    {
                        AddVars(parts[j], variablesUsedAfterNot);
                    }

                    foreach (VariableSymbol variable in ungroundedInNot)
                    {
                        if (variablesUsedAfterNot.Contains(variable))
                            _reporter.Error(DiagnosticsCatalog.NegationVariableEscape, location, variable.Name);
                    }

                    ValidateNegationGoal(not.Goal, grounded, location);
                    break;
                }
            }
        }
    }

    private void ValidateNegationGoal(SemanticExpr goal, ISet<VariableSymbol> groundedAtNegationEntry, Location location)
    {
        foreach (CallExpr call in CollectCalls(goal))
        {
            if (_currentPredicateType is not null &&
                SymbolEqualityComparer.Default.Equals(call.PredicateType, _currentPredicateType) &&
                call.Arity == _currentPredicateArity)
            {
                string predicateName = $"{_currentPredicateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}/{_currentPredicateArity}";
                DiagnosticDescriptor descriptor = IsTabledPredicate(_currentPredicateType)
                    ? DiagnosticsCatalog.InvalidTabledNegationCycle
                    : DiagnosticsCatalog.UnsupportedRecursiveNegation;
                _reporter.Error(
                    descriptor,
                    location,
                    $": {predicateName} -not-> {predicateName}");
            }

            bool hasUngroundedInvocationArgument = call.Arguments.Any(arg => !IsGround(arg, groundedAtNegationEntry));
            if (hasUngroundedInvocationArgument)
            {
                _reporter.Error(DiagnosticsCatalog.UnsupportedInvocationPatternInNegation, location);
            }
        }
    }

    private static IEnumerable<CallExpr> CollectCalls(SemanticExpr expr)
    {
        switch (expr)
        {
            case CallExpr call:
                yield return call;
                foreach (SemanticExpr arg in call.Arguments)
                    foreach (CallExpr nested in CollectCalls(arg))
                        yield return nested;
                yield break;

            case FieldExpr fieldExpr:
                foreach (CallExpr nested in CollectCalls(fieldExpr.Target))
                    yield return nested;
                yield break;

            case UnifyExpr unifyExpr:
                foreach (CallExpr nested in CollectCalls(unifyExpr.Left))
                    yield return nested;
                foreach (CallExpr nested in CollectCalls(unifyExpr.Right))
                    yield return nested;
                yield break;

            case CompExpr compExpr:
                foreach (CallExpr nested in CollectCalls(compExpr.Left))
                    yield return nested;
                foreach (CallExpr nested in CollectCalls(compExpr.Right))
                    yield return nested;
                yield break;

            case ArithExpr arithExpr:
                foreach (CallExpr nested in CollectCalls(arithExpr.Left))
                    yield return nested;
                foreach (CallExpr nested in CollectCalls(arithExpr.Right))
                    yield return nested;
                yield break;

            case ConstraintExpr constraintExpr:
                foreach (SemanticExpr argument in constraintExpr.Arguments)
                    foreach (CallExpr nested in CollectCalls(argument))
                        yield return nested;
                yield break;

            case ConjExpr conjExpr:
                foreach (SemanticExpr part in conjExpr.Parts)
                    foreach (CallExpr nested in CollectCalls(part))
                        yield return nested;
                yield break;

            case DisjExpr disjExpr:
                foreach (CallExpr nested in CollectCalls(disjExpr.Left))
                    yield return nested;
                foreach (CallExpr nested in CollectCalls(disjExpr.Right))
                    yield return nested;
                yield break;

            case WithExpr withExpr:
                foreach (CallExpr nested in CollectCalls(withExpr.Body))
                    yield return nested;
                yield break;

            case NotExpr notExpr:
                foreach (CallExpr nested in CollectCalls(notExpr.Goal))
                    yield return nested;
                yield break;

            case ListConsExpr listCons:
                foreach (CallExpr nested in CollectCalls(listCons.Head))
                    yield return nested;
                foreach (CallExpr nested in CollectCalls(listCons.Tail))
                    yield return nested;
                yield break;
        }
    }

    private static bool IsTabledPredicate(INamedTypeSymbol predicateType)
    {
        return predicateType.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "TabledAttribute");
    }

    private static bool IsGround(SemanticExpr expr, ISet<VariableSymbol> grounded)
    {
        return expr switch
        {
            ConstExpr => true,
            VarExpr varExpr => grounded.Contains(varExpr.Variable),
            FieldExpr fieldExpr => IsGround(fieldExpr.Target, grounded),
            ArithExpr arithExpr => IsGround(arithExpr.Left, grounded) && IsGround(arithExpr.Right, grounded),
            ListEmptyExpr => true,
            ListConsExpr listCons => IsGround(listCons.Head, grounded) && IsGround(listCons.Tail, grounded),
            _ => false,
        };
    }

    private static void AddVars(SemanticExpr expr, ISet<VariableSymbol> grounded)
    {
        foreach (VariableSymbol variable in CollectVariables(expr))
            grounded.Add(variable);
    }

    private static IEnumerable<VariableSymbol> CollectVariables(SemanticExpr expr)
    {
        switch (expr)
        {
            case VarExpr varExpr:
                yield return varExpr.Variable;
                yield break;

            case FieldExpr fieldExpr:
                foreach (VariableSymbol variable in CollectVariables(fieldExpr.Target))
                    yield return variable;
                yield break;

            case UnifyExpr unifyExpr:
                foreach (VariableSymbol variable in CollectVariables(unifyExpr.Left))
                    yield return variable;
                foreach (VariableSymbol variable in CollectVariables(unifyExpr.Right))
                    yield return variable;
                yield break;

            case CompExpr compExpr:
                foreach (VariableSymbol variable in CollectVariables(compExpr.Left))
                    yield return variable;
                foreach (VariableSymbol variable in CollectVariables(compExpr.Right))
                    yield return variable;
                yield break;

            case ArithExpr arithExpr:
                foreach (VariableSymbol variable in CollectVariables(arithExpr.Left))
                    yield return variable;
                foreach (VariableSymbol variable in CollectVariables(arithExpr.Right))
                    yield return variable;
                yield break;

            case ConstraintExpr constraintExpr:
                foreach (SemanticExpr argument in constraintExpr.Arguments)
                    foreach (VariableSymbol variable in CollectVariables(argument))
                        yield return variable;
                yield break;

            case ConjExpr conjExpr:
                foreach (SemanticExpr part in conjExpr.Parts)
                    foreach (VariableSymbol variable in CollectVariables(part))
                        yield return variable;
                yield break;

            case DisjExpr disjExpr:
                foreach (VariableSymbol variable in CollectVariables(disjExpr.Left))
                    yield return variable;
                foreach (VariableSymbol variable in CollectVariables(disjExpr.Right))
                    yield return variable;
                yield break;

            case CallExpr callExpr:
                foreach (SemanticExpr arg in callExpr.Arguments)
                    foreach (VariableSymbol variable in CollectVariables(arg))
                        yield return variable;
                yield break;

            case NotExpr notExpr:
                foreach (VariableSymbol variable in CollectVariables(notExpr.Goal))
                    yield return variable;
                yield break;

            case ListConsExpr listCons:
                foreach (VariableSymbol variable in CollectVariables(listCons.Head))
                    yield return variable;
                foreach (VariableSymbol variable in CollectVariables(listCons.Tail))
                    yield return variable;
                yield break;
        }
    }

    private SemanticExpr? AnalyzeInvocation(InvocationExpressionSyntax inv)
    {
        ITypeSymbol boolType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);

        SymbolInfo symbolInfo = _semanticModel.GetSymbolInfo(inv);
        ISymbol? symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

        if (symbol is IMethodSymbol method)
        {
            // Check if it's Logic.With<T...>
            if (method.Name == "With" &&
                (method.ContainingType?.Name == "Logic" || method.ContainingType?.Name == "LogicExpr"))
            {
                return AnalyzeWith(inv, method);
            }

            // Check if it's Logic.Not(goal)
            if (method.Name == "Not" && method.ContainingType?.Name == "Logic")
            {
                return AnalyzeNot(inv);
            }

            // Check if it's Logic.Empty<T>()
            if (method.Name == "Empty" && method.ContainingType?.Name == "Logic")
            {
                return AnalyzeListEmpty(method);
            }

            // Check if it's Logic.List<T>(...)
            if (method.Name == "List" && method.ContainingType?.Name == "Logic")
            {
                return AnalyzeList(inv, method);
            }

            // Check if it's Logic.Cons<T>(head, tail)
            if (method.Name == "Cons" && method.ContainingType?.Name == "Logic")
            {
                return AnalyzeListCons(inv, method);
            }
        }

        // Check if the invocation target is itself a [Predicate]-annotated type
        // Pattern: SomePredicate(terminalVarArg) — recognized as a call expression
        ITypeSymbol? calleeType = ResolvePredicateCallTarget(inv);
        if (calleeType is INamedTypeSymbol predicateType)
        {
            var args = new List<SemanticExpr>();
            foreach (ArgumentSyntax arg in inv.ArgumentList.Arguments)
            {
                SemanticExpr? argExpr = AnalyzeExpr(arg.Expression, null);
                if (argExpr is null) return null;
                args.Add(argExpr);
            }
            int arity = args.Count;
            if (!HasPredicateBodyForArity(predicateType, arity))
            {
                _reporter.Error(
                    DiagnosticsCatalog.InvalidPredicateCall,
                    inv.GetLocation(),
                    $"Predicate '{predicateType.Name}/{arity}' was not found or argument types do not match");
                return null;
            }

            return new CallExpr(predicateType, args, boolType, arity);
        }

        if (symbol is IMethodSymbol methodSymbol)
        {
            // Otherwise treat as constraint
            var args = new List<SemanticExpr>();
            // For member invocations, the receiver is arg[0]
            if (inv.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                SemanticExpr? receiver = AnalyzeExpr(memberAccess.Expression, null);
                if (receiver is null) return null;
                args.Add(receiver);
            }

            foreach (ArgumentSyntax arg in inv.ArgumentList.Arguments)
            {
                SemanticExpr? argExpr = AnalyzeExpr(arg.Expression, null);
                if (argExpr is null) return null;
                args.Add(argExpr);
            }

            return new ConstraintExpr(methodSymbol, args, boolType);
        }

        // Try syntactic fallback for Logic.With<T>
        if (inv.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.Text: "With" } })
        {
            return AnalyzeWithSyntactic(inv);
        }

        if (inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "List" })
        {
            _reporter.Error(DiagnosticsCatalog.InvalidListExpression,
                inv.GetLocation(),
                "Logic.List(...) could not be resolved. Ensure all list elements share the same type.");
            return null;
        }

        _reporter.Error(DiagnosticsCatalog.UnsupportedExpression,
            inv.GetLocation(),
            "Unresolved invocation");
        return null;
    }

    private ITypeSymbol? ResolvePredicateCallTarget(InvocationExpressionSyntax inv)
    {
        // Handle: SomePredicate(...) — identifier or member access resolving to a [Predicate] type
        ITypeSymbol? candidate = null;

        if (inv.Expression is IdentifierNameSyntax ident)
        {
            SymbolInfo si = _semanticModel.GetSymbolInfo(ident);
            candidate = ResolveTypeCandidate(si) ?? ResolvePredicateTypeByName(ident.Identifier.ValueText, ident.SpanStart);
        }
        else if (inv.Expression is MemberAccessExpressionSyntax mem)
        {
            SymbolInfo si = _semanticModel.GetSymbolInfo(mem);
            candidate = ResolveTypeCandidate(si) ?? ResolveMemberPredicateType(mem);
        }

        // Also try to resolve via the semantic model as a type symbol directly
        if (candidate is null)
        {
            SymbolInfo si = _semanticModel.GetSymbolInfo(inv.Expression);
            candidate = ResolveTypeCandidate(si);
        }

        if (candidate is not INamedTypeSymbol namedCandidate) return null;

        bool hasPredicate = namedCandidate.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "PredicateAttribute");
        return hasPredicate ? candidate : null;
    }

    private INamedTypeSymbol? ResolvePredicateTypeByName(string name, int position)
    {
        INamedTypeSymbol[] matches = _semanticModel.LookupNamespacesAndTypes(position, name: name)
            .OfType<INamedTypeSymbol>()
            .Where(type => type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "PredicateAttribute"))
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private INamedTypeSymbol? ResolveMemberPredicateType(MemberAccessExpressionSyntax memberAccess)
    {
        if (_semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol is not INamespaceOrTypeSymbol container)
            return null;

        INamedTypeSymbol[] matches = container.GetTypeMembers(memberAccess.Name.Identifier.ValueText)
            .Where(type => type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "PredicateAttribute"))
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static ITypeSymbol? ResolveTypeCandidate(SymbolInfo symbolInfo)
    {
        if (symbolInfo.Symbol is INamedTypeSymbol namedType)
            return namedType;

        if (symbolInfo.Symbol is IMethodSymbol method && method.MethodKind == MethodKind.Constructor)
            return method.ContainingType;

        if (symbolInfo.CandidateSymbols.OfType<INamedTypeSymbol>().FirstOrDefault() is { } candidateType)
            return candidateType;

        if (symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor) is { } candidateCtor)
            return candidateCtor.ContainingType;

        return null;
    }

    private static bool HasPredicateBodyForArity(INamedTypeSymbol predicateType, int arity)
    {
        return GetPredicateBodyMethods(predicateType)
            .Any(m => m.Parameters.Length == arity);
    }

    private SemanticExpr? AnalyzeWith(InvocationExpressionSyntax inv, IMethodSymbol method)
    {
        ITypeSymbol boolType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);

        if (inv.ArgumentList.Arguments.Count < 1 ||
            inv.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "With<T> requires a lambda argument");
            return null;
        }

        // Get the fact type arguments
        IReadOnlyList<ITypeSymbol> typeArgs = method.TypeArguments;
        if (typeArgs.Count == 0)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "With<T> requires at least one type argument");
            return null;
        }

        return BuildWithExpr(lambda, typeArgs, boolType);
    }

    private SemanticExpr? AnalyzeWithSyntactic(InvocationExpressionSyntax inv)
    {
        ITypeSymbol boolType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);

        // Extract type arguments from syntax
        var typeArgSyntaxes = new List<TypeSyntax>();
        if (inv.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax gn })
            typeArgSyntaxes.AddRange(gn.TypeArgumentList.Arguments);

        var typeArgs = new List<ITypeSymbol>();
        foreach (TypeSyntax ts in typeArgSyntaxes)
        {
            TypeInfo ti = _semanticModel.GetTypeInfo(ts);
            if (ti.Type is null)
            {
                _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, ts.GetLocation(),
                    "Could not resolve type argument");
                return null;
            }
            typeArgs.Add(ti.Type);
        }

        if (typeArgs.Count == 0)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "With<T> requires at least one type argument");
            return null;
        }

        if (inv.ArgumentList.Arguments.Count < 1 ||
            inv.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "With<T> requires a lambda argument");
            return null;
        }

        return BuildWithExpr(lambda, typeArgs, boolType);
    }

    private SemanticExpr? BuildWithExpr(
        LambdaExpressionSyntax lambda,
        IReadOnlyList<ITypeSymbol> typeArgs,
        ITypeSymbol boolType)
    {
        // Get parameter names from lambda
        var paramNames = GetLambdaParameterNames(lambda);
        if (paramNames.Count != typeArgs.Count)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, lambda.GetLocation(),
                $"Lambda has {paramNames.Count} parameters but With<T> has {typeArgs.Count} type arguments");
            return null;
        }

        var newScope = new Dictionary<string, VariableSymbol>(_scope);
        var variables = new List<VariableSymbol>();

        for (int i = 0; i < paramNames.Count; i++)
        {
            string name = paramNames[i];
            if (newScope.ContainsKey(name))
            {
                _reporter.Error(DiagnosticsCatalog.DuplicateVariable, lambda.GetLocation(), name);
                return null;
            }
            VariableKind variableKind = ClassifyWithVariableKind(typeArgs[i], lambda.GetLocation());
            if (_reporter.HasErrors)
                return null;

            var v = new VariableSymbol(name, typeArgs[i], variableKind);
            variables.Add(v);
            newScope[name] = v;
            _scopedVariableNames.Add(name);
        }

        // Analyze lambda body with extended scope
        var inner = new SemanticAnalyzer(_semanticModel, _reporter);
        inner._currentPredicateType = _currentPredicateType;
        foreach (var kv in newScope) inner._scope[kv.Key] = kv.Value;
        foreach (string scopedName in _scopedVariableNames) inner._scopedVariableNames.Add(scopedName);

        ExpressionSyntax? bodyExpr = GetLambdaBody(lambda);
        if (bodyExpr is null)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, lambda.GetLocation(),
                "Lambda body must be a single expression");
            return null;
        }

        SemanticExpr? body = inner.AnalyzeExpr(bodyExpr, boolType);
        if (body is null) return null;

        return new WithExpr(variables, body, boolType);
    }

    private static List<string> GetLambdaParameterNames(LambdaExpressionSyntax lambda)
    {
        return lambda switch
        {
            SimpleLambdaExpressionSyntax s => new List<string> { s.Parameter.Identifier.Text },
            ParenthesizedLambdaExpressionSyntax p =>
                p.ParameterList.Parameters.Select(x => x.Identifier.Text).ToList(),
            _ => new List<string>()
        };
    }

    private static ExpressionSyntax? GetLambdaBody(LambdaExpressionSyntax lambda)
    {
        return lambda.Body switch
        {
            ExpressionSyntax expr => expr,
            BlockSyntax block when block.Statements.Count == 1 &&
                block.Statements[0] is ReturnStatementSyntax ret => ret.Expression,
            _ => null
        };
    }

    private SemanticExpr? AnalyzeMemberAccess(MemberAccessExpressionSyntax mem)
    {
        // Check if this is a Proxy<T> access (DSL fact variable field access)
        SemanticExpr? target = AnalyzeExpr(mem.Expression, null);
        if (target is null) return null;

        SymbolInfo symbolInfo = _semanticModel.GetSymbolInfo(mem);
        ISymbol? memberSymbol = symbolInfo.Symbol;

        if (memberSymbol is null)
        {
            // Could be a Proxy<T> field access — the Proxy type doesn't have the fields yet
            // since they're generated. Try to resolve via the variable type.
            if (target is VarExpr varExpr && varExpr.Variable.Kind is not VariableKind.Terminal)
            {
                ITypeSymbol factType = varExpr.Variable.Type;
                string fieldName = mem.Name.Identifier.Text;
                ISymbol? fieldSym = factType.GetMembers(fieldName).FirstOrDefault();
                if (fieldSym is IPropertySymbol prop)
                {
                    return new FieldExpr(target, fieldSym, prop.Type);
                }
                if (fieldSym is IFieldSymbol fld)
                {
                    return new FieldExpr(target, fieldSym, fld.Type);
                }
                _reporter.Error(DiagnosticsCatalog.InvalidMemberAccess, mem.GetLocation(), fieldName);
                return null;
            }

            _reporter.Error(DiagnosticsCatalog.InvalidMemberAccess,
                mem.GetLocation(),
                mem.Name.Identifier.Text);
            return null;
        }

        ITypeSymbol? fieldType = memberSymbol switch
        {
            IPropertySymbol prop => prop.Type,
            IFieldSymbol fld => fld.Type,
            IMethodSymbol mth => mth.ReturnType,
            _ => null
        };

        if (fieldType is null)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidMemberAccess,
                mem.GetLocation(),
                memberSymbol.Name);
            return null;
        }

        return new FieldExpr(target, memberSymbol, fieldType);
    }

    private SemanticExpr? AnalyzeIdentifier(IdentifierNameSyntax ident)
    {
        string name = ident.Identifier.Text;

        if (_scope.TryGetValue(name, out VariableSymbol? variable))
            return new VarExpr(variable);

        // Try via semantic model
        SymbolInfo symbolInfo = _semanticModel.GetSymbolInfo(ident);
        if (symbolInfo.Symbol is IParameterSymbol param)
        {
            if (_scope.TryGetValue(param.Name, out VariableSymbol? pv))
                return new VarExpr(pv);
        }

        if (symbolInfo.Symbol is ILocalSymbol local)
        {
            if (_scope.TryGetValue(local.Name, out VariableSymbol? lv))
                return new VarExpr(lv);
        }

        if (_scopedVariableNames.Contains(name))
        {
            _reporter.Error(DiagnosticsCatalog.InvalidScopedVariableEscape, ident.GetLocation(), name);
            return null;
        }

        _reporter.Error(DiagnosticsCatalog.UnsupportedExpression,
            ident.GetLocation(),
            $"Unknown identifier '{name}'");
        return null;
    }

    private SemanticExpr? AnalyzeLiteral(LiteralExpressionSyntax lit)
    {
        TypeInfo typeInfo = _semanticModel.GetTypeInfo(lit);
        ITypeSymbol type = typeInfo.Type ?? _semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        object? value = _semanticModel.GetConstantValue(lit).Value;
        return new ConstExpr(value, type);
    }

    private static bool IsLogicExprBool(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named)
        {
            if (named.Name == "LogicExpr" && named.TypeArguments.Length == 1)
            {
                ITypeSymbol arg = named.TypeArguments[0];
                return arg.SpecialType == SpecialType.System_Boolean;
            }
        }
        return false;
    }

    /// <summary>Analyzes a <c>Logic.Empty&lt;T&gt;()</c> call and returns a <see cref="ListEmptyExpr"/>.</summary>
    private SemanticExpr? AnalyzeListEmpty(IMethodSymbol method)
    {
        if (method.TypeArguments.Length != 1)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, null,
                "Logic.Empty<T>() requires exactly one type argument");
            return null;
        }

        ITypeSymbol elementType = method.TypeArguments[0];
        // Return type is LogicExpr<LogicList<T>>; unwrap to get LogicList<T>
        ITypeSymbol listType = method.ReturnType is INamedTypeSymbol ret && ret.TypeArguments.Length == 1
            ? ret.TypeArguments[0]
            : method.ReturnType;

        return new ListEmptyExpr(elementType, listType);
    }

    /// <summary>Analyzes a <c>Logic.List&lt;T&gt;(...)</c> call and returns nested list expressions.</summary>
    private SemanticExpr? AnalyzeList(InvocationExpressionSyntax inv, IMethodSymbol method)
    {
        if (method.TypeArguments.Length != 1)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidListExpression, inv.GetLocation(),
                "Logic.List<T>(...) requires a single list element type.");
            return null;
        }

        ITypeSymbol elementType = method.TypeArguments[0];
        ITypeSymbol listType = method.ReturnType is INamedTypeSymbol ret && ret.TypeArguments.Length == 1
            ? ret.TypeArguments[0]
            : method.ReturnType;

        SemanticExpr result = new ListEmptyExpr(elementType, listType);
        for (int argumentIndex = inv.ArgumentList.Arguments.Count - 1; argumentIndex >= 0; argumentIndex--)
        {
            ArgumentSyntax argument = inv.ArgumentList.Arguments[argumentIndex];
            SemanticExpr? item = AnalyzeExpr(argument.Expression, elementType);
            if (item is null)
                return null;

            result = new ListConsExpr(item, result, elementType, listType);
        }

        return result;
    }

    /// <summary>Analyzes a <c>Logic.Cons&lt;T&gt;(head, tail)</c> call and returns a <see cref="ListConsExpr"/>.</summary>
    private SemanticExpr? AnalyzeListCons(InvocationExpressionSyntax inv, IMethodSymbol method)
    {
        if (method.TypeArguments.Length != 1)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "Logic.Cons<T>() requires exactly one type argument");
            return null;
        }

        if (inv.ArgumentList.Arguments.Count != 2)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "Logic.Cons<T>() requires exactly two arguments (head, tail)");
            return null;
        }

        ITypeSymbol elementType = method.TypeArguments[0];
        ITypeSymbol listType = method.ReturnType is INamedTypeSymbol ret && ret.TypeArguments.Length == 1
            ? ret.TypeArguments[0]
            : method.ReturnType;

        SemanticExpr? head = AnalyzeExpr(inv.ArgumentList.Arguments[0].Expression, null);
        SemanticExpr? tail = AnalyzeExpr(inv.ArgumentList.Arguments[1].Expression, null);
        if (head is null || tail is null) return null;

        return new ListConsExpr(head, tail, elementType, listType);
    }

    /// <summary>Analyzes a <c>Logic.Not(goal)</c> call and returns a <see cref="NotExpr"/>.</summary>
    private SemanticExpr? AnalyzeNot(InvocationExpressionSyntax inv)
    {
        ITypeSymbol boolType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);

        if (inv.ArgumentList.Arguments.Count != 1)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody, inv.GetLocation(),
                "Logic.Not() requires exactly one argument");
            return null;
        }

        SemanticExpr? goal = AnalyzeExpr(inv.ArgumentList.Arguments[0].Expression, boolType);
        if (goal is null) return null;

        return new NotExpr(goal, boolType);
    }

    private VariableKind ClassifyWithVariableKind(ITypeSymbol typeSymbol, Location location)
    {
        if (typeSymbol.Kind == SymbolKind.ErrorType)
        {
            _reporter.Error(
                DiagnosticsCatalog.UnsupportedOrAmbiguousWithResolution,
                location,
                typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            return VariableKind.Fresh;
        }

        return SourceSymbolHelpers.HasAttribute(typeSymbol, "Fletched.Core.FactAttribute", "FactAttribute")
            ? VariableKind.Source
            : VariableKind.Fresh;
    }
}
