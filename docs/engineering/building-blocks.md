# Building Blocks

## Purpose

This document describes the core toolchain building blocks used in the Fletched repository.

## Runtime and Generator

| Building Block | Responsibility |
| --- | --- |
| `Fletched.Core` | Runtime DSL, `EngineContext`, and fact storage primitives |
| `Fletched.Roslyn` | Source generator, validation pipeline, and C# code generation |

## Test Infrastructure

| Building Block | Responsibility |
| --- | --- |
| TUnit | Test framework (Microsoft.Testing.Platform) |
| `Fletched.Core.Tests` | Core runtime behavior tests |
| `Fletched.Features.Tests` | Feature-level behavioral tests |
| `Fletched.Integration.Tests` | Integration tests |
| `Fletched.Performance.Tests` | Performance and diagnostics tests |
| `Fletched.Sample.Tests` | Sample application tests |

## Benchmark Infrastructure

| Building Block | Responsibility |
| --- | --- |
| BenchmarkDotNet | Benchmark runner |
| `Fletched.Benchmarks` | Benchmark project under `benchmarks/` |

## Source Generation

| Building Block | Responsibility |
| --- | --- |
| Roslyn Incremental Source Generators | Compile-time code generation |
| `PredicateEmitter` | Emits generated predicate execution code |
| `SemanticAnalyzer` | Validates predicate call graphs and semantics |

# Authority

This document is authoritative for:
- identifying the core building blocks of the repository
- describing the responsibilities of each building block

This document is not authoritative for:
- detailed source generator architecture (see `docs/ARCHITECTURE.md`)
- behavioral specifications (see `docs/SPECS.md`)
