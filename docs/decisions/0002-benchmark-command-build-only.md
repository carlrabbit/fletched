# 0002 — Keep `eng/benchmark.sh` build-only

## Status

Accepted

## Context

Engineering Guide V3 defaults `eng/benchmark.sh` to benchmark execution (`dotnet run`).

This repository separates benchmark build and benchmark execution so default engineering commands stay predictable and avoid accidentally running expensive benchmark jobs during routine local or CI validation.

The performance workflow already performs benchmark execution explicitly after the build step.

## Decision

`eng/benchmark.sh` remains build-only:

```sh
dotnet build benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj -c Release
```

Benchmark execution remains a separate explicit command path (for example in `docs/workflows/performance-testing.md` and `.github/workflows/performance-testing.yml`).

## Consequences

- `eng/test.sh` and `eng/check.sh` stay benchmark-free.
- Performance workflow semantics remain explicit: build benchmark project first, then run benchmark executable.
- Documentation must continue to call out that benchmark execution is not part of canonical fast validation.
