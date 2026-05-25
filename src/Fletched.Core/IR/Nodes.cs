using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fletched.Core.IR;

/// <summary>Base node for the typed intermediate representation of logical expressions.</summary>
public abstract record ExprNode;

/// <summary>A logical variable with a name and type.</summary>
public record VarNode(string Name, Type Type) : ExprNode;

/// <summary>A compile-time constant value.</summary>
public record ConstNode(object? Value, Type Type) : ExprNode;

/// <summary>Structural field access on a target node.</summary>
public record FieldNode(ExprNode Target, MemberInfo Member, Type FieldType) : ExprNode;

/// <summary>Logical unification between two expressions (== in DSL).</summary>
public record UnifyNode(ExprNode Left, ExprNode Right) : ExprNode;

/// <summary>Logical conjunction — always flat (no nested ConjNode parts).</summary>
public record ConjNode(IReadOnlyList<ExprNode> Parts) : ExprNode;

/// <summary>Logical disjunction.</summary>
public record DisjNode(ExprNode Left, ExprNode Right) : ExprNode;

/// <summary>Boolean method call constraint.</summary>
public record ConstraintNode(MethodInfo Method, IReadOnlyList<ExprNode> Arguments) : ExprNode;

/// <summary>Introduces scoped fact variables (from With&lt;T&gt; in DSL).</summary>
public record WithNode(IReadOnlyList<VarNode> Variables, ExprNode Body) : ExprNode;

/// <summary>Call to another predicate.</summary>
public record CallNode(Type PredicateType, IReadOnlyList<ExprNode> Arguments) : ExprNode;

/// <summary>Built-in AllDistinct constraint: all elements in the collection must be pairwise distinct.</summary>
public record AllDistinctNode(ExprNode Collection, Type ElementType) : ExprNode;

/// <summary>Represents the empty logical list of element type <see cref="ElementType"/>.</summary>
public record ListEmptyNode(Type ElementType) : ExprNode;

/// <summary>Represents a cons cell with a head expression and a tail list expression.</summary>
public record ListConsNode(ExprNode Head, ExprNode Tail) : ExprNode;

/// <summary>Arithmetic operation kind used in the DSL.</summary>
public enum ArithOp
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo
}

/// <summary>Binary arithmetic expression (+, - in DSL). Produces a value of the same type.</summary>
public record ArithNode(ArithOp Op, ExprNode Left, ExprNode Right) : ExprNode;

/// <summary>Negation-as-failure: succeeds iff Goal produces no solutions.</summary>
public record NotNode(ExprNode Goal) : ExprNode;

/// <summary>Flattens nested <see cref="ConjNode"/> trees into a single flat list.</summary>
public static class ConjNormalizer
{
    public static ExprNode Normalize(ExprNode node)
    {
        if (node is ConjNode conj)
        {
            var flat = new List<ExprNode>();
            Flatten(conj, flat);
            return new ConjNode(flat);
        }
        return node;
    }

    private static void Flatten(ConjNode conj, List<ExprNode> result)
    {
        foreach (ExprNode part in conj.Parts)
        {
            if (part is ConjNode nested)
                Flatten(nested, result);
            else
                result.Add(part);
        }
    }
}
