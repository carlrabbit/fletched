using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

/// <summary>
/// Compile-time diagnostic descriptors for Fletched DSL errors and warnings.
/// </summary>
public static class DiagnosticsCatalog
{
    private const string Category = "Fletched";

    public static readonly DiagnosticDescriptor InvalidPredicateBody = new(
        "FLTCH001",
        "Invalid predicate body",
        "{0}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedExpression = new(
        "FLTCH002",
        "Unsupported expression",
        "Expression of kind '{0}' is not supported in a predicate body",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMismatch = new(
        "FLTCH003",
        "Type mismatch",
        "Cannot unify '{0}' with '{1}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnboundTerminalVar = new(
        "FLTCH004",
        "Unbound terminal variable",
        "Terminal variable '{0}' is not bound in all execution paths",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFactType = new(
        "FLTCH005",
        "Invalid fact type",
        "Type '{0}' marked with [Fact] must be a partial record struct",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMemberAccess = new(
        "FLTCH006",
        "Invalid member access",
        "Member '{0}' is not a readable property or field on a [Fact] type",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidConstraint = new(
        "FLTCH007",
        "Invalid constraint",
        "Constraint method '{0}' must return bool and be side-effect free",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateVariable = new(
        "FLTCH008",
        "Duplicate variable",
        "Variable '{0}' is already declared in this scope",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidPredicateCall = new(
        "FLTCH009",
        "Invalid predicate call",
        "Predicate '{0}' was not found or argument types do not match",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidListExpression = new(
        "FLTCH010",
        "Invalid list expression",
        "{0}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidPredicateType = new(
        "FLTCH011",
        "Invalid predicate type",
        "Type '{0}' marked with [Predicate] must be a partial record struct",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidModuleType = new(
        "FLTCH012",
        "Invalid module type",
        "Type '{0}' marked with [Module] must be a static partial class",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidContainingType = new(
        "FLTCH013",
        "Invalid containing type",
        "Containing type '{0}' must be partial because it encloses generated Fletched declarations",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedOrAmbiguousWithResolution = new(
        "FLTCH014",
        "Unsupported or ambiguous With<T> resolution",
        "With<T> type '{0}' cannot be resolved deterministically as a source variable or fresh variable",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidScopedVariableEscape = new(
        "FLTCH015",
        "Invalid scoped variable escape",
        "Variable '{0}' escapes its local scope",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UngroundedNegation = new(
        "FLG0001",
        "Ungrounded variable in negation",
        "Variable '{0}' is used in Logic.Not(...) before it is grounded",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NegationVariableEscape = new(
        "FLG0002",
        "Variable escapes negation scope",
        "Variable '{0}' introduced inside Logic.Not(...) escapes negation scope",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedRecursiveNegation = new(
        "FLG0003",
        "Unsupported recursive negation",
        "Recursive negation is not supported{0}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedInvocationPatternInNegation = new(
        "FLG0004",
        "Unsupported invocation pattern in negation",
        "Invocation pattern in Logic.Not(...) is not supported",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedSubsumptiveTabling = new(
        "FLT2001",
        "Unsupported subsumptive tabling",
        "Subsumptive tabling is not supported in this milestone",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTabledNegationCycle = new(
        "FLT2002",
        "Invalid tabled negation cycle",
        "Tabled predicate participates in an unsupported recursive negation cycle",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousTableKey = new(
        "FLT2003",
        "Ambiguous table key",
        "Table key cannot be constructed deterministically",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedAsyncTablePattern = new(
        "FLT2004",
        "Unsupported async table pattern",
        "Async tabled recursive pattern is unsupported",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedTabledMutualRecursion = new(
        "FLT2005",
        "Unsupported tabled mutual recursion",
        "Mutually recursive [Tabled] predicates are not supported in this milestone",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MagicRewriteUnsupported = new(
        "FLM3001",
        "Magic rewrite unsupported",
        "Magic-set rewriting is unsupported for '{0}' and the planner will use the conservative fallback plan",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MagicRewriteSkippedAllFree = new(
        "FLM3002",
        "Magic rewrite skipped for all-free call",
        "Magic-set rewriting was skipped for '{0}' because all arguments are free",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MagicRewriteRejectedNegation = new(
        "FLM3003",
        "Magic rewrite rejected by negation",
        "Magic-set rewriting was rejected for '{0}' because recursive negation is involved",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingRecursiveIndex = new(
        "FLM3004",
        "Missing recursive index",
        "Recursive predicate '{0}' could benefit from an indexed access path",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousAdornment = new(
        "FLM3005",
        "Ambiguous adornment",
        "Binding pattern for '{0}' could not be determined safely and the planner used a conservative free adornment",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnknownIndexMember = new(
        "FLI4001",
        "Unknown index member",
        "Fact '{0}' index member '{1}' is invalid: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidIndexMember = new(
        "FLI4002",
        "Invalid index member",
        "Fact '{0}' index member '{1}' is invalid: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedRangeIndexType = new(
        "FLI4003",
        "Unsupported range index type",
        "Fact '{0}' range index member '{1}' is invalid: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedCompositeRangeIndex = new(
        "FLI4004",
        "Unsupported composite range index",
        "Fact '{0}' range index declaration '{1}' is invalid: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateIndexDeclaration = new(
        "FLI4005",
        "Duplicate index declaration",
        "Fact '{0}' index declaration '{1}' is duplicated: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IndexNameCollision = new(
        "FLI4006",
        "Index name collision",
        "Fact '{0}' index name '{1}' collides: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateUniqueIndexValue = new(
        "FLI4007",
        "Duplicate unique index value",
        "Fact '{0}' unique index '{1}' received a duplicate value: {2}. Suggested fix: {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
