# Copilot Instructions

Primary repository documentation:
- `copilot-instructions.md`
- `docs/TERMINOLOGY.md`
- `docs/SPECS.md`
- `docs/architecture/`
- `docs/decisions/`
- `docs/workflows/`
- `docs/tbps/`
- `docs/agent-context/project-context.md`

Repository conventions:
- Use canonical terminology from `docs/TERMINOLOGY.md`
- Prefer minimal, targeted changes over broad rewrites
- Keep `README.md` as a navigation document, not an architectural specification
- Keep detailed design material in `specs/` and authoritative operational guidance in `docs/`

Implementation conventions:
- Target C# 14 and .NET 10 where appropriate
- Prefer file-scoped namespaces
- Use nullable reference types
- Use async APIs for I/O-bound work
- Use TUnit for tests and await all assertions

Workflow synchronization rules:
- Workflow intent is defined in `docs/workflows/`
- GitHub workflow files in `.github/workflows/` must remain synchronized
- New recurring processes belong in `docs/tbps/`
- New terminology belongs in `docs/TERMINOLOGY.md`
- New or revised behavioral specifications belong in `docs/specs/` and stay indexed in `docs/SPECS.md`
