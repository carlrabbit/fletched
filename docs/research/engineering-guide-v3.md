# Engineering Guide V3

## Status

Authoritative engineering guide for the default .NET repository profile.

## Purpose

This guide defines an opinionated, AI-agent-friendly engineering setup for a professional repository.

The default stack is:

- .NET 10
- Microsoft Testing Platform (MTP)
- TUnit
- BenchmarkDotNet
- Bun
- Biome

Optional modules cover:

- Blazor
- Playwright
- TypeScript runtime/browser tooling
- NuGet packaging
- samples
- GitHub Pages

This guide defines the concrete engineering substrate:

- repository command contract;
- build, test, format, benchmark, package, release, and site commands;
- toolchain pinning;
- project layout;
- engineering building blocks;
- test classification;
- optional modules;
- agent validation expectations.

This guide is referenced by Project Setup Guide V4 through:

- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`
- `docs/guardrails/testing.md`
- `docs/guardrails/implementation.md`
- language-specific guardrails.

## Relationship to Project Setup Guide V4

Project Setup Guide V4 defines the repository knowledge model.

This guide defines the concrete engineering implementation profile.

In short:

```text
Project Setup Guide V4 tells the repository how to organize knowledge.
Engineering Guide V3 tells the repository how to build, test, validate, and package.
```

## README rule

Only the root `README.md` is allowed.

Do not create local README files in:

```text
eng/
samples/
site/
tools/
docs/**/
```

Use named documents under `docs/engineering/` instead:

```text
docs/ENGINEERING.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
docs/engineering/building-blocks.md
docs/engineering/samples.md
docs/engineering/site.md
docs/engineering/typescript-tools.md
docs/engineering/packaging.md
```

---

# 1. Core principles

## 1.1 Agent-executable over descriptive

Instructions must be executable or directly checkable.

Prefer:

```text
Run ./eng/check.sh and ensure it exits with code 0.
```

Avoid:

```text
Make sure the project looks clean.
```

## 1.2 One canonical command per workflow

Agents must not guess which command to run.

Each repository should expose these canonical commands:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
./eng/benchmark.sh
```

Optional modules may add:

```text
./eng/e2e.sh
./eng/frontend-check.sh
./eng/frontend-format.sh
./eng/package.sh
./eng/publish.sh
./eng/site-build.sh
./eng/samples.sh
```

## 1.3 Building blocks, not one giant template

Repositories start small and add capabilities by applying building blocks.

A block must define:

- block ID;
- purpose;
- when to apply;
- files to create or modify;
- packages or tools to add;
- commands to expose;
- validation command;
- done criteria.

## 1.4 Tooling must be pinned or explicit

The repository must pin or explicitly define:

- .NET SDK version through `global.json`;
- package versions through central package management;
- JavaScript/TypeScript tooling through `package.json`, `bun.lock`, and `biome.json` when the frontend/tooling module is used.

## 1.5 Optional means absent by default

Blazor, Playwright, TypeScript, NuGet packaging, samples, and GitHub Pages are optional modules.

Do not add them unless the repository needs them.

---

# 2. Required repository layout

A repository generated from the base blocks should use this layout:

```text
/
├─ .config/
│  └─ dotnet-tools.json
├─ .github/
│  ├─ workflows/
│  ├─ instructions/
│  └─ copilot-instructions.md
├─ artifacts/
│  └─ .gitkeep
├─ docs/
│  ├─ ENGINEERING.md
│  ├─ GUARDRAILS.md
│  ├─ WORKFLOWS.md
│  ├─ engineering/
│  │  ├─ dotnet.md
│  │  ├─ command-contract.md
│  │  ├─ building-blocks.md
│  │  ├─ optional-modules.md
│  │  ├─ packaging.md
│  │  ├─ samples.md
│  │  ├─ site.md
│  │  └─ typescript-tools.md
│  ├─ guardrails/
│  │  ├─ testing.md
│  │  ├─ implementation.md
│  │  └─ languages/
│  │     ├─ dotnet.md
│  │     └─ typescript.md
│  └─ workflows/
├─ eng/
│  ├─ restore.sh
│  ├─ build.sh
│  ├─ test.sh
│  ├─ format.sh
│  ├─ check.sh
│  ├─ benchmark.sh
│  ├─ common.sh
│  ├─ ci/
│  ├─ local/
│  └─ templates/
├─ src/
├─ tests/
│  ├─ unit/
│  └─ integration/
├─ benchmarks/
├─ samples/
├─ site/
├─ packages/
├─ tools/
├─ .editorconfig
├─ .gitignore
├─ AGENTS.md
├─ Directory.Build.props
├─ Directory.Packages.props
├─ NuGet.config
├─ global.json
└─ README.md
```

