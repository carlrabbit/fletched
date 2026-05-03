namespace Fletched.Core.Performance;

/// <summary>
/// Optional observer interface that receives callbacks for significant engine events.
/// Implementations MUST NOT allocate on hot paths and MUST NOT affect control flow.
/// Pass an instance to the generated <c>Execute</c> method to receive events.
/// </summary>
public interface IExecutionObserver
{
    /// <summary>Called when a slot unification is attempted.</summary>
    void OnUnify(int slotId);

    /// <summary>Called when a slot unification fails.</summary>
    void OnUnifyFailure(int slotId);

    /// <summary>Called when the engine backtracks to a choice point.</summary>
    void OnBacktrack();

    /// <summary>Called when a new choice point is pushed onto the stack.</summary>
    void OnChoicePoint();

    /// <summary>Called when a full sequential scan of a fact table begins.</summary>
    void OnFactScan(string factName);

    /// <summary>Called when an indexed lookup is performed on a fact table.</summary>
    void OnIndexHit(string factName);

    /// <summary>Called when a sub-predicate is invoked.</summary>
    void OnPredicateInvocation(string predicateName);
}
