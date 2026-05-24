#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet format whitespace "${REPO_ROOT}/Fletched.slnx" "$@"
