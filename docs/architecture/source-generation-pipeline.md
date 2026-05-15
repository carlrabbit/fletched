# Goal

Define how attributed source moves from user-written facts and predicates to generated executable code.

# Responsibilities

- Validate `[Fact]`, `[Predicate]`, `[PredicateBody]`, and module-scoped declarations before generation.
- Build semantic models that describe facts, predicates, bodies, and lowered expressions.
- Build execution plans that preserve logical semantics while enabling optimized code generation.
- Emit generated C# for runtime entry points, result shapes, and async variants.

# Constraints

- Validation must fail early for unsupported or inconsistent source shapes.
- Generated code must preserve typed query boundaries and explicit `EngineContext` usage.
- Async enumeration must remain a native `IAsyncEnumerable<T>` implementation with cancellation support.
- Planning and emission must preserve backtracking semantics.

# Non-Goals

- Runtime compilation through reflection or expression tree interpretation.
- Hidden ambient execution context.
- Tool-specific generator instructions as the source of architectural truth.

# Related Documents

- `docs/TERMINOLOGY.md`
- `docs/architecture/system-overview.md`
- `docs/architecture/execution-model.md`
- `docs/specs/EngineContext.md`
- `docs/specs/IndexedSources.md`
