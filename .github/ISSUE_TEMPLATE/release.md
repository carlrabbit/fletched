---
name: Release
about: Prepare and execute a versioned release.
title: "Release v"
labels: release
---

# Required Reading

- `docs/workflows/release.md`
- `docs/PUBLIC-DOCS.md`
- `docs/specs/PublicApi.md`
- `docs/ENGINEERING.md`

# Release Goal

# Validation Checklist

- [ ] Version chosen
- [ ] Public docs updated for version
- [ ] Package READMEs updated
- [ ] Public API baselines updated if intentional
- [ ] `./eng/check.sh` passes
- [ ] `./eng/release-check.sh <version>` passes
- [ ] Tag created
- [ ] Packages verified on NuGet
- [ ] GitHub release created

# Versioning Policy Reminder

- `0.1.0.0` was premature and has no compatibility guarantees.
- `0.2.0` is the first intentional pre-1.0 package line.
