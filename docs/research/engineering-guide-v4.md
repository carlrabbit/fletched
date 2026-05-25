# Engineering Guide V4

## Status

Authoritative engineering guide for the default .NET repository profile.

## Purpose

This guide defines an opinionated, AI-agent-friendly engineering setup for professional .NET repositories that may publish NuGet packages and public documentation.

Version 4 extends Engineering Guide V3 with:

- public documentation building block;
- public documentation validation command;
- package smoke testing;
- public API validation;
- release readiness command;
- user-facing documentation checks for NuGet libraries preparing for version 1.0;
- upgrade instructions from V3 to V4.

The default stack remains:

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
- GitHub Copilot
- OpenAI Codex
- GitHub Pages
- public documentation
- release readiness

This guide defines the concrete engineering substrate:

- repository command contract;
- build, test, format, benchmark, package, release, and documentation validation commands;
- toolchain pinning;
- project layout;
- engineering building blocks;
- test classification;
- package validation;
- public API validation;
- optional modules;
- agent validation expectations.

## Relationship to Project Setup Guide V5

Project Setup Guide V5 defines the repository knowledge model.

This guide defines the concrete engineering implementation profile.

```text
Project Setup Guide V5 tells the repository how to organize knowledge.
Engineering Guide V4 tells the repository how to build, test, validate, package, document, and release.
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
public-docs/**/
```

Use named documents instead:

```text
docs/ENGINEERING.md
docs/PUBLIC-DOCS.md
docs/engineering/command-contract.md
docs/engineering/samples.md
docs/engineering/site.md
docs/engineering/typescript-tools.md
public-docs/getting-started.md
public-docs/nuget/package-readme.md
public-docs/samples/getting-started.md
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

Package and release capable repositories may add:

```text
./eng/package.sh
./eng/publish.sh
./eng/package-smoke.sh
./eng/public-api.sh
./eng/public-docs.sh
./eng/release-check.sh
```

Optional modules may add:

```text
./eng/e2e.sh
./eng/frontend-check.sh
./eng/frontend-format.sh
./eng/samples.sh
./eng/site-build.sh
```

## 1.3 Fast default, explicit release validation

`./eng/check.sh` is the fast development gate.

It should stay safe for local development, CI pull requests, and AI-agent validation.

`./eng/release-check.sh <version>` is the release gate.

It may run package validation, smoke tests, samples, public API checks, and public documentation checks.

## 1.4 Building blocks, not one giant template

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

## 1.5 Tooling must be pinned or explicit

The repository must pin or explicitly define:

- .NET SDK version through `global.json`;
- package versions through central package management;
- JavaScript/TypeScript tooling through `package.json`, `bun.lock`, and `biome.json` when the frontend/tooling module is used.

## 1.6 Optional means absent by default

Blazor, Playwright, TypeScript, NuGet packaging, samples, GitHub Pages, public documentation, and release-readiness scripts are applied only when the repository needs them.

For NuGet libraries preparing for version 1.0, public documentation and release readiness are required.

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
│  ├─ PUBLIC-DOCS.md
│  ├─ WORKFLOWS.md
│  ├─ engineering/
│  │  ├─ dotnet.md
│  │  ├─ command-contract.md
│  │  ├─ building-blocks.md
│  │  ├─ optional-modules.md
│  │  ├─ packaging.md
│  │  ├─ public-documentation.md
│  │  ├─ release-readiness.md
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
├─ public-docs/
│  ├─ getting-started.md
│  ├─ installation.md
│  ├─ concepts.md
│  ├─ packages.md
│  ├─ samples.md
│  ├─ diagnostics.md
│  ├─ versioning.md
│  ├─ release-notes.md
│  ├─ api/
│  ├─ diagnostics/
│  ├─ guides/
│  ├─ nuget/
│  ├─ samples/
│  └─ website/
├─ eng/
│  ├─ restore.sh
│  ├─ build.sh
│  ├─ test.sh
│  ├─ format.sh
│  ├─ check.sh
│  ├─ benchmark.sh
│  ├─ package.sh
│  ├─ publish.sh
│  ├─ package-smoke.sh
│  ├─ public-api.sh
│  ├─ public-docs.sh
│  ├─ release-check.sh
│  ├─ common.sh
│  ├─ ci/
│  ├─ local/
│  └─ templates/
├─ src/
├─ tests/
│  ├─ unit/
│  ├─ integration/
│  └─ package-smoke/
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
| `tests/package-smoke/` | Tests that consume packed packages from local artifacts. Required for NuGet release readiness. |
| `tests/e2e/` | Optional browser/system tests. Requires Playwright block. |
| `benchmarks/` | BenchmarkDotNet projects only. Not part of normal test execution. |
| `eng/` | Canonical repository commands and reusable engineering scripts. Agents must use these. |
| `packages/` | Local NuGet packages or packaging output when package publishing is enabled. |
| `samples/` | Small runnable examples. No local README. Document in `docs/engineering/samples.md` and `public-docs/samples/`. |
| `site/` | Optional static project website source or generated site shell. No local README. Document in `docs/engineering/site.md`. |
| `tools/` | Repository-local helper tools, generators, scripts, and development utilities. No local README. |
| `docs/` | Internal authoritative engineering and semantic documentation. |
| `public-docs/` | Public consumer-facing documentation source. |
| `artifacts/` | Local/generated outputs. Usually ignored except for `.gitkeep`. |

---

# 3. `eng/` folder design

The `eng/` folder is the canonical engineering entry point for both humans and AI agents.

Top-level scripts are the public engineering API.

Nested scripts are implementation details.

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

## Portability rules

Scripts should:

- use POSIX shell where practical;
- avoid unnecessary Bash-specific features;
- avoid machine-local assumptions;
- work in Linux containers, GitHub Actions, and ChromeOS Linux environments;
- fail clearly when required tools or secrets are missing.

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

dotnet test --no-build --configuration Debug --filter "TestCategory!=Slow&TestCategory!=E2E&TestCategory!=PackageSmoke"
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

## `eng/package.sh`

```sh
#!/usr/bin/env sh
set -eu

