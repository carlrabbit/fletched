using System;

namespace Fletched.Core.Runtime;

/// <summary>Deterministic key for a tabled predicate call within a query scope.</summary>
public readonly record struct TableKey(
    string PredicateIdentity,
    int Arity,
    string CanonicalCall)
{
    public static TableKey Create(string predicateIdentity, int arity, string canonicalCall)
    {
        if (string.IsNullOrWhiteSpace(predicateIdentity))
            throw new ArgumentException("Predicate identity must not be null or whitespace.", nameof(predicateIdentity));

        if (string.IsNullOrWhiteSpace(canonicalCall))
            throw new ArgumentException("Canonical call must not be null or whitespace.", nameof(canonicalCall));

        if (arity < 0)
            throw new ArgumentOutOfRangeException(nameof(arity), "Arity must be non-negative.");

        return new TableKey(predicateIdentity, arity, canonicalCall);
    }
}
