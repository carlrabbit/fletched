# Goal

Define the current repository-level architecture and the responsibilities of the runtime, generator, tests, and supporting documentation.

# Responsibilities

- `src/Fletched.Core` owns the DSL surface, runtime execution primitives, and fact storage types.
- `src/Fletched.Roslyn` owns validation, semantic lowering, planning, and generated code emission.
- `tests/` owns regression coverage across runtime, feature, integration, performance, and sample behavior.
- `samples/` provides runnable application examples that exercise repository-supported usage patterns.
- `docs/` owns authoritative engineering semantics, operational knowledge, and synchronization rules.
- `specs/` preserves detailed design notes that support implementation and architectural discussion.

# Constraints

- Runtime behavior must remain strongly typed and deterministic.
- Predicate execution must remain compile-time generated rather than interpreter-driven.
- Documentation must separate architecture, workflow intent, and recurring execution guidance.
- Workflow intent must remain documented independently from GitHub Actions YAML.

# Non-Goals

- Full ISO Prolog compatibility.
- Runtime interpretation of arbitrary logic definitions.
- Repository knowledge encoded only in tool-specific instructions.

# Related Documents

- `docs/architecture/source-generation-pipeline.md`
- `docs/architecture/execution-model.md`
- `docs/architecture/fact-storage-and-indexing.md`
- `docs/decisions/0001-compiled-typed-relational-engine.md`
- `specs/`
