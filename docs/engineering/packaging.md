# Packaging

## Purpose

This document routes packaging and release work to the authoritative workflow and TBP documents.

## Authoritative Documents

- [`docs/workflows/nuget-pack-and-publish.md`](../workflows/nuget-pack-and-publish.md)
- [`docs/tbps/release-preparation.md`](../tbps/release-preparation.md)
- [`docs/engineering/command-contract.md`](command-contract.md)

## Canonical Packaging Commands

```sh
./eng/package.sh <version>
```

- Requires one SemVer-compatible version argument.
- Packs `src/Fletched.Core/Fletched.Core.csproj` and `src/Fletched.Roslyn/Fletched.Roslyn.csproj`.
- Uses `Release` configuration with `--no-build`.
- Writes package artifacts to `artifacts/nuget`.

```sh
./eng/package-smoke.sh [version]
```

- Validates consumption from locally packed NuGet artifacts (not project references).
- Verifies generator/analyzer loading and an expected diagnostic emission path.

```sh
./eng/public-api.sh
```

- Builds package projects, extracts contracted public API baselines, and fails on drift.
- Intentional changes update baselines via `UPDATE_PUBLIC_API_BASELINE=1 ./eng/public-api.sh`.

```sh
./eng/public-docs.sh [version]
```

- Validates required `public-docs/` content, package README mapping, version snippets, and diagnostics coverage.

```sh
./eng/release-check.sh <version>
```

- Full release gate: `check`, `public-api`, `public-docs`, `package`, and `package-smoke` plus hygiene checks.

```sh
./eng/publish.sh
```

- Requires `NUGET_API_KEY`.
- Publishes `artifacts/nuget/*.nupkg` to nuget.org.
- Uses `--skip-duplicate`.

## Implementation Artifact

- [`.github/workflows/nuget-pack-and-publish.yml`](../../.github/workflows/nuget-pack-and-publish.yml)

# Authority

This document is authoritative for:
- packaging and release document routing
- packaging documentation entry points under `docs/engineering/`

This document is not authoritative for:
- release workflow semantics
- package version selection

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `docs/TBPS.md`

## Must Be Updated Together

When packaging document routing changes, review and update:
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`
- `.github/workflows/nuget-pack-and-publish.yml`
