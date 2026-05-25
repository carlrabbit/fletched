# Public Documentation

## Purpose

`public-docs/` is the consumer-facing documentation layer for published Fletched packages.

## Required Surfaces

- `public-docs/installation.md`
- `public-docs/getting-started.md`
- `public-docs/concepts.md`
- `public-docs/packages.md`
- `public-docs/diagnostics.md`
- `public-docs/versioning.md`
- `public-docs/release-notes.md`
- `public-docs/api-baselines/Fletched.Core.publicapi.txt`
- `public-docs/api-baselines/Fletched.Roslyn.publicapi.txt`
- `public-docs/nuget/Fletched.Core.md`
- `public-docs/nuget/Fletched.Roslyn.md`

## Synchronization Rules

When public package behavior, package metadata, or package references change, update:
- relevant documents under `public-docs/`
- `docs/specs/PublicApi.md` when the public API contract changes
- `eng/public-docs.sh` and `eng/public-api.sh` validations as needed
- package README mapping in `src/Fletched.Core/Fletched.Core.csproj` and `src/Fletched.Roslyn/Fletched.Roslyn.csproj`

## Constraints

- Public docs are consumer-facing; do not document internal compiler/IR/planning internals as public APIs.
- `public-docs/` must not contain `README.md`.
