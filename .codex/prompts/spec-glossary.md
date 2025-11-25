/spec-glossary — Manage domain vocabulary in spec/glossary.md.

Usage:
- /spec-glossary                 # List all terms
- /spec-glossary Recipe          # Show a specific term
- /spec-glossary add Recipe      # Add or update a term

When invoked (Codex):
1) Load spec/glossary.md; if missing, copy spec/glossary-template.md. If the template is missing, stop with the standard install error message (bash .specd/install.sh ...).
2) Modes:
   - No args: list terms; if none, encourage creating the first term.
   - Single arg: show the matching term (case-insensitive) from table or concepts; if missing, suggest closest matches and offer add.
   - add <term>: determine placement (table, concept, or both); ask for concise definition, related terms/synonyms, and concept group if needed. Confirm before saving.
3) Update glossary: maintain alphabetical order in the table, add concept bullets under appropriate headings (create if needed), and refresh front-matter updated date.
4) Report additions/updates and point to /spec-glossary for review.
