Synchronize the living spec with the current code for a specific module or path.

This command is for **code → spec** work: you point it at part of the codebase, and it updates the living architecture, glossary, and change log to reflect what the code actually does now.

**What this command does:**

1. Loads:
   - `spec/living-architecture.md`
   - `spec/invariants.json`
   - `spec/glossary.md`
   - `spec/change-log.md` (if present)
2. Scans the code under a given `path` or `module` to understand:
   - Responsibilities and behavior
   - Public interfaces / endpoints
   - Important data models
3. Updates **only the relevant sections** of:
   - `spec/living-architecture.md` (module or flow description)
   - `spec/glossary.md` (new or refined terms)
   - `spec/change-log.md` (one concise entry for this sync)
4. Keeps the spec small and narrative:
   - No full-file rewrites
   - No expanding into a huge, detailed spec automatically

**Scope & safety rules:**

- You must specify `path=` or `module=`:
  - `path=`: filesystem path to code to analyze (e.g., `src/tasks`)
  - `module=`: a module name from `living-architecture.md` (e.g., `Tasks`, `Auth API`)
- The command only:
  - Updates the architecture section(s) that correspond to the given path/module
  - Touches glossary entries that are clearly related to that module/path
  - Appends **one** new entry to `change-log.md` describing this sync
- It must **not**:
  - Rewrite the entire `living-architecture.md` in one go
  - Erase other modules/sections
  - Override invariants (it can note mismatches, but not silently change invariants)

**Usage:**

```text
/spec-sync-spec path=src/tasks

/spec-sync-spec module=auth

/spec-sync-spec path=src/api/tasks
```

---

## Implementation instructions for Claude Code

When this command is invoked, follow these steps:

### 1. Load spec files

1. Open `spec/living-architecture.md`.
   - If missing, explain that the living spec does not exist yet and suggest running `/spec-overview` first.
2. Try to open `spec/invariants.json`.
   - If missing, continue; invariants are helpful but not required for sync.
3. Try to open `spec/glossary.md`.
   - If missing, you may create it using the glossary template at  `spec/glossary-template.md`.
4. Try to open `spec/change-log.md`.
   - If missing, you may create it using the change-log template at `spec/change-log-template.md`.

### 2. Determine scope

- Parse arguments for:
  - `path=` (e.g., `path=src/tasks`)
  - `module=` (e.g., `module=tasks`)

**If neither `path` nor `module` is provided:**

- Explain that a scope is required and suggest a concrete invocation.
- Do not modify any files.

**If `path` is provided:**

- Treat that path as the primary area for code analysis.
- You may read files outside the path for context (e.g., shared types), but consider the given path as the “module” you are describing.

**If `module` is provided:**

- Use `spec/living-architecture.md` to locate the section(s) describing that module.
- If you cannot confidently map the module to code (no mention of path, or ambiguous naming):
  - Explain the ambiguity.
  - Suggest a `path=` parameter for a follow-up invocation.
  - Do not write any files in this case.

### 3. Analyze the code in scope

Within the scoped path:

1. Identify:
   - Main entry points (handlers/controllers/routes)
   - Core services or use-cases
   - Primary domain models/data structures
2. For each, infer:
   - What responsibilities the module actually has
   - Key flows (e.g., “create task”, “update task status”, “tag tasks”)
   - Inputs/outputs at a conceptual level (don’t dump type signatures verbatim unless necessary)
3. Keep the analysis at a **narrative level**, consistent with the tone of `living-architecture.md`.

### 4. Map to the architecture document

1. In `spec/living-architecture.md`, locate the section(s) that describe:
   - The module by name (e.g., “## Tasks” or “## Module: Tasks”)
   - Or the broader area into which this path naturally fits

2. If a matching section exists:
   - Update only that section:
     - Adjust responsibility description to match reality.
     - Add or refine bullet points for key flows.
     - Mention important entities by name (using glossary terms).
   - Keep the section concise and narrative.

3. If no matching section exists:
   - Create a new subsection for this module near similar modules.
   - Give it:
     - A short responsibility description.
     - A bullet list of key flows.
     - A brief mention of main entities.

Do **not** rewrite unrelated sections or reorder the entire document.

### 5. Update the glossary (if needed)

1. From the code and architecture text, identify terms that:
   - Are central domain concepts.
   - Are used in multiple places (code + spec).
2. For each such term:
   - If it already exists in `spec/glossary.md`, refine the definition if the code indicates a clearer or more accurate meaning.
   - If it does not exist, add a new entry with:
     - The term.
     - A concise definition based on current behavior.
3. Keep the glossary short and focused; do not enumerate every type or internal helper.

### 6. Append a change-log entry

In `spec/change-log.md`:

1. Append a new entry with:
   - The current date (in YYYY-MM-DD).
   - A one-line description of what was synced and why (e.g., “Synced Tasks module spec with current tagging behavior in src/tasks.”).
   - Optional bullet points if there were notable findings (e.g., spec/code mismatches).

Example:

```md
- 2025-11-22 – Synced Tasks module spec with current code in src/tasks.
  - Documented new task tagging behavior.
  - Updated glossary entry for "Task" and added "Tag".
```

### 7. Detect and report mismatches with invariants (optional but recommended)

If `spec/invariants.json` is present:

1. Check whether the behavior observed in code appears to violate any invariants.
   - Example: Invariant says “controllers must not access the database directly”, but code in the scoped path has controllers using a DB client.
2. Do **not** silently change `invariants.json` or modify code.
3. Instead:
   - Mention the potential mismatch in your final summary.
   - Optionally, add a bullet under the new change-log entry noting the suspected violation.
   - The user can then decide whether to:
     - Fix the code, or
     - Update the invariant in a separate step (`/spec-invariants`).

### 8. Summarize changes

After writing updates:

1. Summarize, in natural language:
   - Which sections of `living-architecture.md` were updated (by heading).
   - Which terms were added/updated in `glossary.md`.
   - The new change-log entry that was added.
2. Do not restate the entire documents; keep the summary short so the user can quickly decide where to inspect diffs.

### 9. Best practices

- Keep the living spec **small and readable**; do not dump raw code into the architecture doc.
- When in doubt, prefer under-documenting a minor detail over overloading the spec with noise.
- Never remove large sections of the spec purely because the current code doesn’t yet implement them; instead, call out discrepancies in the change log or summary so the user can decide whether to adjust the plan or the implementation.
