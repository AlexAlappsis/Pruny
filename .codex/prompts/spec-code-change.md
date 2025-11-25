/spec-code-change � Make incremental code changes grounded in the spec, invariants, and existing patterns.

Usage:

- /spec-code-change feature="Add tags to Task" path=src/domain
- /spec-code-change feature="Fix login validation" # uses IDE selection or asks for path

When invoked (Codex):
Phase 0: Resolve working path (path= parameter > IDE selection file/folder, ignoring docs/config/build artifacts > ask the user for a directory like src/tasks).

Phase 1: ANALYZE

- Load spec/overview.md; if missing, stop and suggest /spec-overview.
- Try to load spec/invariants.json and spec/glossary.md (continue if missing but note).
- Understand feature=... from args or user request.
- Scan current code in/near the working path: entry points, domain models, services/use-cases, data access, relevant tests; note existing patterns and any spec/invariant discrepancies.
- Draft a plan: which files to change and how, any needed commands, key decisions, and potential issues.

Phase 2: CONFIRM

- Present the plan clearly.
- Ask up to 3 clarifying questions only for genuine choices or conflicts (not for obvious defaults).
- Seek approval before executing: "This will modify X files and run Y commands. Proceed?".

Phase 3: EXECUTE

- Run required commands (migrations, installs, generators) and report results.
- Make surgical edits that match existing style; update cross-layer wiring/DTOs/tests as needed for a complete change.
- Respect invariants; if existing code conflicts, follow the invariant and note the conflict.
- Report modified files, commands run, next steps (e.g., apply migrations), and any outstanding issues.

Best practices: preserve patterns, avoid unrelated refactors, ensure the change is coherent and functional, and use glossary terminology when naming.
