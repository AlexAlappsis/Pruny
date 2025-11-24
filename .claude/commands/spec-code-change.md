Make incremental code changes grounded in the living spec and existing code patterns.

Use this for **modifying existing code**: adding features, fixing bugs, refactoring, etc.

**What this command does:**

1. Loads the living spec:
   - `spec/overview.md` (narrative overview)
   - `spec/invariants.json` (hard constraints)
   - `spec/glossary.md` (terminology)
2. Determines working path from explicit parameter, IDE selection, or user input
3. Analyzes existing code in the working area
4. Works cooperatively through three phases:
   - **ANALYZE**: Understand current state, propose plan
   - **CONFIRM**: Ask clarifying questions, get approval
   - **EXECUTE**: Apply changes, report results

**Scope & safety:**

- Modifies a **coherent set of files** to complete the requested change
- Can touch multiple layers (domain → data → API) if needed for completeness
- Preserves existing patterns and style
- Respects invariants and architectural boundaries
- Will NOT refactor unrelated code or add unasked features

**Usage:**

```text
/spec-code-change feature="Add tags property to Task entity" path=src/domain

/spec-code-change feature="Fix validation error in login flow"
# (uses IDE selection or asks for path)

/spec-code-change feature="Update task status enum"
# (with src/domain/Task.cs selected in IDE)
```

---

## Implementation instructions for Claude Code

### Phase 0: Determine Working Path

Resolve the working path in priority order:

#### 1. Explicit path= parameter (highest priority)

If user provided `path=some/directory`:
- Use that path directly
- Skip to Phase 1

#### 2. IDE selection context (automatic fallback)

If no `path=` was provided, check for `<ide_selection>` tags in conversation context.

**If a file is selected:**
- Extract the directory containing that file
- Use that directory as the working path
- Inform user: `Working in [directory] (from selected file [filename])`

**If a folder is selected:**
- Use that folder directly as the working path
- Inform user: `Working in [directory] (from selected folder)`

**Smart filtering - ignore selections of:**
- Documentation files (*.md in root, docs/, etc.)
- Config files (package.json, *.csproj, appsettings.json, .env, etc.)
- Build artifacts (bin/, dist/, build/, node_modules/, obj/)
- Git/IDE files (.git/, .vscode/, .idea/)

If these are selected, treat as "no selection" and fall through to asking.

#### 3. Ask the user (final fallback)

If neither explicit path nor valid IDE selection exists:

Ask: `What area of the codebase should I work in? (provide a directory path like 'src/tasks' or 'src/api')`

Wait for user response before proceeding.

---

### Phase 1: ANALYZE (automatic, read-only)

#### 1.1 Load spec context

1. Try to open `spec/overview.md`
   - If missing: explain that spec doesn't exist, suggest `/spec-overview`, then stop
2. Try to open `spec/invariants.json`
   - If missing: continue but note invariants unavailable
3. Try to open `spec/glossary.md`
   - If missing: continue without terminology guidance

#### 1.2 Understand the request

Parse `feature="..."` from command arguments or infer from user's natural language description.

#### 1.3 Analyze existing code

Within the working path and related areas:

1. **Find key files**:
   - Entry points (routes/controllers/handlers)
   - Domain models/entities
   - Services/use-cases
   - Data access (repositories/contexts)
   - Relevant tests

2. **Understand current behavior**:
   - How does the code currently work?
   - What patterns are in use?
   - What naming conventions exist?
   - How are similar features implemented?

3. **Cross-check against spec**:
   - Does current code match `overview.md`?
   - Are invariants being followed?
   - Are there discrepancies to note?

#### 1.4 Design the change

Based on:
- The feature request
- Current code patterns
- The living spec (modules, flows, boundaries)
- The invariants (hard rules)

Identify:
- **What needs to be modified**:
  - Which files need changes
  - What will change in each file
  - Why these changes are needed
- **Cross-layer dependencies**:
  - Related files outside working path (DTOs, migrations, controllers, etc.)
  - Configuration updates
  - Database migrations

**Coherence scope principle**: Include all files needed to make the change work end-to-end, but don't expand beyond what was requested.

#### 1.5 Prepare the plan

Create a structured plan showing:

1. **Files to modify**:
   - `src/domain/Task.cs` - Add `Tags` property (string array)
   - `src/data/Migrations/004_AddTaskTags.cs` - New migration for tags column
   - `src/data/AppDbContext.cs` - Configure tags as JSON column
   - `src/api/dtos/TaskDto.cs` - Add tags to DTO
   - `src/api/TasksController.cs` - Expose tags in GET/POST endpoints

2. **CLI commands** (if any):
   ```bash
   dotnet ef migrations add AddTaskTags
   ```

3. **Key decisions**:
   - Tags stored as JSON array in database (existing pattern for arrays)
   - Tags optional (nullable property)
   - Following invariant: controllers don't access DB directly

4. **Potential issues**:
   - None identified

---

### Phase 2: CONFIRM (requires user input)

#### 2.1 Present the plan

Show the plan from Phase 1 in a clear, scannable format.

