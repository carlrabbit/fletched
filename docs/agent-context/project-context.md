# Project context

## Architectural ownership

- `Fletched.Core` owns the runtime DSL, `EngineContext`, and fact storage primitives.
- `Fletched.Roslyn` owns validation, lowering, planning, and C# generation.
- `tests/` owns behavioral verification across correctness, integration, performance, and sample scenarios.

## Critical constraints

- Runtime behavior is strongly typed and compile-time generated.
- `EngineContext` is explicit and remains separate from per-query execution state.
- Workflow intent is documented in `docs/workflows/` and synchronized with `.github/workflows/`.
- Canonical vocabulary lives in `docs/TERMINOLOGY.md`.

## Major non-goals

- Interpreter-first runtime architecture
- Full Prolog compatibility
- Repository knowledge stored only in tool-specific instructions

## Key references

- `docs/TERMINOLOGY.md`
- `docs/architecture/system-overview.md`
- `docs/decisions/0001-compiled-typed-relational-engine.md`
- `docs/workflows/`
- `docs/tbps/`
