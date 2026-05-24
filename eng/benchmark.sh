#!/usr/bin/env sh
# Build benchmarks. Does not run them — use a BenchmarkDotNet runner directly.
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet build "${REPO_ROOT}/benchmarks/Fletched.Benchmarks/Fletched.Benchmarks.csproj" -c Release
