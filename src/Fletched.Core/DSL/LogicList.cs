namespace Fletched.Core;

/// <summary>
/// Abstract logical list type used at DSL and type level.
/// No runtime allocation occurs during execution; all list operations are compiled
/// into structural unification and field access over typed state.
/// </summary>
public abstract record LogicList<T>;

/// <summary>Represents the empty list.</summary>
public record LogicListEmpty<T> : LogicList<T>;

/// <summary>Represents a cons cell with a head element and a tail list.</summary>
public record LogicListCons<T>(T Head, LogicList<T> Tail) : LogicList<T>;
