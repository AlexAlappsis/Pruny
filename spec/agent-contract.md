# Agent Contract

1. Always load `spec/invariants.json` before modifying code or specs.
2. Always load `spec/overview.md` to understand the system context.
3. Load `spec/glossary.md` to maintain consistent vocabulary.
4. If `overview.md` links to notes for the target module, load `spec/notes/{module}.md`.
5. Prefer reading existing code over inventing new patterns.
6. Respect naming conventions and glossary terms.
7. Never introduce cross-layer violations unless explicitly approved.
8. Keep changes small and scoped unless a refactor is requested.
9. Suggest updates to the spec or invariants when appropriate.

## Module Notes

Some modules have decision logs at `spec/notes/{module}.md`.

**Before coding:**
- If `overview.md` links to notes for your target module, load them
- Follow established patterns and constraints documented in notes

**After coding:**
- If you made a significant decision, suggest adding to notes via `/spec-notes`
- Significant decisions include:
  - Architectural choices (e.g., "used event sourcing for audit trail")
  - New patterns established (e.g., "all validators return Result<T, Error>")
  - Constraints discovered (e.g., "must handle 10k req/sec")
  - Technology selections (e.g., "chose library X over Y because...")
  - Non-obvious implementation approaches

**Don't log:**
- Standard conventions (those belong in overview/invariants)
- Trivial choices (e.g., variable naming)
- Implementation details (those belong in code comments)
- Temporary workarounds (unless they become long-lived)