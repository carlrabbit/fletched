# AGENTS

Read first:
- `README.md`
- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`
- `docs/ENGINEERING.md`
- `docs/GUARDRAILS.md`
- `docs/agent-context/project-context.md`

When changing workflows:
- update `docs/workflows/` first
- keep `.github/workflows/` synchronized

When introducing terminology:
- update `docs/TERMINOLOGY.md`

When introducing recurring execution patterns:
- add or update a document in `docs/tbps/`

When changing architecture:
- update `docs/architecture/`
- record the decision in `docs/decisions/` when the change affects long-term direction

When running validation:
- use `./eng/check.sh` as the canonical completion gate
- do not run benchmarks or long-running tests by default
- see `docs/GUARDRAILS.md` and `docs/engineering/command-contract.md` for rules

Avoid:
- duplicated architectural documentation
- duplicated workflow descriptions
- prose-heavy instructions that drift from authoritative docs
- README files under `docs/**` (use ALLCAPS index documents instead)
