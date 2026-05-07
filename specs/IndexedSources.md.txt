IndexedSources.md


---

1. Overview

Defines the indexed-source optimization that replaces a full fact-table scan with a keyed lookup when a `With<T>` loop is constrained by `fact.Member == value`.


---

2. Detection Rule

Match a top-level body term shaped like:

Unify(
  Field(Var(fact), Member),
  Key
)

or the symmetric form where `Key` is on the left.

`Key` MUST be either:

- a bound slot/variable, or
- a compile-time constant

The field target MUST be the fact variable introduced by the loop being planned.


---

3. Planned Representation

Loops keep their existing control-flow blocks, but carry indexed-lookup metadata:

IndexedLookupSpec(
  MemberName,
  Key
)

This metadata is attached to the loop init/check/bind instructions so code generation can emit either:

- indexed lookup, or
- full-scan fallback

without changing the surrounding execution model.


---

4. Planning Rewrite

Input

With<User>(user => user.Login == name && Body)

Planned loop

- loop source = indexed on `Login` with key `name`
- remove `user.Login == name` from the remaining body

The removed unify is handled by the loop itself:

- indexed path: succeeds by construction
- scan path: binds/checks the key slot while iterating


---

5. Generated Behaviour

If the key slot is already bound:

if (!ctx.Users.TryGetIndex("Login", state.name, out var indices))
    goto Fail;

foreach (var i in indices)
{
    var user = ctx.Users.Data[i];
    ...
}

If the key slot is not bound:

foreach (var user in ctx.Users.Data)
{
    state.name = user.Login;
    state.name_bound = true;
    ...
}

If the key is a constant, generation always uses the index.


---

6. Invariants

- Indexed lookup is only selected for direct fact-member equality.
- Slot keys fall back to full scan when the slot is unbound at loop entry.
- Constant keys always use indexed lookup.
- The optimized loop must preserve backtracking semantics.
- Observer/metrics hooks distinguish `FactScan` from `IndexHit`.
