# Purpose

Keep predicate invocation and negation-as-failure semantics synchronized across architecture docs, lowering rules, diagnostics, and implementation planning.

# Preconditions

- The change affects predicate calls, invocation boundaries, caller/callee ownership, or negation semantics.
- The authoritative document set has been identified.

# Required Reading

- `docs/TERMINOLOGY.md`
- `specs/PredicateInvocation.md.txt`
- `specs/Backtracking.md.txt`
- `specs/LoweringRules.md.txt`
- `specs/Diagnostics.md.txt`
- `specs/DSL.md.txt`

# Execution Steps

1. Update canonical terms first (`Ground`, invocation boundary, caller/callee state, copy-in/copy-out).
2. Update invocation semantics in `specs/PredicateInvocation.md.txt`.
3. Synchronize cross-boundary backtracking and trail ownership in `specs/Backtracking.md.txt`.
4. Synchronize deterministic lowering rules for `CallNode`/`NotNode` in `specs/LoweringRules.md.txt`.
5. Synchronize negation semantics in `specs/DSL.md.txt`.
6. Synchronize grounding and scope diagnostics in `specs/Diagnostics.md.txt`.

# Validation

- Check that invocation success/exhaustion semantics are defined once and referenced consistently.
- Check that negation grounding/isolation rules are consistent across DSL, lowering, and diagnostics.
- Check that milestone planning references the same document set.

# Common Failures

- Invocation ABI semantics documented only in implementation notes
- Negation grounding rules omitted from diagnostics
- Backtracking ownership rules inconsistent between invocation and backtracking docs

# Synchronization Requirements

- Keep `specs/PredicateInvocation.md.txt`, `specs/Backtracking.md.txt`, `specs/LoweringRules.md.txt`, `specs/Diagnostics.md.txt`, and `specs/DSL.md.txt` synchronized for invocation/negation changes.
- Update `docs/milestones/` planning when semantic scope or dependency order changes.
- Update `docs/TERMINOLOGY.md` before introducing new canonical terms across multiple docs.
