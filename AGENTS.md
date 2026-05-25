# AGENTS

Read first:
- `README.md`
- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`
- `docs/ENGINEERING.md`
- `docs/PUBLIC-DOCS.md`
- `docs/GUARDRAILS.md`
- `docs/research/project-setup-guide-v5.md`
- `docs/research/engineering-guide-v4.md`
- `docs/agent-context/project-context.md`

When changing workflows:
- update `docs/workflows/` first
- keep `.github/workflows/` synchronized

When changing public package/API behavior:
- update `public-docs/`
- update `docs/specs/PublicApi.md`
- keep `public-docs/api-baselines/` synchronized via `./eng/public-api.sh`

When running validation:
- use `./eng/check.sh` as canonical completion gate
- use `./eng/release-check.sh <version>` for release readiness
- do not run benchmarks or long-running tests by default
