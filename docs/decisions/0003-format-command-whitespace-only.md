# 0003 — Keep `eng/format.sh` whitespace-only

## Status

Accepted

## Context

Engineering Guide V3 defaults formatting validation to full `dotnet format --verify-no-changes`.

In this repository, full `dotnet format` currently fails workspace loading for generator-backed test projects due unresolved generated result types, while the canonical build and test pipeline succeeds. This makes full-format validation noisy and unreliable as a completion gate.

## Decision

`eng/format.sh` remains:

```sh
dotnet format whitespace Fletched.slnx
```

`eng/check.sh` continues to call:

```sh
./eng/format.sh --verify-no-changes
```

## Consequences

- Formatting validation in canonical scripts remains whitespace-focused.
- Analyzer/style enforcement remains in build and `.editorconfig` policy.
- If generator workspace constraints are resolved, this decision should be revisited.
