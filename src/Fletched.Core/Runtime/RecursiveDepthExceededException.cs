using System;
using System.Collections.Generic;
using System.Linq;

namespace Fletched.Core.Runtime;

/// <summary>Raised when runtime recursion depth exceeds configured maximum.</summary>
public sealed class RecursiveDepthExceededException : InvalidOperationException
{
    public const string RecursiveDepthExceededDiagnosticId = "FLR1001";
    public const string RecursiveGuardInsideNegationDiagnosticId = "FLR1003";

    public RecursiveDepthExceededException(
        string predicateName,
        int depth,
        int maxDepth,
        IReadOnlyList<string> callChain,
        bool insideNegation)
        : base(BuildMessage(predicateName, depth, maxDepth, callChain, insideNegation))
    {
        PredicateName = predicateName;
        Depth = depth;
        MaxDepth = maxDepth;
        CallChain = callChain;
        IsInsideNegation = insideNegation;
    }

    public string PredicateName { get; }

    public int Depth { get; }

    public int MaxDepth { get; }

    public IReadOnlyList<string> CallChain { get; }

    public bool IsInsideNegation { get; }

    public string DiagnosticId => IsInsideNegation
        ? RecursiveGuardInsideNegationDiagnosticId
        : RecursiveDepthExceededDiagnosticId;

    private static string BuildMessage(
        string predicateName,
        int depth,
        int maxDepth,
        IReadOnlyList<string> callChain,
        bool insideNegation)
    {
        string chain = callChain.Count == 0
            ? predicateName
            : string.Join(" -> ", callChain.Select(name => name));

        string negationSuffix = insideNegation
            ? " Guard violation occurred inside negation."
            : string.Empty;

        return $"Recursive depth exceeded for predicate '{predicateName}'. Depth={depth}, MaxDepth={maxDepth}, CallChain={chain}.{negationSuffix}";
    }
}
