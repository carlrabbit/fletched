#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=${1:-}

if [ "$#" -ne 1 ]; then
  echo "Usage: ./eng/package.sh <version>" >&2
  exit 1
fi

if ! printf '%s' "${VERSION}" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$'; then
  echo "Version must be SemVer-compatible (for example 1.2.3 or 1.2.3-beta.1)." >&2
  exit 1
fi

mkdir -p "${REPO_ROOT}/artifacts/nuget"

"${DOTNET_BIN}" pack "${REPO_ROOT}/src/Fletched.Core/Fletched.Core.csproj" \
  -c Release \
  --no-build \
  -o "${REPO_ROOT}/artifacts/nuget" \
  -p:ContinuousIntegrationBuild=true \
  -p:Version="${VERSION}"

"${DOTNET_BIN}" pack "${REPO_ROOT}/src/Fletched.Roslyn/Fletched.Roslyn.csproj" \
  -c Release \
  --no-build \
  -o "${REPO_ROOT}/artifacts/nuget" \
  -p:ContinuousIntegrationBuild=true \
  -p:Version="${VERSION}"
