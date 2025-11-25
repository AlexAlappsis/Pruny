/spec-invariants — View or edit system invariants in spec/invariants.json.

Usage:
- /spec-invariants                 # List and interactively edit
- /spec-invariants add             # Add a new invariant
- /spec-invariants edit 2          # Edit invariant by id
- /spec-invariants deprecate 3     # Mark an invariant as deprecated

When invoked (Codex):
1) Load or initialize spec/invariants.json; if missing, copy spec/invariants-template.json. If the template is missing, stop with the standard install error message (bash .specd/install.sh ...).
2) Parse JSON; ensure an "invariants" array exists and build an id-indexed list.
3) Modes:
   - add: ask for category, summary, details (optional), appliesTo (optional); auto-suggest a non-conflicting id.
   - edit <id>: show current entry and ask which fields to change.
   - deprecate <id>: set status to "deprecated" (create field if missing).
   - default: show grouped summary by category and offer add/edit/deprecate options.
4) Write back spec/invariants.json with stable 2-space indentation.
