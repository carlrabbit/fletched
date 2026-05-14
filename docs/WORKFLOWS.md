# Workflows

| Workflow | Purpose |
| --- | --- |
| [`build-and-test.md`](workflows/build-and-test.md) | Validate restore, build, and automated test execution |
| [`nuget-pack-and-publish.md`](workflows/nuget-pack-and-publish.md) | Pack NuGet artifacts and publish tagged releases |
| [`performance-testing.md`](workflows/performance-testing.md) | Run performance-focused tests and benchmarks |

## Cross-document synchronization workflow

Changes to predicate invocation or negation semantics must update the authoritative documentation set together:

- `specs/PredicateInvocation.md.txt`
- `specs/Backtracking.md.txt`
- `specs/LoweringRules.md.txt`
- `specs/Diagnostics.md.txt`
- `specs/DSL.md.txt`
- `docs/TERMINOLOGY.md`
