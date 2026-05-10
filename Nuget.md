# NuGet packaging and publishing

This document summarizes practical best practices for packaging and publishing this repository to NuGet, and what was implemented in this PR.

## Best practices

- Keep package scope focused:
  - `Fletched.Core` for runtime/DSL APIs.
  - `Fletched.Roslyn` for source generation.
- Publish deterministic, reproducible packages from CI (`ContinuousIntegrationBuild=true`).
- Publish symbols (`.snupkg`) together with `.nupkg` for debugging support.
- Include repository metadata (`RepositoryUrl`, tags, description, readme) to improve package discoverability and trust.
- Drive public versioning from Git tags (`vX.Y.Z`).
- Use `--skip-duplicate` during publish so retries are safe.
- Keep NuGet API keys in GitHub Actions secrets, never in source.

## What is now automated in this repository

### 1) Package metadata and pack support

Both `src/Fletched.Core/Fletched.Core.csproj` and `src/Fletched.Roslyn/Fletched.Roslyn.csproj` now include:

- NuGet package metadata (`PackageId`, description, repository URLs, tags, readme).
- Symbol package generation (`IncludeSymbols`, `SymbolPackageFormat=snupkg`).
- `IsPackable=true`.

`README.md` from repository root is included in both packages as the package readme.

For `Fletched.Roslyn`, the netstandard2.0 output is additionally packed into `analyzers/dotnet/cs` so it is consumed as a Roslyn analyzer/source generator.

### 2) CI workflow for pack and publish

Workflow: `.github/workflows/nuget-pack-and-publish.yml`

- Triggers:
  - `push` tags matching `v*`
  - manual `workflow_dispatch` with explicit `version` input
- Behavior:
  - restore + build
  - pack `Fletched.Core` and `Fletched.Roslyn` with resolved version
  - upload packages as workflow artifacts
  - on tag push, publish packages to nuget.org if `NUGET_API_KEY` is configured

## Local commands

From repository root:

```bash
dotnet restore Fletched.slnx
dotnet build Fletched.slnx -c Release --no-restore
dotnet pack src/Fletched.Core/Fletched.Core.csproj -c Release --no-build -p:Version=0.1.0
dotnet pack src/Fletched.Roslyn/Fletched.Roslyn.csproj -c Release --no-build -p:Version=0.1.0
```

## Additional setup required outside this PR

These actions cannot be completed automatically by this PR and must be done in GitHub/NuGet UI:

1. Create and reserve package IDs on nuget.org:
   - `Fletched.Core`
   - `Fletched.Roslyn`
2. Create a nuget.org API key with push permission for those package IDs.
3. Add repository secret `NUGET_API_KEY` in GitHub Actions settings.
4. Publish by creating a tag like `v0.1.0` (or run the workflow manually with a version input).
