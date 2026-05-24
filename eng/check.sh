#!/usr/bin/env sh
# Canonical completion gate.
# Runs restore, build, fast tests, and formatting verification.
# Does not run benchmarks or long-running tests.
set -eu

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)

"${SCRIPT_DIR}/restore.sh"
"${SCRIPT_DIR}/build.sh"
"${SCRIPT_DIR}/test.sh"
"${SCRIPT_DIR}/format.sh" --verify-no-changes