VERSION="${1:?version is required}"

dotnet pack --configuration Release --no-build \
  -p:PackageVersion="$VERSION" \
  -p:ContinuousIntegrationBuild=true \
  --output ./artifacts/nuget
```

## `eng/package-smoke.sh`

```sh
#!/usr/bin/env sh
set -eu

VERSION="${1:?version is required}"

# Repository-specific implementation:
# 1. create temporary consumer project
# 2. add artifacts/nuget as local package source
# 3. install package(s)
# 4. build and run the consumer
# 5. verify source generator/analyzer/package behavior where applicable

dotnet test tests/package-smoke --configuration Release \
  -p:PackageSmokeVersion="$VERSION"
```

## `eng/public-api.sh`

```sh
#!/usr/bin/env sh
set -eu

# Repository-specific implementation.
# Typical options:
# - PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt validation
# - Verify snapshots
# - generated API diff
# - package public surface validation

dotnet build --configuration Release --no-restore
```

## `eng/public-docs.sh`

```sh
#!/usr/bin/env sh
set -eu

required_files="
README.md
docs/PUBLIC-DOCS.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
"

for file in $required_files; do
  if [ ! -f "$file" ]; then
    echo "Missing public documentation file: $file" >&2
    exit 1
  fi
done
```

## `eng/release-check.sh`

```sh
#!/usr/bin/env sh
set -eu

VERSION="${1:?version is required}"

./eng/check.sh
dotnet build --configuration Release
./eng/package.sh "$VERSION"
./eng/package-smoke.sh "$VERSION"
./eng/samples.sh
./eng/public-api.sh
./eng/public-docs.sh
```

If a repository does not use samples, `eng/samples.sh` may be omitted, but NuGet package repositories should strongly prefer samples before version 1.0.

---

# 5. Building block overview

| Block | Name | Required | Purpose |
|---|---|---:|---|
| BB00 | Repository Base | Yes | Common repository skeleton and command contract. |
| BB01 | .NET Solution | Yes | Solution, source project, test project structure. |
| BB02 | Shared Build Configuration | Yes | `global.json`, `Directory.Build.props`, central package management. |
| BB03 | EditorConfig and C# Style | Yes | Opinionated formatting, analyzers, and style rules. |
| BB04 | MTP + TUnit Unit Tests | Yes | Fast unit testing foundation. |
| BB05 | Test Guardrails | Yes | Fast/slow/integration/e2e/package-smoke separation. |
| BB06 | BenchmarkDotNet | Recommended | Dedicated benchmark project. |
| BB07 | GitHub Actions CI | Recommended | Build/test/check automation. |
| BB08 | Agent Instructions | Yes | Repository-local operating instructions for AI agents. |
| BB09 | Bun + Biome | Optional | TypeScript/JavaScript tooling. |
| BB10 | Blazor Module | Optional | Blazor application project. |
| BB11 | Playwright E2E Module | Optional | Browser automation tests. |
| BB12 | TypeScript Runtime Tools | Optional | Self-authored TypeScript scripts/runtime utilities. |
| BB13 | Documentation Skeleton | Yes | Minimal docs required for maintainability. |
| BB14 | NuGet Packaging | Required for NuGet libraries | NuGet package generation and publishing conventions. |
| BB15 | Samples | Recommended for public packages | Runnable examples that demonstrate supported usage patterns. |
| BB16 | GitHub Copilot | Optional | Repository instructions for Copilot. |
| BB17 | OpenAI Codex | Optional | Repository instructions optimized for Codex. |
| BB18 | GitHub Pages Website | Optional | Static project website deployed through GitHub Pages. |
| BB19 | Public Documentation | Required for public packages before 1.0 | Consumer-facing documentation source and validation. |
| BB20 | Release Readiness | Required for public packages before 1.0 | Release gate, package smoke tests, public API checks, public docs checks. |

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
docs/GUARDRAILS.md
docs/guardrails/testing.md
docs/guardrails/implementation.md
```

