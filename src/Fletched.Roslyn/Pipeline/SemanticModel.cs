using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

// ─── VariableSymbol ────────────────────────────────────────────────────────

public enum VariableKind { Terminal, Local }

/// <summary>A resolved logical variable with a name, type, and kind.</summary>
public record VariableSymbol(string Name, ITypeSymbol Type, VariableKind Kind);

// ─── SemanticExpr hierarchy ────────────────────────────────────────────────

/// <summary>Base node for the semantic expression tree.</summary>
public abstract record SemanticExpr(ITypeSymbol Type);

/// <summary>Reference to a declared variable.</summary>
public record VarExpr(VariableSymbol Variable) : SemanticExpr(Variable.Type);

/// <summary>Compile-time constant.</summary>
public record ConstExpr(object? Value, ITypeSymbol Type) : SemanticExpr(Type);

/// <summary>Field/property access on a target expression.</summary>
public record FieldExpr(SemanticExpr Target, ISymbol Member, ITypeSymbol FieldType)
    : SemanticExpr(FieldType);

/// <summary>Logical unification (== in DSL).</summary>
public record UnifyExpr(SemanticExpr Left, SemanticExpr Right)
    : SemanticExpr(Left.Type); // type is the operand type (used for validation)

/// <summary>Logical conjunction — flattened.</summary>
public record ConjExpr(IReadOnlyList<SemanticExpr> Parts, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Logical disjunction.</summary>
public record DisjExpr(SemanticExpr Left, SemanticExpr Right, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Boolean method constraint.</summary>
public record ConstraintExpr(IMethodSymbol Method, IReadOnlyList<SemanticExpr> Arguments, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Scoped fact variable introduction (With&lt;T&gt; in DSL).</summary>
public record WithExpr(IReadOnlyList<VariableSymbol> Variables, SemanticExpr Body, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Call to another predicate.</summary>
public record CallExpr(INamedTypeSymbol PredicateType, IReadOnlyList<SemanticExpr> Arguments, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Comparison operator kind.</summary>
public enum CompOp { NotEqual, LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual }

/// <summary>Arithmetic operator kind.</summary>
public enum ArithOp { Add, Subtract }

/// <summary>Binary comparison constraint (!=, &lt;, &gt;, &lt;=, &gt;= in DSL).</summary>
public record CompExpr(CompOp Op, SemanticExpr Left, SemanticExpr Right, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Binary arithmetic expression (+, - in DSL). Produces a value of the same type as its operands.</summary>
public record ArithExpr(ArithOp Op, SemanticExpr Left, SemanticExpr Right)
    : SemanticExpr(Left.Type);

/// <summary>Negation-as-failure: Not(G) succeeds iff G produces no solutions.</summary>
public record NotExpr(SemanticExpr Goal, ITypeSymbol BoolType)
    : SemanticExpr(BoolType);

/// <summary>Empty logical list of element type <see cref="ElementType"/>.</summary>
public record ListEmptyExpr(ITypeSymbol ElementType, ITypeSymbol ListType)
    : SemanticExpr(ListType);

/// <summary>Cons cell: head element prepended to a tail list.</summary>
public record ListConsExpr(SemanticExpr Head, SemanticExpr Tail, ITypeSymbol ElementType, ITypeSymbol ListType)
    : SemanticExpr(ListType);

// ─── PredicateModel ────────────────────────────────────────────────────────

/// <summary>Fully-resolved model of a single [Predicate] type.</summary>
public record PredicateModel(
    string Name,
    INamedTypeSymbol Symbol,
    IReadOnlyList<VariableSymbol> Parameters,
    SemanticExpr Body);