#### 2.2 Ask clarifying questions (only when necessary)

Ask questions ONLY when:

1. **Multiple valid approaches exist**:
   - "Tags could be a simple string array or a separate Tags table with many-to-many. Which do you prefer?"
   - "Should existing tasks get empty tags array or null?"

2. **The spec/code is contradictory**:
   - "Invariants say use UUIDs, but existing code uses auto-increment IDs. Follow invariant (breaking change) or existing pattern?"

3. **The request is genuinely ambiguous**:
   - "'Fix validation' - should this be client-side, server-side, or both?"
   - "'Update status enum' - add new values or rename existing ones?"

**DO NOT ask when**:
- The answer is in `spec/overview.md` or `spec/invariants.json`
- The answer is clear from existing code patterns
- It's a standard convention (e.g., new properties are optional, use existing serialization)
- There's an obvious sensible default

Keep questions to **3 or fewer**. If you need more than 3 answers, the request is too ambiguous - ask user to clarify the feature instead.

#### 2.3 Get approval

After any clarifying questions are answered, ask for final approval:

```
This will modify [X] files and run [Y] commands.

Proceed? (yes/no)
```

If user says no or requests changes, loop back with adjustments.

---

### Phase 3: EXECUTE (automatic after approval)

#### 3.1 Run CLI commands

Execute any CLI commands from the plan:
- Database migrations
- Package installation
- Code generation tools

Report each command and its result.

#### 3.2 Modify files

For each file in the plan:

1. **Make surgical changes**:
   - Add new properties/methods
   - Update existing logic minimally
   - Preserve surrounding code
   - Don't refactor unrelated code

2. **Follow existing patterns**:
   - Match naming conventions
   - Use same error handling approach
   - Apply consistent formatting and style
   - Reuse existing abstractions

3. **Respect invariants**:
   - Follow layering rules
   - Use required ID/date formats
   - Maintain architectural boundaries
   - If invariant conflicts with existing code, note it in summary

4. **Use glossary terms**:
   - Use terminology from `spec/glossary.md`
   - Name new concepts consistently

5. **Cross-layer changes allowed**:
   - If working path is `src/domain` but change requires updating DTOs, migrations, and controllers, do it
   - The goal is a **complete, working change**

#### 3.3 Report results

After all changes:

Produce a summary with:

1. **What was modified**:
   ```
   Modified:
   - src/domain/Task.cs (added Tags property)
   - src/data/AppDbContext.cs (configured Tags as JSON column)
   - src/data/Migrations/004_AddTaskTags.cs (new migration)
   - src/api/dtos/TaskDto.cs (added Tags to DTO)
   - src/api/TasksController.cs (exposed tags in endpoints)
   ```

2. **Commands run**:
   ```
   Ran:
   - dotnet ef migrations add AddTaskTags
   ```

3. **Next steps** (optional):
   ```
   Next steps:
   - Run migration: dotnet ef database update
   - Consider adding tag validation (max length, allowed characters)
   - Update tests to include tags
   ```

4. **Invariants followed** (if notable):
   ```
   Adhered to:
   - Invariant #1: Controllers don't access DB directly (used EF context)
   - Invariant #3: All arrays stored as JSON columns
   ```

5. **Issues detected** (if any):
   ```
   Noted:
   - Invariant #2 specifies UUIDs, but existing code uses int IDs (followed existing pattern)
   ```

Do NOT run git commands. Let user review and commit.

---

### Best Practices

1. **Prefer coherence over artificial limits**:
   - Don't stop at 5 files if the change needs 7 files to work
   - DO stop if you're modifying unrelated code

2. **Make complete changes**:
   - Domain → Data → API → Config in one pass
   - Change should be complete and functional

3. **Preserve existing style**:
   - Match formatting, naming, patterns
   - Don't refactor unless explicitly asked
   - Respect existing abstractions

4. **Minimal surgical edits**:
   - Add what's needed, don't rewrite
   - Preserve surrounding code exactly
   - Don't "improve" unrelated code

5. **Follow the code, not assumptions**:
   - Use patterns already present in codebase
   - Don't impose external "best practices" unless in spec
   - When spec and code conflict, ask user

6. **Use glossary terminology**:
   - Use terms from `spec/glossary.md` when naming things
   - Helps maintain consistency across codebase

7. **Handle conflicts gracefully**:
   - If invariant conflicts with code, note it (don't silently pick one)
   - If existing code has multiple patterns for same thing, ask which to follow

---

## Path Parameter Definition

**Purpose**: Specifies the primary area of the codebase where changes should be made.

**Behavior**:
1. **Analysis starts here**: Agent examines code under this path to understand current state
2. **Changes centered here**: Primary modifications happen in this area
3. **Natural dependencies allowed**: Agent can modify files outside this path if directly needed (DTOs, migrations, controllers, config, etc.)
4. **Scope control**: Agent will NOT refactor unrelated code, add unasked features, or modify distant areas

**Think of it as**: "Make this change, starting from this area, but don't wander off into unrelated work"