Do not create local README files outside the root repository `README.md`.

## Required conventions

- Shell scripts in `eng/` are executable.
- Agents must use `eng/check.sh` before declaring implementation work complete.
- `artifacts/` is used for generated local output and is ignored except for `.gitkeep`.
- `README.md` lists canonical commands and links to `docs/ENGINEERING.md`.

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

## Required conventions

- Production projects live under `src/`.
- Unit test projects live under `tests/unit/`.
- Integration test projects live under `tests/integration/`.
- Package smoke tests live under `tests/package-smoke/` when BB20 is applied.
- Project names include their role.
- Test projects reference the production projects they test, except package smoke tests, which should consume packed packages.

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

## Required conventions

- Package versions must be defined centrally.
- Project files must not contain inline package versions unless justified.
- SDK version must be pinned.
- Production code treats warnings as errors.
- Package repositories should generate XML documentation for public API projects.

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

## Required conventions

- Unit tests must be fast.
- Unit tests must not use network, real database, browser automation, or sleeps.
- Test names should describe observable behavior.
- Tests must be deterministic.
- Avoid broad generated tests that assert implementation details.

## Validation

```sh
dotnet test tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
```

## Done criteria

- At least one unit test exists.
- Unit tests run through `dotnet test`.
- Unit test project participates in `eng/test.sh`.

---

# 11. BB05 — Test Guardrails

## Purpose

Prevent agents from creating slow, broad, or operationally expensive tests by default.

## Apply when

Always.

## Test categories

| Category | Default run | Description |
|---|---:|---|
| Unit | Yes | Fast, isolated tests. |
| Integration | Optional in local loop | Uses database, filesystem, test server, or containers. |
| PackageSmoke | No | Tests packed packages as a real consumer. Release gate only. |
| Slow | No | Expensive tests not suitable for normal agent iterations. |
| E2E | No | Browser/system tests. |
| Benchmark | Never via test command | BenchmarkDotNet only. |

## Required rules

- `eng/test.sh` runs fast tests only.
- Integration tests must have their own command or documented filter.
- Package smoke tests must not run in `eng/test.sh`.
- E2E tests must not run as part of normal `dotnet test` unless explicitly requested.
- Benchmarks must never be represented as tests.
- Agents must not add sleeps to tests unless unavoidable and documented.
- Agents must not create tests that depend on test execution order.

## Validation

```sh
./eng/test.sh
```

## Done criteria

- Test categories are documented.
- Default test command excludes slow/e2e/package-smoke work.
- Benchmark policy is documented.
- Release validation has a separate path.

---

# 12. BB06 — BenchmarkDotNet

## Purpose

Add performance measurement without polluting the test suite.

## Apply when

Recommended for libraries, algorithms, serialization, parsers, graph processing, data structures, or performance-sensitive services.

## Required conventions

- Benchmarks run in Release configuration.
- Benchmarks are not part of `eng/test.sh`.
- Benchmark output is written to ignored artifacts.
- Benchmark projects must not contain correctness assertions as their primary purpose.

## Validation

```sh
./eng/benchmark.sh
```

## Done criteria

- Benchmark project exists.
- Benchmark command is documented.
- Normal test command does not execute benchmarks.

---

# 13. BB07 — GitHub Actions CI

## Purpose

Provide hosted validation for build, test, formatting, and optional frontend checks.

## Apply when

Recommended for every repository hosted on GitHub.

## Files to create

```text
.github/workflows/ci.yml
docs/workflows/build.md
docs/workflows/test-short.md
```

## Required conventions

- CI must use the same commands as local development.
- CI must not invent separate build logic.
- Optional module setup must be conditional.
- Workflow intent must be documented in `docs/workflows/`.

## Validation

CI passes on a clean checkout.

## Done criteria

- Workflow exists.
- Workflow uses `./eng/check.sh`.
- Workflow supports repositories with and without Bun tooling.
- Workflow documentation exists.

---

# 14. BB08 — Agent Instructions

