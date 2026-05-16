# GitHub Workflow Implementations

## Purpose

This directory contains the GitHub Actions workflow implementations for the workflow specifications documented under `docs/workflows/`.

## Current workflow implementations

- `build-and-test.yml`
- `build-and-test-long-running.yml`
- `performance-testing.yml`
- `nuget-pack-and-publish.yml`

## Authority

This document is authoritative for:
- the implementation role of files under `.github/workflows/`
- the synchronization expectation with `docs/workflows/`

This document is not authoritative for:
- workflow intent
- release policy rationale
- repository architecture

## Document Contract

### Related Documents

- `docs/WORKFLOWS.md`
- `docs/workflows/README.md`

### Must Be Updated Together

When workflow implementations are added, removed, or renamed, review and update:
- `docs/WORKFLOWS.md`
- `docs/workflows/README.md`
- the matching workflow specification under `docs/workflows/`
