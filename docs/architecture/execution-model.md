# Goal

Describe the runtime execution behavior of generated predicates, including enumeration, backtracking, and state handling.

# Responsibilities

- Generated predicates enumerate results synchronously through `Execute(...)` and asynchronously through `ExecuteAsync(...)`.
- Generated state machines manage variable bindings, frames, choice points, and trail restoration.
- Predicate composition supports reusable subgoals across backtracking boundaries.
- Metrics and observer hooks distinguish execution behaviors such as scans and index hits.

# Constraints

- Synchronous and asynchronous execution paths must preserve the same logical outcomes.
- Execution state must remain separate from `EngineContext` fact storage.
- Backtracking semantics must remain deterministic across scans, indexed lookups, and predicate calls.
- Cancellation must propagate through async enumeration.

# Non-Goals

- A single interpreter loop shared by all predicates.
- Implicit global execution state.
- Dynamic mutation of fact storage during predicate execution.

# Related Documents

- `docs/architecture/system-overview.md`
- `docs/architecture/fact-storage-and-indexing.md`
- `docs/TERMINOLOGY.md`
- `docs/specs/FactStorage.md`
- `docs/specs/EngineContext.md`