## Purpose

Provide local operating rules for AI agents.

## Apply when

Always.

## Files to create or modify

```text
AGENTS.md
.github/copilot-instructions.md
```

## Required `AGENTS.md` rules

```md
# Agent Instructions

## Required Reading

- docs/TERMINOLOGY.md
- docs/GUARDRAILS.md
- docs/ENGINEERING.md
- docs/PUBLIC-DOCS.md when public-facing behavior changes
- docs/engineering/dotnet.md
- docs/TBPS.md
- relevant specs
- relevant guardrails
- relevant workflows

## Required Workflow

Before completing implementation work, run:

```sh
./eng/check.sh
```

Before completing release work, run:

```sh
./eng/release-check.sh <version>
```

## Repository Rules

- Do not add README files outside the root README.md.
- Use eng/ scripts instead of inventing commands.
- Do not add new root-level folders without updating documentation.
- Do not add package versions directly to project files. Use Directory.Packages.props.
- Do not use npm. Use Bun for JavaScript/TypeScript tooling.
- Do not add ESLint or Prettier. Use Biome unless explicitly instructed otherwise.
- Do not add slow tests to the default test path.
- Do not run benchmarks during normal validation.
- Do not run package smoke tests during normal validation.
- Do not introduce Playwright unless the Playwright building block is applied.
- Update public-docs/ when public behavior changes.
- Prefer small, vertical changes over broad rewrites.
- Preserve the command contract under eng/.
```

## Done criteria

- `AGENTS.md` exists.
- Rules match installed building blocks.
- No command contradicts `README.md`, `docs/ENGINEERING.md`, `docs/PUBLIC-DOCS.md`, or guardrails.

---

# 15. BB09 — Bun + Biome

## Purpose

Add JavaScript/TypeScript tooling with minimal moving parts.

## Apply when

Apply when the repository needs:

- TypeScript runtime scripts;
- browser TypeScript assets;
- Blazor JavaScript interop source files;
- Playwright tests;
- frontend linting/formatting;
- documentation tooling that explicitly requires TypeScript.

Do not apply for pure .NET repositories.

## Required conventions

- Use Bun, not npm.
- Use Biome, not ESLint/Prettier.
- Commit the Bun lockfile.
- Keep TypeScript optional and scoped.
- Prefer self-authored TypeScript over framework-heavy build chains.

## Validation

```sh
bun install --frozen-lockfile
bun run check
```

## Done criteria

- Bun install succeeds.
- Biome check passes.
- TypeScript typecheck passes.
- `eng/check.sh` invokes `bun run check` when `biome.json` exists.

---

# 16. BB10 — Blazor Module

## Purpose

Add a Blazor application while keeping frontend tooling optional.

## Apply when

Apply when the repository needs a web UI implemented primarily in .NET/Blazor.

## Required conventions

- Blazor is the primary UI framework when this block is applied.
- Do not add a separate SPA framework unless explicitly required.
- JavaScript interop code should be small, typed, and isolated.
- If TypeScript is used for browser interop, also apply BB09.
- If browser tests are needed, also apply BB11.
- Document runtime usage in `docs/engineering/dotnet.md` or a named engineering document, not in a local README.

## Validation

```sh
dotnet build src/Example.Project.Web/Example.Project.Web.csproj
```

## Done criteria

- Blazor project builds.
- Root README links to the public or engineering documentation as appropriate.
- Optional TypeScript usage is documented if present.

---

# 17. BB11 — Playwright E2E Module

## Purpose

Add explicit browser/system testing.

## Apply when

Apply only when the repository contains a UI or browser-observable workflow that requires real browser testing.

## Required conventions

- E2E tests are opt-in.
- E2E tests must not run in `eng/test.sh`.
- E2E tests run through `eng/e2e.sh`.
- E2E tests should be few and high-value.
- Avoid testing every UI detail through Playwright.

## Validation

```sh
./eng/e2e.sh
```

## Done criteria

- E2E command exists.
- E2E tests are excluded from default test command.
- Browser installation/setup is documented in `docs/engineering/`.

---

# 18. BB12 — TypeScript Runtime Tools

## Purpose

Support self-authored TypeScript scripts or runtime utilities without adopting a full frontend stack.

## Apply when

Apply when the repository needs TypeScript for:

- graph layout processing;
- code generation;
- JSON/schema transformations;
- browser-adjacent asset generation;
- local development utilities.

## Files to create

```text
tools/ts/
docs/engineering/typescript-tools.md
```

Also apply BB09.

Do not create `tools/ts/README.md`.

## Required conventions

