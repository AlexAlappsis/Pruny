/spec-sync-spec — Sync the living spec to reflect the current code for a scoped area.

Usage:
- /spec-sync-spec path=src/tasks
- /spec-sync-spec module=auth

When invoked (Codex):
1) Load spec/overview.md; if missing, explain the spec does not exist yet and suggest /spec-overview. Optionally load spec/invariants.json, spec/glossary.md, and spec/change-log.md (create glossary/change-log from templates if helpful).
2) Require scope: path=... or module=...; if absent, explain and suggest concrete invocations without modifying files.
3) Analyze code in scope to capture responsibilities, public interfaces/endpoints, key flows, and primary models.
4) Map to overview: find or create the matching section; update only that module/area with concise narrative (responsibilities, key flows, main entities). Do not rewrite unrelated sections.
5) Glossary: add or refine terms directly related to the scoped area, keeping it focused.
6) Change log: append one entry dated YYYY-MM-DD summarizing the sync and notable findings (including potential invariant mismatches).
7) If invariants exist and the code seems to violate them, note the mismatch in the summary or change log; do not modify invariants silently.