Optional modules may add:

```text
tests/e2e/
web/
package.json
bun.lock
biome.json
tsconfig.json
playwright.config.ts
```

## Folder ownership

| Path | Purpose |
|---|---|
| `src/` | Production source projects. |
| `tests/unit/` | Fast unit tests. No network, no database, no browser. |
| `tests/integration/` | Integration tests. May use databases, containers, test hosts, or real infrastructure substitutes. |
| `tests/e2e/` | Optional browser/system tests. Requires Playwright block. |
| `benchmarks/` | BenchmarkDotNet projects only. Not part of normal test execution. |
| `eng/` | Canonical repository commands and reusable engineering scripts. Agents must use these. |
| `eng/ci/` | CI-only helper scripts or workflow fragments. |
| `eng/local/` | Local developer utilities not required in CI. |
| `eng/templates/` | Reusable file templates for generators or agents. |
| `packages/` | Local NuGet packages or packaging output when package publishing is enabled. |
| `samples/` | Small runnable usage examples. No local README. Document in `docs/engineering/samples.md`. |
| `site/` | Optional static project website source for GitHub Pages. No local README. Document in `docs/engineering/site.md`. |
| `tools/` | Repository-local helper tools, generators, scripts, and development utilities. No local README. |
| `docs/` | Human- and agent-readable engineering documentation. |
| `artifacts/` | Local/generated outputs. Usually ignored except for `.gitkeep`. |

---

# 3. `eng/` folder design

The `eng/` folder is the canonical engineering entry point for both humans and AI agents.

The goal is:

- one stable location for engineering operations;
- minimal command ambiguity;
- reusable script composition;
- deterministic CI behavior;
- easy discoverability for agents.

## 3.1 Script layering

Use the following model:

```text
eng/
  common.sh         shared helpers
  restore.sh        canonical entry point
  build.sh          canonical entry point
  test.sh           canonical entry point
  format.sh         canonical entry point
  check.sh          canonical entry point
  benchmark.sh      canonical entry point

  ci/
    *.sh            CI-only helpers

  local/
    *.sh            optional local utilities

  templates/
    *               reusable templates
```

Top-level scripts are the public engineering API.

Agents and CI should prefer only these scripts:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
./eng/format.sh
./eng/benchmark.sh
```

Nested scripts are implementation details.

## 3.2 Canonical script rules

Top-level scripts should:

- be short;
- compose lower-level helpers;
- avoid duplicated logic;
- avoid hidden side effects;
- fail fast;
- use deterministic command ordering.

Prefer:

```sh
./eng/restore.sh
./eng/build.sh
./eng/test.sh
```

Avoid duplicated restore/build/test logic in CI YAML or issue instructions.

## 3.3 Shared helper example

`eng/common.sh`:

```sh
#!/usr/bin/env sh
set -eu

require_command() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Required command not found: $1" >&2
    exit 1
  }
}
```

## 3.4 Script extension rules

When adding a new capability:

- prefer extending an existing canonical script first;
- add a new top-level script only if the workflow is conceptually separate;
- avoid creating many overlapping commands.

Good examples:

```text
eng/e2e.sh
eng/package.sh
eng/publish.sh
eng/site-build.sh
eng/samples.sh
```

Bad examples:

```text
eng/test-all.sh
eng/test-fast.sh
eng/test-fast-no-db.sh
eng/test-local.sh
```

## 3.5 CI behavior

CI workflows should call `eng/` scripts instead of embedding repository logic directly.

Prefer:

```yaml
run: ./eng/check.sh
```

Avoid:

```yaml
run: |
  dotnet restore
  dotnet build
  dotnet test
```

## 3.6 Portability rules

Scripts should:

- use POSIX shell where practical;
- avoid unnecessary Bash-specific features;
- avoid machine-local assumptions;
- work in Linux containers, GitHub Actions, and ChromeOS Linux environments.

If PowerShell support is required, add parallel `.ps1` wrappers while preserving the same command contract.

---

# 4. Required command contract

## `eng/restore.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet restore

if [ -f package.json ]; then
  bun install --frozen-lockfile
