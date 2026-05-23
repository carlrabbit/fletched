#!/usr/bin/env bash
# Canonical completion gate.
# Runs restore, build, and fast tests.
# Does not run benchmarks or long-running tests.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"${SCRIPT_DIR}/restore.sh"
"${SCRIPT_DIR}/build.sh"
"${SCRIPT_DIR}/test.sh"
