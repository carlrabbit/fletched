# Goal

Define how fact data is stored, exposed, and optimized for generated predicate execution.

# Responsibilities

- `FactTable<T>` owns dense fact data for a single fact type.
- Optional indexes map a fact member key to fact-row positions.
- `EngineContext` exposes one fact table per `[Fact]` type.
- Planning selects between full scans and indexed sources based on available equality constraints.

# Constraints

- Fact tables are read-only during query execution.
- Generated fact table access must remain explicit through `EngineContext`.
- Indexed access is valid only when the planner can prove the required key shape.
- Full scan behavior must remain available as the fallback path.

# Non-Goals

- Mutable transactional storage.
- Hidden caching layers outside `FactTable<T>`.
- Multi-step query optimization beyond the current planner-supported rewrites.

# Related Documents

- `docs/TERMINOLOGY.md`
- `docs/architecture/system-overview.md`
- `docs/architecture/execution-model.md`
- `specs/FactStorage.md.txt`
- `specs/IndexedSources.md.txt`
- `specs/EngineContext.md.txt`
