namespace Fletched.Core.Runtime;

/// <summary>Status of an answer table during query-scoped production.</summary>
public enum TableStatus
{
    Producing = 0,
    Complete = 1,
    Faulted = 2
}
