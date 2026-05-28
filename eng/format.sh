#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
DOTNET_BIN=$("${REPO_ROOT}/eng/dotnet.sh")

"${DOTNET_BIN}" format whitespace "${REPO_ROOT}/Fletched.slnx" "$@"
