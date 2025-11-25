/spec-code-new — Create new code grounded in the spec, invariants, and existing patterns (greenfield work).

Usage:
- /spec-code-new feature="Email notifications" path=src/notifications
- /spec-code-new feature="Healthcheck endpoint"        # uses IDE selection or asks for path

When invoked (Codex):
Phase 0: Resolve working path (path= parameter > IDE selection file/folder, ignoring docs/config/build artifacts > ask the user for a directory like src/api).

Phase 1: ANALYZE
- Load spec/overview.md; if missing, stop and suggest /spec-overview.
- Try to load spec/invariants.json and spec/glossary.md (continue if missing but note).
- Understand feature=... from args or user request.
- Scan repo patterns (language/framework, layering, DI, error/logging, testing) with emphasis on the working path and adjacent areas.
- Draft a plan: commands (if any), files to create, files to modify, cross-layer wiring, key assumptions.

Phase 2: CONFIRM
- Present the plan clearly.
- Ask up to 3 clarifying questions only when there are real choices, contradictions, or ambiguity (not for obvious defaults).
- Seek approval before executing: "This will create/modify X files and run Y commands. Proceed?".

Phase 3: EXECUTE
- Run planned CLI commands when needed (scaffolding, installs, migrations) and report results.
- Create new files matching observed patterns and respecting invariants/glossary; keep code minimal but coherent (vertical slice across layers allowed).
- Modify existing files surgically and consistently; touch wiring/config/DTOs/tests as needed for completeness.
- Report created/modified files, commands run, next steps (e.g., migrations/config), and note any invariant conflicts.

Best practices: follow spec/invariants, reuse patterns, avoid unrelated refactors, and prefer under-generating to speculative boilerplate.
