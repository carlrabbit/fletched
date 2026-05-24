# Codespaces

## Purpose

This document defines Codespaces and dev-container guidance for the Fletched repository.

## Configuration

The shared Codespaces/dev-container configuration lives in `.devcontainer/devcontainer.json`.

## Rules

- Use the repository dev container as the shared development baseline for Codespaces and local VS Code dev containers.
- Keep the container image aligned with the repository's `.NET 10` workflow environment.
- Put deterministic setup in `updateContentCommand` so Codespaces prebuilds can cache restore/build work.
- Keep checked-in editor setup lightweight and shared.
- Use repository or Codespaces secrets for private feeds or external credentials.
- CI remains the source of truth for full validation.

## Expected Environment

The checked-in configuration provides:

- a `.NET 10` SDK Linux environment;
- essential C# and EditorConfig VS Code extensions;
- `Fletched.slnx` as the default solution;
- format-on-save support;
- `dotnet restore Fletched.slnx`;
- `dotnet build Fletched.slnx -c Release --no-restore`.

## Recommended Commands

Use the canonical engineering scripts after the Codespace opens:

```sh
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
```

# Authority

This document is authoritative for:
- Codespaces and dev-container guidance
- checked-in Codespaces expectations

This document is not authoritative for:
- workflow implementation details
- GitHub-side Codespaces repository settings

# Document Contract

## Related Documents

- `docs/ENGINEERING.md`
- `docs/engineering/dotnet.md`
- `.devcontainer/devcontainer.json`

## Must Be Updated Together

When Codespaces or dev-container guidance changes, review and update:
- `.devcontainer/devcontainer.json`
- `docs/ENGINEERING.md`
