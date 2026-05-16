# Goal

Stabilize baseline recursive predicate support with explicit safety controls, diagnostics, observability, and performance baselines.

# Status (2026-05-16)

- Overall: **complete**
- Phase 1 — Docs and specs: **complete**
- Phase 2 — Runtime options: **complete**
- Phase 3 — Guard enforcement: **complete**
- Phase 4 — Diagnostics and observability: **complete**
- Phase 5 — Benchmarks: **complete**
- Phase 6 — Validation: **complete**

Completed validation:

- `dotnet restore Fletched.slnx`
- `dotnet build Fletched.slnx -c Release --no-restore`
- `dotnet run --no-build -c Release --project tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj`
- `dotnet run --no-build -c Release --project tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj`
- `dotnet run --no-build -c Release --project tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj`
- `dotnet run --no-build -c Release --project tests/Fletched.Performance.Tests/Fletched.Performance.Tests.csproj`
- `dotnet build benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj -c Release`

# Scope

- Recursion depth guard
- Recursion diagnostics
- Recursive execution metrics/tracing
- Recursive benchmark scenarios
- Recursive performance baseline documentation
- Validation tests for productive and non-productive recursive patterns
- Documentation/spec synchronization

# Constraints

- Preserve baseline recursive predicate semantics
- Preserve source-order depth-first execution
- Preserve existing predicate invocation ABI
- Preserve copy-in/copy-out semantics
- Do not introduce tabling or memoization
- Do not introduce recursive query planning

# Deliverables

- `docs/specs/RecursiveSafety.md`
- `docs/specs/RecursivePerformanceBaselines.md`
- `docs/specs/RecursivePredicates.md`
- updates to diagnostics, observability, predicate invocation, engine context, and performance specs
- runtime recursion guard configuration (`MaxRecursionDepth`)
- guard violation runtime diagnostics and observer callbacks
- recursive benchmark scenarios and baseline-compatible output
- guard behavior and diagnostics tests

# Finish Milestone Closure Checklist

- [x] Exit criteria checked explicitly
- [x] Remaining work captured outside milestone scope (none identified)
- [x] Related specs synchronized
- [x] Validation evidence recorded
