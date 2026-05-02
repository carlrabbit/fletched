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

    public SemanticAnalyzer(
        Microsoft.CodeAnalysis.SemanticModel semanticModel,
        DiagnosticReporter reporter)
    {
        _semanticModel = semanticModel;
        _reporter = reporter;
    }

    public PredicateModel? Analyze(INamedTypeSymbol predicateType)
    {
        // Find the [PredicateBody] method
        IMethodSymbol? bodyMethod = predicateType.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "PredicateBodyAttribute"));

        if (bodyMethod is null)
        {
            _reporter.Error(DiagnosticsCatalog.InvalidPredicateBody,
                predicateType.Locations.FirstOrDefault(),
                "No method marked with [PredicateBody] found");
            return null;
        }

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

        return new PredicateModel(predicateType.Name, predicateType, parameters, body);
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
                return new ConjExpr(parts, boolType);
            }

            case SyntaxKind.BitwiseOrExpression:
            {
                SemanticExpr? left = AnalyzeExpr(bin.Left, boolType);
                SemanticExpr? right = AnalyzeExpr(bin.Right, boolType);
                if (left is null || right is null) return null;
                return new DisjExpr(left, right, boolType);
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

            return new ConstraintExpr(method, args, boolType);
        }

        // Try syntactic fallback for Logic.With<T>
        if (inv.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.Text: "With" } })
        {
            return AnalyzeWithSyntactic(inv);
        }

        _reporter.Error(DiagnosticsCatalog.UnsupportedExpression,
            inv.GetLocation(),
            "Unresolved invocation");
        return null;
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
            var v = new VariableSymbol(name, typeArgs[i], VariableKind.Local);
            variables.Add(v);
            newScope[name] = v;
        }

        // Analyze lambda body with extended scope
        var inner = new SemanticAnalyzer(_semanticModel, _reporter);
        foreach (var kv in newScope) inner._scope[kv.Key] = kv.Value;

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
            if (target is VarExpr varExpr && varExpr.Variable.Kind == VariableKind.Local)
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
}