- TypeScript tools must have explicit inputs and outputs.
- Prefer pure functions and file-based boundaries.
- Avoid hidden global state.
- Avoid long-running watchers unless explicitly needed.
- Heavy libraries such as graph layout engines must be isolated behind small adapter modules.

## Validation

```sh
bun run check
```

## Done criteria

- TypeScript tool code is typechecked.
- Tool scripts are documented in `docs/engineering/typescript-tools.md`.
- Heavy dependencies are isolated behind adapters.

---

# 19. BB13 — Documentation Skeleton

## Purpose

Provide the minimum documentation needed for maintainable human and agent work.

## Apply when

Always.

## Files to create

```text
README.md
docs/ENGINEERING.md
docs/GUARDRAILS.md
docs/guardrails/testing.md
docs/guardrails/implementation.md
docs/engineering/dotnet.md
docs/engineering/command-contract.md
AGENTS.md
.github/copilot-instructions.md
```

If the repository publishes a package, public API, tool, source generator, CLI, website, or user-facing artifact, also apply BB19 Public Documentation.

## Done criteria

- Required docs exist.
- Docs do not contradict scripts.
- Docs mention optional modules only when installed or clearly marked optional.
- No non-root README files exist.

---

# 20. BB14 — NuGet Packaging

## Purpose

Provide a standardized NuGet packaging and publishing setup.

## Apply when

Apply when the repository produces reusable libraries distributed as NuGet packages.

For NuGet libraries preparing for version 1.0, this block is required.

## Files to create or modify

```text
NuGet.config
docs/engineering/packaging.md
eng/package.sh
eng/publish.sh
packages/.gitkeep
```

## Required conventions

- Packages are generated only through `eng/package.sh`.
- Publishing is explicit and never part of normal CI validation.
- Package output goes to `artifacts/nuget` or `packages/`, as documented.
- Package metadata should be centralized where practical.
- Public packages should include source link and symbol packages.
- Public packages should include a package README.
- Repositories producing multiple packages should document package ownership.

## Package README

NuGet package README source should live under:

```text
public-docs/nuget/
```

For a single-package repository:

```text
public-docs/nuget/package-readme.md
```

For a multi-package repository:

```text
public-docs/nuget/<PackageId>.md
```

## Validation

```sh
./eng/package.sh <version>
```

## Done criteria

- Package generation succeeds.
- Package output exists under the documented artifact folder.
- Packaging commands are documented.
- Publishing requires explicit credentials.
- Public package README content exists.

---

# 21. BB15 — Samples

## Purpose

Add a `samples/` area for small, runnable examples that demonstrate how the repository is intended to be used.

Samples are executable documentation.

## Apply when

Apply when the repository exposes:

- a reusable library;
- a NuGet package;
- public APIs;
- a Blazor component;
- a tool or framework extension;
- a non-trivial integration pattern.

For NuGet libraries preparing for version 1.0, samples are strongly recommended.

## Required conventions

- Samples must be small.
- Samples must compile.
- Samples must reference public packages or public APIs intentionally.
- Release-oriented samples should consume packed packages when practical.
- Samples must not contain hidden test assertions.
- Samples must not become a second application architecture.
- Samples must not be required for normal production builds unless explicitly documented.
- Samples should prefer clarity over completeness.
- Sample documentation lives in `docs/engineering/samples.md` and `public-docs/samples/`.
- Do not create `samples/README.md`.

## Optional command

```text
eng/samples.sh
```

## Validation

```sh
./eng/samples.sh
```

## Done criteria

- `docs/engineering/samples.md` exists.
- Public sample docs exist under `public-docs/samples/`.
- Each sample has a documented purpose.
- Each sample builds or has documented prerequisites.
- Samples do not replace tests.
- Samples do not contain production-only secrets or local machine assumptions.

---

# 22. BB16 — GitHub Copilot

## Purpose

Add repository-specific instructions for GitHub Copilot.

## Apply when

Apply when GitHub Copilot Chat, Copilot code review, Copilot coding agent, or Copilot-assisted IDE workflows are expected.

## Files to create

```text
.github/copilot-instructions.md
.github/instructions/
```

## Required conventions

- Repository-wide Copilot instructions live in `.github/copilot-instructions.md`.
- Path-specific instructions live under `.github/instructions/` when needed.
- Copilot instructions must point to `AGENTS.md` for shared agent rules.
- Copilot instructions must not duplicate the whole setup guide.
- Copilot instructions should be short, operational, and command-oriented.
- Public documentation impact must be mentioned when public behavior changes.

## Validation

```sh
./eng/check.sh
```

## Done criteria

- `.github/copilot-instructions.md` exists.
- Instructions reference `AGENTS.md`, `docs/ENGINEERING.md`, and `docs/GUARDRAILS.md`.
- Instructions include the canonical validation command.
- Path-specific instruction files exist only when they reduce ambiguity.

