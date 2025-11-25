/spec-overview — Maintain the living overview at spec/overview.md.

Usage:
- /spec-overview               # Create or refine the overview
- /spec-overview Modules       # Focus a specific section (e.g., Modules, Flows)
- /spec-overview Flows

When invoked (Codex):
1) Load files:
   - Open spec/overview.md; if missing, copy spec/overview-template.md into place.
   - Open spec/change-log.md; if missing, copy spec/change-log-template.md into place.
   - Optionally load spec/invariants.json and spec/glossary.md if present.
   - If templates are missing, stop with:
     Error: Templates not found. Please run the install script first:
     
     bash .specd/install.sh

     This will copy templates to your project's spec/ directory.
2) Determine focus: if an argument is provided, use it as the target section; otherwise summarize current content, suggest 2-4 areas to improve, and ask where to edit.
3) Edit cooperatively: show the current section (or short excerpt), propose concise narrative updates that reuse glossary terms and reference invariants when relevant, and write back only the targeted section.
4) Change log: initialize if newly created; if the update is meaningful, suggest/append a change entry describing what changed.
5) Suggest invariants/glossary updates when new constraints or terms appear; remind the user they can refine via /spec-invariants and /spec-glossary.
6) Suggest next steps if open questions remain (e.g., areas to research or code tasks using /spec-code-new or /spec-code-change).
