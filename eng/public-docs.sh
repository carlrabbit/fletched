#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=${1:-0.2.0}
PUBLIC_DOCS_DIR="${REPO_ROOT}/public-docs"

require_file() {
  if [ ! -f "$1" ]; then
    echo "$2" >&2
    exit 1
  fi
}

require_file "${PUBLIC_DOCS_DIR}/installation.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/installation.md"
require_file "${PUBLIC_DOCS_DIR}/getting-started.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/getting-started.md"
require_file "${PUBLIC_DOCS_DIR}/concepts.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/concepts.md"
require_file "${PUBLIC_DOCS_DIR}/packages.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/packages.md"
require_file "${PUBLIC_DOCS_DIR}/diagnostics.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/diagnostics.md"
require_file "${PUBLIC_DOCS_DIR}/versioning.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/versioning.md"
require_file "${PUBLIC_DOCS_DIR}/release-notes.md" "PKG0008 PublicDocsVersionMismatch: missing public-docs/release-notes.md"
require_file "${PUBLIC_DOCS_DIR}/nuget/Fletched.Core.md" "PKG0002 MissingPackageReadme: missing public-docs/nuget/Fletched.Core.md"
require_file "${PUBLIC_DOCS_DIR}/nuget/Fletched.Roslyn.md" "PKG0002 MissingPackageReadme: missing public-docs/nuget/Fletched.Roslyn.md"
require_file "${PUBLIC_DOCS_DIR}/api-baselines/Fletched.Core.publicapi.txt" "PKG0013 MissingPublicApiBaseline: missing public-docs/api-baselines/Fletched.Core.publicapi.txt"
require_file "${PUBLIC_DOCS_DIR}/api-baselines/Fletched.Roslyn.publicapi.txt" "PKG0013 MissingPublicApiBaseline: missing public-docs/api-baselines/Fletched.Roslyn.publicapi.txt"

if cmp -s "${PUBLIC_DOCS_DIR}/nuget/Fletched.Core.md" "${PUBLIC_DOCS_DIR}/nuget/Fletched.Roslyn.md"; then
  echo "PKG0012 PackageReadmesNotDistinct: package README files must be distinct." >&2
  exit 1
fi

for path in $(find "${REPO_ROOT}/docs" "${PUBLIC_DOCS_DIR}" "${REPO_ROOT}/eng" "${REPO_ROOT}/samples" -type f -name README.md 2>/dev/null || true); do
  if [ "${path}" != "${REPO_ROOT}/README.md" ]; then
    echo "PKG0010 UnauthorizedReadmePresent: unauthorized README.md at ${path}" >&2
    exit 1
  fi
done

if grep -R -nE 'project-setup-guide-v[1-4]|engineering-guide-v[1-3]' "${PUBLIC_DOCS_DIR}" "${REPO_ROOT}/docs/PUBLIC-DOCS.md" >/dev/null 2>&1; then
  echo "PKG0009 OldResearchGuidePresent: public docs reference old guide versions." >&2
  exit 1
fi

if ! grep -Eq "Fletched\.Core\" Version=\"${VERSION}" "${PUBLIC_DOCS_DIR}/installation.md"; then
  echo "PKG0008 PublicDocsVersionMismatch: installation snippet does not use version ${VERSION}." >&2
  exit 1
fi

if ! grep -Eq "Fletched\.Roslyn\" Version=\"${VERSION}" "${PUBLIC_DOCS_DIR}/installation.md"; then
  echo "PKG0008 PublicDocsVersionMismatch: installation snippet does not use version ${VERSION} for Roslyn." >&2
  exit 1
fi

for id in $(grep -oE '"FL[A-Z]+[0-9]{4}"' "${REPO_ROOT}/src/Fletched.Roslyn/Pipeline/DiagnosticsCatalog.cs" | tr -d '"'); do
  if ! grep -q "${id}" "${PUBLIC_DOCS_DIR}/diagnostics.md"; then
    echo "PKG0008 PublicDocsVersionMismatch: diagnostics doc missing ${id}." >&2
    exit 1
  fi
done

if grep -R -n "0.1.0.0" "${PUBLIC_DOCS_DIR}" | grep -Eiv 'premature|compatibility' >/dev/null 2>&1; then
  echo "PKG0014 PrematureVersionMarkedStable: 0.1.0.0 referenced without premature-release context." >&2
  exit 1
fi

echo "Public docs validation passed."
