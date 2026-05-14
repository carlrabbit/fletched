# Refactor Planning

# Goal

Plan refactors that improve internal structure without accidentally changing authoritative behavior.

# Constraints

- Refactors must preserve observable behavior unless a spec update is explicitly in scope.
- Risks and validation scope must be explicit before major restructuring.
- Architectural boundary changes must update architecture documents and decisions when they affect durable structure.
- Refactor plans must remain smaller than milestone-sized rewrites unless a milestone explicitly owns them.

# Non-Goals

- Sneaking in behavioral changes without specification updates
- Using refactoring as a catch-all for unrelated work
- Omitting rollback or validation considerations for risky changes

# Required Reading

- related specs
- related architecture documents
- related decisions
- `docs/TBPS.md`

# Process

1. Identify the structural problem.
2. Confirm the behavior that must remain unchanged.
3. Define scope, risks, and validation strategy.
4. Decide whether the work belongs in an issue or a milestone.
5. Update architecture or decision docs if the plan changes durable structure.
6. Execute only after the plan is bounded and validated.

# Validation

- Preserved behavior is explicit.
- Risks and rollback considerations are documented.
- Validation strategy covers the touched behavior.
- Related architecture and decision docs were reviewed.

# Authority

This document is authoritative for:
- refactor planning process
- refactor-risk validation expectations

# Document Contract

## Related Documents

- `docs/TBPS.md`
- `docs/architecture/README.md`
- `docs/decisions/README.md`

## Must Be Updated Together

When refactor-planning rules change, review and update:
- `docs/TBPS.md`
- related architecture or decision guidance documents
