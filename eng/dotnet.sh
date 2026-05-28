#!/usr/bin/env sh
set -eu

if [ -n "${DOTNET_BIN:-}" ] && [ -x "${DOTNET_BIN}" ]; then
  printf '%s\n' "${DOTNET_BIN}"
  exit 0
fi

if command -v dotnet >/dev/null 2>&1; then
  command -v dotnet
  exit 0
fi

if [ -x "${HOME}/.dotnet/dotnet" ]; then
  # Codex Cloud Universal images may install .NET under ~/.dotnet without
  # exporting that path for non-interactive shell invocations.
  printf '%s\n' "${HOME}/.dotnet/dotnet"
  exit 0
fi

echo "error: dotnet SDK executable was not found." >&2
echo "hint: install .NET SDK 10.x or set DOTNET_BIN to the dotnet executable path." >&2
echo "hint: in Codex Cloud Universal only, dotnet may exist at ~/.dotnet/dotnet while not on PATH." >&2
exit 127
