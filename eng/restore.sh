#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet restore "${REPO_ROOT}/Fletched.slnx"
