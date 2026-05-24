#!/usr/bin/env sh
# Run fast tests only.
# Long-running tests and benchmarks are excluded.
# To include long-running integration tests, set:
#   FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet run --no-build -c Release \
  --project "${REPO_ROOT}/tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj"

dotnet run --no-build -c Release \
  --project "${REPO_ROOT}/tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj"

dotnet run --no-build -c Release \
  --project "${REPO_ROOT}/tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj"
