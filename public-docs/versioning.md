# Versioning

## Policy

- `0.1.0.0`: Premature release. No compatibility guarantees.
- `0.2.x`: First intentional pre-1.0 package line. Breaking changes are allowed between minor versions. Patch versions should avoid deliberate breaking changes unless needed to fix broken package behavior.
- `1.0.0`: First stable public API contract.

## Version Fields

- `PackageVersion`: NuGet package version.
- `AssemblyVersion`: CLR binding identity version.
- `FileVersion`: file/product version metadata.
- `InformationalVersion`: informational build/version string, often including commit metadata.