---

# 23. BB17 — OpenAI Codex

## Purpose

Optimize the repository for OpenAI Codex local and cloud workflows.

Codex should be able to enter the repository, read the instructions, understand the command contract, make changes, validate them, and report completion without guessing.

## Apply when

Apply when OpenAI Codex CLI, Codex IDE extension, Codex cloud tasks, or Codex code review are expected.

## Required conventions

- `AGENTS.md` is the primary Codex instruction file.
- `AGENTS.md` must be concise and operational.
- The first implementation validation command must be `./eng/check.sh`.
- The release validation command must be `./eng/release-check.sh <version>` when release work is requested.
- Long-running or expensive commands must be explicitly marked.
- Cloud-safe and local-only workflows must be distinguished.
- Instructions must state what completion means.

## Expensive commands

Do not run these unless explicitly requested:

```sh
./eng/benchmark.sh
./eng/e2e.sh
./eng/package-smoke.sh <version>
./eng/release-check.sh <version>
./eng/publish.sh
```

## Done criteria

- `AGENTS.md` contains Codex-safe workflow rules.
- Expensive commands are marked.
- `eng/check.sh` is the canonical implementation completion gate.
- `eng/release-check.sh` is the canonical release completion gate.
- Codex can validate a clean checkout without local secrets.

---

# 24. BB18 — GitHub Pages Website

## Purpose

Add a static project website hosted through GitHub Pages.

The website is a publication mechanism. It is not the only source of public documentation.

## Apply when

Apply when the project needs a public or internal static website.

Do not apply merely to store ordinary repository documentation.

## Files to create

```text
site/
site/index.html
site/assets/
.github/workflows/pages.yml
eng/site-build.sh
docs/engineering/site.md
docs/workflows/pages.md
public-docs/website/
```

Do not create `site/README.md`.

## Required conventions

- Website source or shell lives under `site/`.
- Authoritative public documentation content lives under `public-docs/`.
- Website output must be generated into `artifacts/site/` or another ignored build output folder.
- Publishing is performed by GitHub Actions.
- The Pages workflow must not publish on every pull request.
- The site build must not require secrets.
- The site must not be required for normal backend build/test unless explicitly selected.

## Validation

```sh
./eng/site-build.sh
```

## Done criteria

- `site/` exists.
- `eng/site-build.sh` creates static output.
- Pages workflow uploads and deploys static output.
- Website publishing is separate from normal validation.
- Site documentation lives in `docs/engineering/site.md`.
- Website content is synchronized with `public-docs/website/` or generated from `public-docs/`.

---

# 25. BB19 — Public Documentation

## Purpose

Add public user-facing documentation for packages, APIs, diagnostics, samples, release notes, and website publication.

## Apply when

Apply when the repository produces:

- a NuGet package;
- a public API;
- a source generator;
- a CLI;
- a web API;
- a public website;
- any externally consumed artifact.

For NuGet libraries preparing for version 1.0, this block is required.

## Files to create

```text
docs/PUBLIC-DOCS.md
docs/tbps/public-documentation-update.md
docs/engineering/public-documentation.md
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/api/
public-docs/diagnostics/
public-docs/guides/
public-docs/nuget/
public-docs/samples/
public-docs/website/
eng/public-docs.sh
```

## Required conventions

- Public docs live in `public-docs/`.
- Internal docs live in `docs/`.
- `docs/PUBLIC-DOCS.md` is the authority for public documentation synchronization.
- Do not create README files under `public-docs/`.
- The root `README.md` remains the first-contact user document.
- NuGet package README content must come from `public-docs/nuget/`.
- Diagnostics must be documented by diagnostic ID.
- Samples must be documented from the user perspective.
- Release notes must describe externally visible changes.
- Public docs must use canonical terminology.
- Public docs must not copy internal specs verbatim.

## Minimal public docs for NuGet libraries

```text
public-docs/getting-started.md
public-docs/installation.md
public-docs/concepts.md
public-docs/packages.md
public-docs/samples.md
public-docs/diagnostics.md
public-docs/versioning.md
public-docs/release-notes.md
public-docs/nuget/package-readme.md
```

## Validation

```sh
./eng/public-docs.sh
```

## Done criteria

- Public docs exist.
- Public docs are linked from README.md.
- NuGet README content is current.
- Diagnostics reference is current if diagnostics exist.
- Samples documentation matches runnable samples.
- Release checklist references public docs.
- No README files exist under `public-docs/`.

---

# 26. BB20 — Release Readiness

## Purpose

Add release-oriented validation that proves the repository is ready to publish public artifacts.

This block separates the fast development gate from the release gate.

