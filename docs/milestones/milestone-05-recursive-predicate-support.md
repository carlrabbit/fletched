# Goal

Define and implement baseline recursive predicate support using the existing predicate invocation ABI, typed state model, and source-order depth-first backtracking semantics.

# Status (2026-05-16)

- Overall: **complete**
- Phase 1 — Call graph analysis and recursive-negation diagnostics: **complete**
- Phase 2 — Invocation ABI alignment for recursive argument/result mapping: **complete**
- Phase 3 — Runtime and validation expansion: **complete**

Completed validation:
- `dotnet build Fletched.slnx -c Release --no-restore`
- `dotnet run --no-build -c Release --project tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj`
- `dotnet run --no-build -c Release --project tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj`
- `dotnet run --no-build -c Release --project tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj`
- `dotnet run --no-build -c Release --project tests/Fletched.Performance.Tests/Fletched.Performance.Tests.csproj`

# Scope

- Positive direct recursion through the existing predicate call path
- Positive mutual recursion through the existing predicate call path
- Predicate call graph construction and recursion classification
- Negative recursion detection with cycle-aware diagnostics
- Recursive copy-in / copy-out semantics and frame-local backtracking behavior

# Constraints

- Preserve compiled DSL → semantic model → lowering → plan → codegen → execution architecture
- Preserve typed generated state and iterator-based execution
- Preserve source-order depth-first search behavior
- Reject recursive negation before lowering/code generation
- Do not introduce tabling, memoization, or a custom recursion scheduler

# Deliverables

- Synchronized updates to:
  - `docs/TERMINOLOGY.md`
  - `docs/specs/PredicateInvocation.md`
  - `docs/specs/Backtracking.md`
  - `docs/specs/LoweringRules.md`
  - `docs/specs/Diagnostics.md`
  - `docs/specs/DSL.md`
  - `docs/MILESTONES.md`
- Call-graph analysis and recursive-negation validation in the generator pipeline
- Tests for recursive call graph analysis, recursive-negation diagnostics, and recursive call emission behavior

# Validation Strategy

- Verify recursive predicate calls remain ordinary predicate calls in the lowered plan and emitted code
- Verify recursive-negation cycles are rejected with cycle-aware diagnostics
- Verify argument/result mapping still uses deterministic copy-in / copy-out across recursive call sites
