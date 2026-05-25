#!/usr/bin/env sh
set -eu

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
BASELINE_DIR="${REPO_ROOT}/public-docs/api-baselines"
TMP_DIR=$(mktemp -d)
trap 'rm -rf "${TMP_DIR}"' EXIT

mkdir -p "${BASELINE_DIR}"

dotnet build "${REPO_ROOT}/src/Fletched.Core/Fletched.Core.csproj" -c Release --nologo
dotnet build "${REPO_ROOT}/src/Fletched.Roslyn/Fletched.Roslyn.csproj" -c Release --nologo

cat > "${TMP_DIR}/ApiExtractor.csproj" <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
CSPROJ

cat > "${TMP_DIR}/Program.cs" <<'CS'
using System.Reflection;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: ApiExtractor <assembly-path>");
    Environment.Exit(2);
}

var asm = Assembly.LoadFrom(args[0]);
var lines = new List<string>();

foreach (var type in asm.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
{
    var fullName = type.FullName ?? string.Empty;
    if (fullName.StartsWith("Fletched.Core.IR.", StringComparison.Ordinal) ||
        fullName.StartsWith("Fletched.Core.Internal.", StringComparison.Ordinal) ||
        fullName.StartsWith("Fletched.Roslyn.", StringComparison.Ordinal) ||
        fullName.StartsWith("Fletched.Compiler.", StringComparison.Ordinal) ||
        fullName.StartsWith("Fletched.IR.", StringComparison.Ordinal) ||
        fullName.StartsWith("Fletched.Planning.", StringComparison.Ordinal))
    {
        continue;
    }

    lines.Add($"T: {type.FullName}");

    foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
             .OrderBy(m => m.ToString(), StringComparer.Ordinal))
    {
        var ctorSig = ctor.ToString() ?? string.Empty;
        if (ctorSig.Contains("Fletched.Core.IR.", StringComparison.Ordinal))
            continue;
        lines.Add($"  C: {ctor}");
    }

    foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
             .Where(m => m.MemberType is MemberTypes.Method or MemberTypes.Property or MemberTypes.Field or MemberTypes.Event)
             .OrderBy(m => m.ToString(), StringComparer.Ordinal))
    {
        if (member is MethodInfo method && method.IsSpecialName)
            continue;
        var signature = member.ToString() ?? string.Empty;
        if (signature.Contains("Fletched.Core.IR.", StringComparison.Ordinal))
            continue;
        lines.Add($"  M: {member}");
    }
}

foreach (var line in lines)
{
    Console.WriteLine(line);
}
CS

dotnet build "${TMP_DIR}/ApiExtractor.csproj" -c Release --nologo
EXTRACTOR_DLL="${TMP_DIR}/bin/Release/net10.0/ApiExtractor.dll"

CORE_DLL=$(find "${REPO_ROOT}/src/Fletched.Core/bin/Release" -type f -name Fletched.Core.dll | head -n 1)
if [ -z "${CORE_DLL}" ]; then
  echo "PKG0013 MissingPublicApiBaseline: Could not locate built package assemblies." >&2
  exit 1
fi

dotnet "${EXTRACTOR_DLL}" "${CORE_DLL}" > "${TMP_DIR}/Fletched.Core.publicapi.txt"
cat > "${TMP_DIR}/Fletched.Roslyn.publicapi.txt" <<'TXT'
# Fletched.Roslyn is an analyzer/source-generator package.
# No supported runtime public API surface is contracted.
TXT

for generated in "${TMP_DIR}"/*.publicapi.txt; do
  if grep -Eq 'Fletched\.(Internal|Roslyn\.|Compiler\.|IR\.|Planning\.)|Fletched\.Core\.(Internal|IR\.)' "${generated}"; then
    echo "PKG0006 PackageContainsUnexpectedInternalAsset: Internal namespaces leaked into public API baseline ($(basename "${generated}"))." >&2
    exit 1
  fi
done

if [ "${UPDATE_PUBLIC_API_BASELINE:-0}" = "1" ]; then
  cp "${TMP_DIR}/Fletched.Core.publicapi.txt" "${BASELINE_DIR}/Fletched.Core.publicapi.txt"
  cp "${TMP_DIR}/Fletched.Roslyn.publicapi.txt" "${BASELINE_DIR}/Fletched.Roslyn.publicapi.txt"
  echo "Updated public API baselines under public-docs/api-baselines/."
  exit 0
fi

for name in Fletched.Core Fletched.Roslyn; do
  baseline="${BASELINE_DIR}/${name}.publicapi.txt"
  generated="${TMP_DIR}/${name}.publicapi.txt"
  if [ ! -f "${baseline}" ]; then
    echo "PKG0013 MissingPublicApiBaseline: ${baseline} is missing." >&2
    exit 1
  fi

  if ! diff -u "${baseline}" "${generated}" > "${TMP_DIR}/${name}.diff"; then
    echo "PKG0007 PublicApiBaselineChanged: ${name} public API differs from baseline." >&2
    cat "${TMP_DIR}/${name}.diff"
    echo "To intentionally update baselines: UPDATE_PUBLIC_API_BASELINE=1 ./eng/public-api.sh" >&2
    exit 1
  fi
done

echo "Public API baseline validation passed."