fi
```

## `eng/build.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet build --no-restore
```

## `eng/test.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet test --no-build --configuration Debug --filter "TestCategory!=Slow&TestCategory!=E2E"
```

If the selected test framework or adapter does not use `TestCategory`, the repository must document and implement the equivalent filter.

## `eng/format.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet format

if [ -f biome.json ]; then
  bun run format
fi
```

## `eng/check.sh`

```sh
#!/usr/bin/env sh
set -eu

./eng/restore.sh
./eng/build.sh
./eng/test.sh

dotnet format --verify-no-changes

if [ -f biome.json ]; then
  bun run check
fi
```

## `eng/benchmark.sh`

```sh
#!/usr/bin/env sh
set -eu

dotnet run --configuration Release --project benchmarks/PROJECT_NAME.Benchmarks
```

Replace `PROJECT_NAME.Benchmarks` with the actual benchmark project name when the benchmark block is applied.

---

# 5. Building block overview

| Block | Name | Required | Purpose |
|---|---|---:|---|
| BB00 | Repository Base | Yes | Common repository skeleton and command contract. |
| BB01 | .NET Solution | Yes | Solution, source project, test project structure. |
| BB02 | Shared Build Configuration | Yes | `global.json`, `Directory.Build.props`, central package management. |
| BB03 | EditorConfig and C# Style | Yes | Opinionated formatting, analyzers, and style rules. |
| BB04 | MTP + TUnit Unit Tests | Yes | Fast unit testing foundation. |
| BB05 | Test Guardrails | Yes | Fast/slow/integration/e2e separation. |
| BB06 | BenchmarkDotNet | Recommended | Dedicated benchmark project. |
| BB07 | GitHub Actions CI | Recommended | Build/test/check automation. |
| BB08 | Agent Instructions | Yes | Repository-local operating instructions for AI agents. |
| BB09 | Bun + Biome | Optional | TypeScript/JavaScript tooling. |
| BB10 | Blazor Module | Optional | Blazor application project. |
| BB11 | Playwright E2E Module | Optional | Browser automation tests. |
| BB12 | TypeScript Runtime Tools | Optional | Self-authored TypeScript scripts/runtime utilities. |
| BB13 | Documentation Skeleton | Yes | Minimal docs required for maintainability. |
| BB14 | NuGet Packaging | Optional | NuGet package generation and publishing conventions. |
| BB15 | Samples | Optional | Runnable examples that demonstrate supported usage patterns. |
| BB16 | GitHub Copilot | Optional | Repository instructions for Copilot Chat, coding agent, and code review. |
| BB17 | OpenAI Codex | Optional | Repository instructions and command contracts optimized for Codex. |
| BB18 | GitHub Pages Website | Optional | Static project website deployed through GitHub Pages. |

---

# 6. BB00 — Repository Base

## Purpose

Create the repository skeleton and canonical engineering scripts.

## Apply when

Always.

## Files to create

```text
.gitignore
README.md
AGENTS.md
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
eng/benchmark.sh
artifacts/.gitkeep
docs/ENGINEERING.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
docs/guardrails/testing.md
docs/guardrails/implementation.md
```

Do not create local README files outside the root repository `README.md`.

## Required conventions

- Shell scripts in `eng/` are executable.
- Agents must use `eng/check.sh` before declaring work complete.
- `artifacts/` is used for generated local output and is ignored except for `.gitkeep`.
- `README.md` lists canonical commands and links to `docs/ENGINEERING.md`.

## Example `.gitignore`

```gitignore
# .NET
bin/
obj/
TestResults/
*.user
*.suo
*.rsuser

# BenchmarkDotNet
BenchmarkDotNet.Artifacts/

