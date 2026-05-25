#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=${1:-}

if [ -z "${VERSION}" ]; then
  echo "Usage: ./eng/release-check.sh <version>" >&2
  echo "PKG0001 PackageVersionMismatch: version argument is required." >&2
  exit 1
fi

check_metadata() {
  csproj="$1"
  for prop in PackageId Authors Description PackageTags RepositoryUrl RepositoryType PackageReadmeFile PublishRepositoryUrl EmbedUntrackedSources ContinuousIntegrationBuild SymbolPackageFormat IncludeSymbols; do
    if ! grep -q "<${prop}>" "${csproj}"; then
      echo "PKG0003 MissingRepositoryMetadata: ${csproj} missing ${prop}." >&2
      exit 1
    fi
  done
  if ! grep -Eq '<PackageLicenseExpression>|<PackageLicenseFile>' "${csproj}"; then
    echo "PKG0003 MissingRepositoryMetadata: ${csproj} missing package license metadata." >&2
    exit 1
  fi
}

"${REPO_ROOT}/eng/check.sh"

if [ ! -f "${REPO_ROOT}/docs/research/project-setup-guide-v5.md" ] || [ ! -f "${REPO_ROOT}/docs/research/engineering-guide-v4.md" ]; then
  echo "PKG0011 MissingCurrentResearchGuide: current research guides are missing." >&2
  exit 1
fi

if find "${REPO_ROOT}/docs/research" -type f \( -name 'project-setup-guide-v1.md' -o -name 'project-setup-guide-v2.md' -o -name 'project-setup-guide-v3.md' -o -name 'project-setup-guide-v4.md' -o -name 'engineering-guide-v1.md' -o -name 'engineering-guide-v2.md' -o -name 'engineering-guide-v3.md' \) | grep -q .; then
  echo "PKG0009 OldResearchGuidePresent: old research guides remain in docs/research/." >&2
  exit 1
fi

if find "${REPO_ROOT}/docs" "${REPO_ROOT}/public-docs" "${REPO_ROOT}/eng" "${REPO_ROOT}/samples" -type f -name README.md 2>/dev/null | grep -q .; then
  echo "PKG0010 UnauthorizedReadmePresent: only root README.md is allowed." >&2
  exit 1
fi

check_metadata "${REPO_ROOT}/src/Fletched.Core/Fletched.Core.csproj"
check_metadata "${REPO_ROOT}/src/Fletched.Roslyn/Fletched.Roslyn.csproj"

"${REPO_ROOT}/eng/public-api.sh"
"${REPO_ROOT}/eng/public-docs.sh" "${VERSION}"
"${REPO_ROOT}/eng/package.sh" "${VERSION}"
"${REPO_ROOT}/eng/package-smoke.sh" "${VERSION}"

echo "Release check passed for version ${VERSION}."
