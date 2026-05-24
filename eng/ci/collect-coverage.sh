#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/../.." && pwd)
MODE=${1:-}

run_with_coverage() {
  project_path=$1
  coverage_output=$2

  dotnet run --no-build -c Release \
    --project "${REPO_ROOT}/${project_path}" \
    -- --coverage --coverage-output-format cobertura \
    --coverage-output "${coverage_output}"
}

case "${MODE}" in
  standard)
    run_with_coverage "tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj" "./Fletched.Core.Tests.cobertura.xml"
    run_with_coverage "tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj" "./Fletched.Features.Tests.cobertura.xml"
    run_with_coverage "tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj" "./Fletched.Integration.Tests.cobertura.xml"
    ;;
  long-running)
    run_with_coverage "tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj" "./Fletched.Core.Tests.cobertura.xml"
    run_with_coverage "tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj" "./Fletched.Features.Tests.cobertura.xml"
    FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS=1 dotnet run --no-build -c Release \
      --project "${REPO_ROOT}/tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj" \
      -- --coverage --coverage-output-format cobertura \
      --coverage-output "./Fletched.Integration.Tests.cobertura.xml"
    ;;
  performance)
    run_with_coverage "tests/Fletched.Performance.Tests/Fletched.Performance.Tests.csproj" "./Fletched.Performance.Tests.cobertura.xml"
    ;;
  *)
    echo "Usage: ./eng/ci/collect-coverage.sh {standard|long-running|performance}" >&2
    exit 1
    ;;
esac
