namespace Fletched.Core.Runtime;

/// <summary>
/// Hand-written base for the engine execution context.
/// The source generator adds one <see cref="FactTable{T}"/> property per <c>[Fact]</c> type.
/// </summary>
public partial class EngineContext
{
    /// <summary>Runtime options for operational execution safeguards.</summary>
    public EngineRuntimeOptions RuntimeOptions { get; } = new();

    /// <summary>Gets the current active predicate invocation depth.</summary>
    public int CurrentRecursionDepth => RecursionGuard.GetCurrentDepth(this);

    /// <summary>Marks entry into a negation scope.</summary>
    public void EnterNegationScope() => RecursionGuard.EnterNegationScope(this);

    /// <summary>Marks exit from a negation scope.</summary>
    public void ExitNegationScope() => RecursionGuard.ExitNegationScope(this);

    /// <summary>Enters a predicate invocation frame and enforces recursion depth policy.</summary>
    public void EnterPredicateInvocation(string predicateName, object? observer = null) =>
        RecursionGuard.EnterPredicateInvocation(this, predicateName, observer);

    /// <summary>Exits the current predicate invocation frame.</summary>
    public void ExitPredicateInvocation() => RecursionGuard.ExitPredicateInvocation(this);
}
