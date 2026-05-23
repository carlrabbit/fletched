# Copilot Instructions

Read these documents first:
- `AGENTS.md`
- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/WORKFLOWS.md`
- `docs/TBPS.md`
- `docs/ENGINEERING.md`
- `docs/GUARDRAILS.md`
- `docs/agent-context/project-context.md`

Repository rules:
- Prefer minimal, targeted changes over broad rewrites.
- Keep `README.md` as a navigation document, not an architectural specification.
- Keep detailed design material in `specs/` and authoritative operational guidance in `docs/`.
- Update `docs/workflows/` before changing `.github/workflows/`.
- Add new terminology to `docs/TERMINOLOGY.md` before broad reuse.
- Keep new or revised behavioral specifications in `docs/specs/` and indexed in `docs/SPECS.md`.
- Keep this file synchronized with `.github/copilot-instructions.md`.
- Use `./eng/check.sh` as the canonical completion gate before declaring work complete.
- Do not run benchmarks or long-running tests by default.