## Apply when

Apply when the repository publishes:

- NuGet packages;
- public APIs;
- source generators;
- analyzers;
- tools;
- websites;
- release notes.

For NuGet libraries preparing for version 1.0, this block is required.

## Files to create

```text
eng/release-check.sh
eng/package-smoke.sh
eng/public-api.sh
docs/engineering/release-readiness.md
docs/workflows/release-check.md
tests/package-smoke/
```

## Required validation flow

```text
1. ./eng/check.sh
2. dotnet build -c Release
3. ./eng/package.sh <version>
4. ./eng/package-smoke.sh <version>
5. ./eng/samples.sh
6. ./eng/public-api.sh
7. ./eng/public-docs.sh
```

## Package smoke test requirements

Packed-package smoke tests should verify:

- package can be installed from local artifacts;
- clean consumer project can restore;
- clean consumer project can build;
- public API can be used from the package;
- source generator/analyzer package loads if applicable;
- analyzer dependencies do not leak unexpectedly;
- generated code or runtime output behaves as documented;
- at least one invalid usage produces the expected diagnostic when diagnostics are part of the public contract.

## Public API baseline requirements

For public libraries, track public API intentionally.

Recommended options:

```text
PublicAPI.Shipped.txt
PublicAPI.Unshipped.txt
```

or an equivalent snapshot-based public API check.

Rules:

- Public API changes must be explicit.
- Accidental public types must be prevented.
- Internal implementation details must remain internal unless intentionally supported.
- Breaking changes must be listed in release notes.

## Release docs requirements

Before release:

- README is user-first.
- Package README is current.
- Installation docs are current.
- Getting started docs are current.
- Public API docs are current.
- Diagnostics docs are current.
- Samples docs are current.
- Versioning policy is current.
- Release notes are current.

## Validation

```sh
./eng/release-check.sh <version>
```

## Done criteria

- Release check command exists.
- Release check validates packages, public API, samples, and public docs.
- Release check does not publish.
- Publish remains explicit.
- Release workflow can call release check before publishing.

---

# 27. Recommended base setup sequence

For a normal professional .NET library or service repository, apply:

```text
BB00 Repository Base
BB01 .NET Solution
BB02 Shared Build Configuration
BB03 EditorConfig and C# Style
BB04 MTP + TUnit Unit Tests
BB05 Test Guardrails
BB06 BenchmarkDotNet
BB07 GitHub Actions CI
BB08 Agent Instructions
BB13 Documentation Skeleton
```

For public NuGet libraries, additionally apply:

```text
BB14 NuGet Packaging
BB15 Samples
BB19 Public Documentation
BB20 Release Readiness
```

For GitHub Copilot support, additionally apply:

```text
BB16 GitHub Copilot
```

For OpenAI Codex support, additionally apply:

```text
BB17 OpenAI Codex
```

For a GitHub Pages project website, additionally apply:

```text
BB18 GitHub Pages Website
```

For TypeScript tooling, additionally apply:

```text
BB09 Bun + Biome
BB12 TypeScript Runtime Tools
```

For Blazor UI, additionally apply:

```text
BB10 Blazor Module
```

For browser tests, additionally apply:

```text
BB11 Playwright E2E Module
```

---

# 28. Agent repository creation workflow

An AI agent creating a new repository must follow this workflow:

1. Determine required blocks.
2. Create the repository skeleton.
3. Create .NET solution and projects.
4. Add shared build configuration.
5. Add `.editorconfig`.
6. Add TUnit/MTP test projects.
7. Add benchmark project if selected.
8. Add optional Bun/Biome tooling only if selected.
9. Add optional Blazor/Playwright modules only if selected.
10. Add packaging if selected.
11. Add samples if selected.
12. Add public documentation if selected or required.
13. Add release readiness if selected or required.
14. Add docs and agent instructions.
15. Confirm no non-root README files exist.
16. Run `./eng/check.sh`.
17. If release-ready package work was requested, run `./eng/release-check.sh <version>`.
18. Fix all failures.
19. Report what blocks were applied and what validation passed.

Agents must not declare the repository complete until the applicable validation command succeeds or the failure is explicitly documented with the exact failing command and output summary.

---

# 29. Completion checklist

A generated base repository is complete only when all applicable items are true:

