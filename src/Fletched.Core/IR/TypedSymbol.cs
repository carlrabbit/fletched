namespace Fletched.Core.IR;

/// <summary>Distinguishes between fact and predicate type categories in the typed IR.</summary>
public enum TypedSymbolKind { Fact, Predicate }

/// <summary>
/// Typed IR representation of a user-defined <c>[Fact]</c> or <c>[Predicate]</c> type.
/// Produced by the first pipeline stage (Roslyn symbol → typed IR).
/// </summary>
public sealed record TypedSymbol(
    string Name,
    string Namespace,
    TypedSymbolKind Kind,
    IReadOnlyList<TypedField> Fields);

/// <summary>A public field or property descriptor extracted from a <see cref="TypedSymbol"/>.</summary>
public sealed record TypedField(string Name, string TypeDisplayString);
