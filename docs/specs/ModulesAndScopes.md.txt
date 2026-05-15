ModulesAndScopes.md

---

1. Overview

Defines how nested relational declarations map to module boundaries and lexical scopes.
A module is both a compile-time namespace and a runtime storage / execution boundary.
Classes without `[Module]` only group generated names; they do not own storage.

---

2. Core Concepts / Data Structures

2.1 Module Declaration

[Module]
public static partial class IdentityModule
{
}

- `[Module]` marks the relational boundary.
- The module type is the root for generated storage and query entrypoints.
- The module declaration must be a `static partial class` so generated members can merge into it and module entrypoints remain type-level.

---

2.2 Module-Owned Engine Context

[Module]
public static partial class IdentityModule
{
    public sealed partial class EngineContext
    {
        public FactTable<User> Users { get; set; }
            = new FactTable<User>();
    }
}

- Facts declared inside a module generate storage on `<Module>.EngineContext`.
- Storage is owned by the module rather than the global `Fletched.Core.Runtime.EngineContext`.
- The generated module context is independent from other modules' contexts.

---

2.3 Scoped Non-Module Containers

public static partial class UserQueries
{
    [Predicate]
    public partial record struct ActiveUsers;
}

- A containing class without `[Module]` is lexical scope only.
- It namespaces generated predicate / result / helper types.
- It does not generate an `EngineContext` and does not become a storage boundary.

---

2.4 Visibility

- `public` facts and predicates are exported from their containing module or scope.
- `internal` and `private` declarations remain local to the generated containing type.
- Public module predicates additionally generate module-level query wrappers.

---

3. Rules and Invariants

- Nested generated partial declarations must preserve the original containing-type hierarchy.
- The source generator reports a compile-time error if `[Module]` is applied to anything other than a `static partial class`.
- Any containing type that encloses generated partial members must be declared `partial`.
- Facts outside a module continue to extend `Fletched.Core.Runtime.EngineContext`.
- Facts inside a module extend `<Module>.EngineContext` instead of the global engine context.
- Predicates inside a module execute against the module-owned `EngineContext`.
- Non-module scopes do not change storage ownership.

---

4. Execution / Behavior

4.1 Module Storage

var ctx = new IdentityModule.EngineContext();
ctx.Users = new FactTable<IdentityModule.User>(new[]
{
    new IdentityModule.User("alice", "Alice"),
    new IdentityModule.User("bob", "Bob"),
});

The module context is the runtime container for facts declared inside that module.

---

4.2 Module Query Entrypoint

var results = IdentityModule.Query_UserNames(ctx);

Public predicates declared inside a module generate module-level query wrappers that delegate to the generated predicate execution methods.

---

4.3 Scope-Only Grouping

var results = default(UserQueries.ActiveUsers).Execute(ctx);

Lexical scopes preserve generated names under the containing type without creating a new storage boundary.

---

5. Examples

5.1 Module

[Module]
public static partial class IdentityModule
{
    [Fact]
    public partial record struct User(string Login, string Name);

    [Predicate]
    public partial record struct UserNames
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> name) =>
            Logic.With<User>(user => user.Name == name);
    }
}

---

5.2 Generated Module Query API

public static partial class IdentityModule
{
    public static IEnumerable<IdentityModule.UserNamesResult> Query_UserNames(
        EngineContext ctx,
        IExecutionObserver? observer = null)
    {
        return default(IdentityModule.UserNames).ExecuteArity1(ctx, observer);
    }
}

---

5.3 Scope-Only Predicate

public static partial class UserQueries
{
    [Predicate]
    public partial record struct ActiveUsers;
}

---
