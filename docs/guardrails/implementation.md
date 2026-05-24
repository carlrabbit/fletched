# Implementation Guardrails

## Purpose

This document defines implementation constraints for source code and documentation changes in the Fletched repository.

## Code Change Rules

1. **Make minimal, targeted changes.** Do not rewrite unrelated code.
2. **Do not break existing tests.** All existing tests must continue to pass.
3. **Do not remove or edit unrelated tests.** This could mask missing or buggy functionality.
4. **Do not introduce new security vulnerabilities.** Run security checks on dependency additions.
5. **Do not commit secrets.** Credentials, tokens, and API keys must never appear in source.
6. **Do not add new dependencies without advisory review.** Check the GitHub advisory database.
7. **Use existing libraries whenever possible.** Only add new libraries if absolutely necessary.
8. **Formatting command scope is whitespace-only.** Analyzer/style enforcement remains governed by build configuration and `.editorconfig`.

## Documentation Change Rules

1. **Update docs before code when specs or workflows change.** Workflow intent belongs in `docs/workflows/`; specs belong in `docs/SPECS.md`.
2. **Add new terminology to `docs/TERMINOLOGY.md` before broad reuse.**
3. **Keep index documents up to date.** When adding a document to a `docs/` folder, update the corresponding ALLCAPS index document.
4. **Keep `README.md` as a navigation document.** Do not add architectural specifications there.
5. **No README files under `docs/`.** Only ALLCAPS index documents are allowed as folder indexes.

## Agent-Specific Rules

1. **Run `./eng/check.sh` before declaring work complete.**
2. **Do not run benchmarks by default.** Use `./eng/benchmark.sh` only when explicitly required.
3. **Do not enable long-running tests by default.** Respect `FLETCHED_RUN_LONG_RUNNING_INTEGRATION_TESTS`.
4. **Read `docs/ENGINEERING.md` and `docs/GUARDRAILS.md` before starting implementation work.**
5. **Keep `AGENTS.md` and `.github/copilot-instructions.md` synchronized.**

## Synchronization Obligations

When making code changes:
- Update the corresponding spec if behavioral contracts change.
- Update `docs/TERMINOLOGY.md` if new canonical terms are introduced.
- Update `docs/MILESTONES.md` or the milestone document if milestone scope changes.

# Authority

This document is authoritative for:
- code and documentation change constraints
- agent behavior during implementation work

This document is not authoritative for:
- test execution rules (see `docs/guardrails/testing.md`)
- engineering command contracts (see `docs/ENGINEERING.md`)
