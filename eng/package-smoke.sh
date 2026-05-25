#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=${1:-0.2.0}
NUGET_DIR="${REPO_ROOT}/artifacts/nuget"
TMP_DIR=$(mktemp -d)
trap 'rm -rf "${TMP_DIR}"' EXIT

"${REPO_ROOT}/eng/package.sh" "${VERSION}"

SMOKE_DIR="${TMP_DIR}/consumer"
dotnet new console --framework net10.0 --output "${SMOKE_DIR}"

cat > "${SMOKE_DIR}/NuGet.config" <<CFG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="${NUGET_DIR}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
CFG

cat > "${SMOKE_DIR}/consumer.csproj" <<EOF_CSPROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Fletched.Core" Version="${VERSION}" />
    <PackageReference Include="Fletched.Roslyn" Version="${VERSION}" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
EOF_CSPROJ

cp "${REPO_ROOT}/tests/package-smoke/Program.cs.template" "${SMOKE_DIR}/Program.cs"

dotnet restore "${SMOKE_DIR}/consumer.csproj" --configfile "${SMOKE_DIR}/NuGet.config"
dotnet build "${SMOKE_DIR}/consumer.csproj" -c Release --no-restore --nologo | tee "${TMP_DIR}/consumer-build.log"

if ! grep -q 'Fletched\.Roslyn\.FletchedIncrementalGenerator' "${TMP_DIR}/consumer-build.log"; then
  echo "PKG0005 PackageSmokeFailed: generated code not found in consumer obj output." >&2
  exit 1
fi

RUN_OUTPUT=$(dotnet run --project "${SMOKE_DIR}/consumer.csproj" -c Release --no-build)
echo "${RUN_OUTPUT}" | grep -q "SMOKE_OK" || {
  echo "PKG0005 PackageSmokeFailed: minimal query execution failed." >&2
  exit 1
}

cp "${REPO_ROOT}/tests/package-smoke/InvalidDiagnostic.cs.template" "${SMOKE_DIR}/InvalidDiagnostic.cs"
if dotnet build "${SMOKE_DIR}/consumer.csproj" -c Release --no-restore --nologo > "${TMP_DIR}/invalid-build.log" 2>&1; then
  echo "PKG0005 PackageSmokeFailed: expected invalid DSL diagnostic build failure." >&2
  exit 1
fi

grep -q "FLTCH011" "${TMP_DIR}/invalid-build.log" || {
  echo "PKG0005 PackageSmokeFailed: expected diagnostic FLTCH011 was not emitted." >&2
  cat "${TMP_DIR}/invalid-build.log"
  exit 1
}

rm -rf "${SMOKE_DIR}/obj" "${SMOKE_DIR}/bin"
dotnet nuget locals all --clear >/dev/null

dotnet restore "${SMOKE_DIR}/consumer.csproj" --configfile "${SMOKE_DIR}/NuGet.config" >/dev/null

echo "Package smoke validation passed."
