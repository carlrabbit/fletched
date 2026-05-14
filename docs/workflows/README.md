# Workflow Documents

## Purpose

This directory indexes workflow specifications that define operational intent before CI implementation.

## Contents

- `build-and-test.md`
- `nuget-pack-and-publish.md`
- `performance-testing.md`

See `../WORKFLOWS.md` for the authoritative workflow index and synchronization rules.

# Authority

This document is authoritative for:
- the directory-level index for `docs/workflows/`
- routing readers to workflow specification documents

This document is not authoritative for:
- GitHub Actions implementation details
- release policy decisions
- architecture design

# Document Contract

## Related Documents

- `docs/WORKFLOWS.md`
- `.github/workflows/README.md`

## Must Be Updated Together

When the workflow document set changes, review and update:
- `docs/WORKFLOWS.md`
- `.github/workflows/README.md`
- related workflow YAML files under `.github/workflows/`
