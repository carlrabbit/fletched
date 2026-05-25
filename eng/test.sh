#!/usr/bin/env sh
# Run fast tests only.
# Benchmarks are excluded.
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet run --no-build -c Release \
  --project "${REPO_ROOT}/tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj"

dotnet run --no-build -c Release \
  --project "${REPO_ROOT}/tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj"

dotnet run --no-build -c Release \
  --project "${REPO_ROOT}/tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj"