- [ ] `global.json` exists and pins .NET 10 SDK.
- [ ] `Directory.Build.props` exists.
- [ ] `Directory.Packages.props` exists.
- [ ] `.editorconfig` exists.
- [ ] `AGENTS.md` exists.
- [ ] Root `README.md` lists canonical commands.
- [ ] No non-root README files exist.
- [ ] `docs/ENGINEERING.md` exists.
- [ ] `docs/GUARDRAILS.md` exists.
- [ ] `docs/guardrails/testing.md` exists.
- [ ] `docs/guardrails/implementation.md` exists.
- [ ] `docs/engineering/dotnet.md` exists.
- [ ] `eng/` scripts exist and are executable.
- [ ] Solution builds.
- [ ] Unit tests run through MTP/TUnit.
- [ ] Default test command excludes slow/e2e/package-smoke tests.
- [ ] Benchmark project exists if BB06 was selected.
- [ ] Bun/Biome files exist only if BB09 was selected.
- [ ] Blazor project exists only if BB10 was selected.
- [ ] Playwright setup exists only if BB11 was selected.
- [ ] Samples exist only if BB15 was selected.
- [ ] GitHub Copilot instructions exist only if BB16 was selected.
- [ ] Codex-specific guidance exists only if BB17 was selected.
- [ ] GitHub Pages website exists only if BB18 was selected.
- [ ] Public documentation exists if BB19 was selected.
- [ ] Release readiness commands exist if BB20 was selected.
- [ ] `./eng/check.sh` succeeds.
- [ ] `./eng/release-check.sh <version>` succeeds when release readiness applies.

---

# 30. Upgrade guide from Engineering Guide V3

## 1. Add public documentation block

Create:

```text
docs/PUBLIC-DOCS.md
docs/engineering/public-documentation.md
public-docs/
eng/public-docs.sh
```

Do not create `public-docs/README.md`.

## 2. Add release readiness block

Create:

```text
docs/engineering/release-readiness.md
docs/workflows/release-check.md
eng/release-check.sh
eng/package-smoke.sh
eng/public-api.sh
tests/package-smoke/
```

## 3. Extend command contract

Update:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
AGENTS.md
.github/copilot-instructions.md
```

Add:

```text
./eng/public-docs.sh
./eng/package-smoke.sh
./eng/public-api.sh
./eng/release-check.sh <version>
```

## 4. Update test guardrails

Update:

```text
docs/guardrails/testing.md
```

Add `PackageSmoke` as a test category that is excluded from `eng/test.sh` and included in `eng/release-check.sh`.

## 5. Update packaging documentation

Update:

```text
docs/engineering/packaging.md
```

Add rules for:

- package README source under `public-docs/nuget/`;
- package smoke tests;
- release gate before publish;
- package metadata synchronization.

## 6. Add public API validation

Choose and document a public API validation strategy.

Examples:

```text
PublicAPI.Shipped.txt
PublicAPI.Unshipped.txt
```

or an equivalent snapshot-based strategy.

Add command:

```text
./eng/public-api.sh
```

## 7. Update samples documentation

Move or create sample documentation under:

```text
docs/engineering/samples.md
public-docs/samples/
```

Remove any local sample README files.

## 8. Update website documentation

If GitHub Pages is used, document website source and publishing under:

```text
docs/engineering/site.md
public-docs/website/
docs/workflows/pages.md
```

Do not rely on `site/README.md`.

## 9. Update root README

For public packages, make README user-first.

Contributor and agent documentation should be linked after installation, quick start, package explanation, and samples.

## 10. Update issue templates

Add public documentation impact sections to:

```text
.github/ISSUE_TEMPLATE/documentation.md
.github/ISSUE_TEMPLATE/milestone-implementation.md
.github/ISSUE_TEMPLATE/release.md
```

## 11. Update CI/release workflows

Add or update workflow specs:

```text
docs/workflows/public-docs.md
docs/workflows/release-check.md
docs/workflows/release.md
```

CI may run public docs checks if they are fast.

Release workflows should run:

```text
./eng/release-check.sh <version>
```

before publishing.

## 12. Store Engineering Guide V3 as research

Store the previous guide at:

```text
docs/research/engineering-guide-v3.md
```

---

# 31. Final V4 model

Engineering Guide V4 keeps the strong parts of V3:

- canonical `eng/` scripts;
- explicit command contracts;
- building blocks;
- .NET 10 setup;
- MTP + TUnit;
- BenchmarkDotNet;
- Bun + Biome;
- optional Blazor and Playwright;
- test guardrails;
- GitHub Actions using `eng/check.sh`;
- agent-friendly validation.

It adds the release-oriented surface needed for public NuGet libraries:

- `public-docs/` as consumer-facing documentation source;
- `docs/PUBLIC-DOCS.md` as synchronization authority;
- `eng/public-docs.sh` for public documentation validation;
- `eng/package-smoke.sh` for packed-package consumer validation;
- `eng/public-api.sh` for public API control;
- `eng/release-check.sh` as the release gate;
- BB19 Public Documentation;
- BB20 Release Readiness.
