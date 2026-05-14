# Terminology Management

# Goal

Add or revise canonical terminology before it spreads across authoritative documents.

# Constraints

- Each term must have one canonical meaning.
- New terms must be concise and repository-specific.
- Avoid undocumented aliases.
- Terminology changes must trigger document synchronization review.

# Non-Goals

- Redefining behavior through terminology alone
- Adding product marketing language
- Leaving conflicting synonyms unresolved

# Required Reading

- `docs/TERMINOLOGY.md`
- related specs
- related architecture documents
- related workflow documents or TBPs

# Process

1. Identify the new or conflicting term.
2. Define the canonical meaning in one sentence.
3. Check for conflicting uses or aliases.
4. Update `docs/TERMINOLOGY.md`.
5. Review related specs, workflows, TBPs, and contributor instructions.
6. Update issue templates if the term appears in recurring intake forms.

# Validation

- The term has one canonical meaning.
- Related documents use the term consistently.
- Conflicting aliases are removed or explicitly listed.
- Synchronization targets were reviewed.

# Authority

This document is authoritative for:
- terminology management process
- terminology synchronization expectations

# Document Contract

## Related Documents

- `docs/TERMINOLOGY.md`
- `docs/TBPS.md`

## Must Be Updated Together

When terminology-management rules change, review and update:
- `docs/TERMINOLOGY.md`
- `docs/TBPS.md`
