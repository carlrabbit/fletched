# Context

Fletched needs a repository-level architectural direction that explains why the project uses a typed DSL, source generation, and explicit runtime fact storage instead of an interpreter-centered design.

# Decision

Fletched uses a compiled relational model:

- facts and predicates are declared in C# with repository-defined attributes
- the Roslyn generator validates and lowers those declarations into executable plans
- generated code executes against explicit `EngineContext` fact storage
- synchronous and asynchronous enumeration are both first-class generated outputs

# Consequences

- Runtime execution stays strongly typed and avoids interpreter overhead.
- Generator and runtime responsibilities remain separated across `Fletched.Roslyn` and `Fletched.Core`.
- Architecture, terminology, and workflow intent need explicit documentation because behavior spans compile-time and runtime boundaries.
- Repository contributors must keep generated-surface assumptions synchronized with specs, architecture docs, and tests.

# Alternatives Considered

- A traditional interpreter with runtime expression traversal.
- A reflection-heavy query layer with weaker compile-time validation.
- A tool-specific instruction set in place of repository-native architectural documents.

# Related Documents

- `docs/architecture/system-overview.md`
- `docs/architecture/source-generation-pipeline.md`
- `docs/architecture/execution-model.md`
- `specs/`
