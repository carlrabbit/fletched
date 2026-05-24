#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet build "${REPO_ROOT}/Fletched.slnx" -c Release --no-restore
