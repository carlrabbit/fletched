#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

if [ "${NUGET_API_KEY:-}" = "" ]; then
  echo "NUGET_API_KEY is required to publish packages." >&2
  exit 1
fi

PACKAGES_FOUND=0
for package_path in "${REPO_ROOT}"/artifacts/nuget/*.nupkg; do
  if [ ! -f "${package_path}" ]; then
    continue
  fi

  PACKAGES_FOUND=1
  dotnet nuget push "${package_path}" \
    --api-key "${NUGET_API_KEY}" \
    --source "https://api.nuget.org/v3/index.json" \
    --skip-duplicate
done

if [ "${PACKAGES_FOUND}" -eq 0 ]; then
  echo "No packages found under artifacts/nuget/*.nupkg." >&2
  exit 1
fi
