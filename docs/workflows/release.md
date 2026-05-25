# Release Workflow

## Goal

Prepare and publish intentional public package releases.

## Required Sequence

1. Choose version.
2. Update version properties.
3. Update public docs.
4. Update package READMEs.
5. Update public API baselines if intentional.
6. Update release notes.
7. Run `./eng/release-check.sh <version>`.
8. Create tag.
9. Publish packages.
10. Verify packages from NuGet.
11. Create GitHub release.

## Release Policy Notes

- `0.2.0` is the first intentional pre-1.0 package line.
- `0.1.0.0` was premature and is not compatibility-preserved.
