#!/usr/bin/env bash
# Build benchmarks. Does not run them — use a BenchmarkDotNet runner directly.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet build "${REPO_ROOT}/benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj" -c Release