# Local artifacts
artifacts/*
!artifacts/.gitkeep

# Packages
packages/*
!packages/.gitkeep

# Bun / JS / TS
node_modules/
bun.lockb

# IDE
.vs/
.vscode/.ropeproject
.idea/

# OS
.DS_Store
Thumbs.db
```

If Bun creates `bun.lock`, commit it. If Bun creates `bun.lockb`, commit it only if this is the configured lockfile format for the selected Bun version.

## Validation

```sh
./eng/check.sh
```

## Done criteria

- Required files exist.
- Scripts are executable.
- Root `README.md` lists commands.
- No non-root README files exist.
- `eng/check.sh` exists, even if later blocks fill in its full behavior.

---

# 7. BB01 — .NET Solution

## Purpose

Create the .NET solution and project structure.

## Apply when

Always.

## Files/projects to create

Example for repository name `Example.Project`:

```text
Example.Project.slnx
src/Example.Project/Example.Project.csproj
tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
tests/integration/Example.Project.Tests.Integration/Example.Project.Tests.Integration.csproj
```

Use `.slnx` when supported by the installed .NET SDK and tooling. Use `.sln` only when required by external tooling.

## Example commands

```sh
dotnet new sln --name Example.Project
mkdir -p src tests/unit tests/integration benchmarks

dotnet new classlib --name Example.Project --output src/Example.Project

dotnet sln add src/Example.Project/Example.Project.csproj
```

If the project is an application instead of a library, replace `classlib` with the appropriate template.

## Required conventions

- Production projects live under `src/`.
- Unit test projects live under `tests/unit/`.
- Integration test projects live under `tests/integration/`.
- Project names include their role.
- Test projects reference the production projects they test.

## Validation

```sh
dotnet build
```

## Done criteria

- Solution exists.
- At least one production project exists.
- At least one unit test project exists.
- Solution builds.

---

# 8. BB02 — Shared Build Configuration

## Purpose

Centralize .NET SDK, build, analyzer, and package configuration.

## Apply when

Always.

## Files to create

```text
global.json
Directory.Build.props
Directory.Packages.props
.config/dotnet-tools.json
```

## Example `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

Update the SDK version to the exact .NET 10 SDK used by the repository.

## Example `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
  </PropertyGroup>

  <PropertyGroup Condition="$(MSBuildProjectName.Contains('.Tests.'))">
    <IsTestProject>true</IsTestProject>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

## Example `Directory.Packages.props`

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="TUnit" Version="0.0.0" />
    <PackageVersion Include="TUnit.Assertions" Version="0.0.0" />
    <PackageVersion Include="Microsoft.Testing.Platform" Version="0.0.0" />
    <PackageVersion Include="BenchmarkDotNet" Version="0.0.0" />
  </ItemGroup>
</Project>
```

Replace `0.0.0` with current approved versions during repository creation.

## Required conventions

- Package versions must be defined centrally.
- Project files must not contain inline package versions unless justified.
- SDK version must be pinned.
- Production code treats warnings as errors.

## Validation

```sh
dotnet restore
dotnet build
```

## Done criteria

- SDK is pinned.
- Central package management is enabled.
- Build properties apply to all projects.
- Restore and build succeed.

---

# 9. BB03 — EditorConfig and C# Style

## Purpose

Provide concrete formatting and analyzer rules so agents do not infer style from examples.

## Apply when

Always.

## File to create

```text
.editorconfig
```

## Example `.editorconfig`

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

[*.cs]
indent_size = 4

# C# language style
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
csharp_style_expression_bodied_methods = false:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion
csharp_style_prefer_null_check_over_type_check = true:suggestion
csharp_prefer_braces = true:warning
csharp_style_prefer_primary_constructors = true:suggestion

# .NET style
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion

# Analyzer severity baseline
dotnet_analyzer_diagnostic.category-Style.severity = warning
dotnet_analyzer_diagnostic.category-Performance.severity = warning
dotnet_analyzer_diagnostic.category-Reliability.severity = warning
dotnet_analyzer_diagnostic.category-Security.severity = warning

[*.{json,yml,yaml,md,ts,tsx,js,jsx,css,html}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

## Required conventions

- `.editorconfig` is authoritative for C# style.
- Agents must run `dotnet format --verify-no-changes` before completion as part of `eng/check.sh`.
- Do not rely on IDE defaults.

## Validation

```sh
dotnet format --verify-no-changes
```

## Done criteria

- `.editorconfig` exists.
- `dotnet format --verify-no-changes` passes.

---

# 10. BB04 — MTP + TUnit Unit Tests

## Purpose

Create the default test foundation using Microsoft Testing Platform and TUnit.

## Apply when

Always.

## Files/projects to create or modify

```text
tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
```

## Example test project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" />
    <PackageReference Include="TUnit.Assertions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../src/Example.Project/Example.Project.csproj" />
  </ItemGroup>
</Project>
```

## Example test

```csharp
using TUnit.Assertions;
using TUnit.Core;

namespace Example.Project.Tests.Unit;

public sealed class ExampleTests
{
    [Test]
    public async Task Example_should_be_true()
    {
        var value = true;

        await Assert.That(value).IsTrue();
    }
}
```

## Required conventions

- Unit tests must be fast.
- Unit tests must not use network, real dat
