#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet build "${REPO_ROOT}/Fletched.slnx" -c Release --no-restore
