# Goal

Stabilize the core compiled relational engine surface and its repository-native documentation.

# Scope

- Runtime DSL and fact storage primitives
- Source generation pipeline and execution planning
- Core, integration, and feature validation
- Canonical terminology and architecture overview documents

# Constraints

- Preserve typed execution semantics.
- Preserve deterministic build and test workflows.
- Keep authoritative documentation synchronized with implementation.

# Deliverables

- Stable `Fletched.Core` and `Fletched.Roslyn` project boundaries
- Repository workflow documentation for build, test, performance, and package release
- Initial decision and terminology documents

# Non-Goals

- Broad runtime feature expansion without documentation updates
- Repository-specific automation beyond documented workflow intent

# Dependencies

- `docs/architecture/`
- `docs/decisions/0001-compiled-typed-relational-engine.md`
- `.github/workflows/build-and-test.yml`
