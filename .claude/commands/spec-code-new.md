Create new code grounded in the living spec, project patterns, and invariants.

Use this for **greenfield** work: new modules, new features, new services, etc.

**What this command does:**

1. Loads the living spec:
   - `spec/overview.md` (narrative overview)
   - `spec/invariants.json` (hard constraints)
   - `spec/glossary.md` (terminology)
2. Determines working path from explicit parameter, IDE selection, or user input
3. Scans the repo to infer project patterns and style
4. Works cooperatively through three phases:
   - **ANALYZE**: Understand context, propose plan
   - **CONFIRM**: Ask clarifying questions, get approval
   - **EXECUTE**: Generate code, report results

**Scope & safety:**

- Creates a **coherent vertical slice** for the requested feature
- Can touch multiple layers (domain → data → API) if needed for completeness
- Can create new directories/projects if they don't exist
- Can run CLI commands (dotnet new, migrations, package installs)
- Will NOT add unrelated features or generate excessive boilerplate

**Usage:**

```text
/spec-code-new feature="Email notifications for task updates" path=src/notifications

/spec-code-new feature="New healthcheck endpoint"
# (uses IDE selection or asks for path)

/spec-code-new feature="Task tagging system"
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

Ask: `What area of the codebase should I work in? (provide a directory path like 'src/notifications' or 'src/api')`

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

#### 1.3 Scan for project patterns

Examine existing codebase to infer:
- **Language & framework** (C#/ASP.NET, TypeScript/Node, Python/Django, etc.)
- **Folder structure** (src/domain, lib/, app/, etc.)
- **Common patterns**:
  - Layering (controllers → services → repositories)
  - Dependency injection patterns
  - Error handling conventions
  - Logging approach
  - Testing style and location

Focus scanning on:
1. The working path (if it exists)
2. Similar modules/areas
3. Shared infrastructure/utilities

**Optimization:** Read all potentially relevant files in parallel to minimize token overhead

#### 1.4 Design the feature

Based on:
- The feature request
- The living spec (modules, flows, boundaries)
- The invariants (hard rules)
- Observed project patterns

Identify:
- **What needs to be created**:
  - New files (domain models, services, controllers, tests, etc.)
  - New directories or projects
  - CLI commands to run (e.g., `dotnet new classlib`, `npm install`, migrations)
- **What needs to be modified**:
  - Existing files that need updates (DI registration, routing, etc.)
  - Configuration files
- **Cross-layer dependencies**:
  - Files outside the working path that must be touched for coherence

**Coherence scope principle**: Include all files needed to make the feature work end-to-end, but don't expand beyond what was requested.

#### 1.5 Prepare the plan

Create a structured plan showing:

1. **CLI commands** (if any):
   ```bash
   dotnet new classlib -n MyApp.Notifications -o src/notifications
   dotnet add src/api reference src/notifications
   ```

2. **Files to create**:
   - `src/notifications/NotificationService.cs` - Core notification logic
   - `src/notifications/IEmailSender.cs` - Email abstraction
   - `src/notifications/EmailSender.cs` - SMTP implementation

3. **Files to modify**:
   - `src/api/Program.cs` - DI registration for notification services
   - `src/api/TasksController.cs` - Call notification service on task updates
   - `appsettings.json` - Add email configuration section

4. **Key assumptions**:
   - Using existing DI container pattern
   - Following invariant: services layer for business logic
   - Email config via IOptions<EmailSettings>

---

### Phase 2: CONFIRM (requires user input)

#### 2.1 Present the plan

Show the plan from Phase 1 in a clear, scannable format.

#### 2.2 Ask clarifying questions (only when necessary)

Ask questions ONLY when:

1. **Multiple valid architectural choices exist**:
   - "This could live in a new Notifications project or inside the existing API project. Which do you prefer?"
   - "Should I create a domain layer, or put models in the API layer?"

2. **The spec/code is contradictory**:
   - "Invariants say controllers can't access DB directly, but TasksController currently does. Follow invariant (refactor) or existing pattern?"

3. **The request is genuinely ambiguous**:
   - "Email notifications could be synchronous or queued (background worker). Which approach?"
   - "Should task creators be notified, or all assigned users?"

**DO NOT ask when**:
- The answer is in `spec/overview.md` or `spec/invariants.json`
- The answer is clear from existing code patterns
- It's a standard convention (e.g., make new properties optional, use ISO dates)
- There's an obvious sensible default

Keep questions to **3 or fewer**. If you need more than 3 answers, the request is too ambiguous - ask user to clarify the feature instead.

#### 2.3 Get approval

After any clarifying questions are answered, ask for final approval:

```
This will create [X] new files, modify [Y] existing files, and run [Z] commands.

Proceed? (yes/no)
```

If user says no or requests changes, loop back with adjustments.

---

### Phase 3: EXECUTE (automatic after approval)

#### 3.1 Run CLI commands

Execute any CLI commands from the plan:
- Project creation (`dotnet new`, `rails generate`, etc.)
- Package installation (`npm install`, `dotnet add package`)
- Database migrations
- Code generation tools

Report each command and its result.

#### 3.2 Create new files

For each new file in the plan:

1. **Follow discovered patterns**:
   - Match naming conventions (PascalCase, kebab-case, etc.)
   - Use same folder structure
   - Follow existing class/module patterns
   - Apply consistent formatting

2. **Respect invariants**:
   - Follow layering rules
   - Use required ID formats, date formats, etc.
   - Maintain architectural boundaries

3. **Use glossary terms**:
   - Use terminology from `spec/glossary.md`
   - Name classes/types/variables consistently with existing code

4. **Keep it minimal**:
   - Generate what's needed for the feature to work
   - Don't add speculative future features
   - Don't generate comprehensive tests unless requested (basic happy path is fine)

#### 3.3 Modify existing files

For each file to modify:

1. **Make surgical changes**:
   - Add new dependencies/imports
   - Register new services in DI
   - Add new routes/endpoints
   - Update configuration

2. **Preserve existing style**:
   - Match indentation and formatting
   - Follow existing patterns for similar changes
   - Don't refactor unrelated code

3. **Cross-layer changes allowed**:
   - If the working path is `src/notifications` but you need to wire it up in `src/api/Program.cs`, do it
   - If adding a domain property requires updating DTOs, migrations, and controllers, do it
   - The goal is a **complete, working feature**

#### 3.4 Report results

After all changes:

Produce a summary with:

1. **What was created**:
   ```
   Created:
   - src/notifications/NotificationService.cs
   - src/notifications/IEmailSender.cs
   - src/notifications/EmailSender.cs
   ```

2. **What was modified**:
   ```
   Modified:
   - src/api/Program.cs (added DI registration)
   - src/api/TasksController.cs (added notification call)
   - appsettings.json (added email config)
   ```

3. **Commands run**:
   ```
   Ran:
   - dotnet new classlib -n MyApp.Notifications
   - dotnet add package MailKit
   ```

4. **Next steps** (optional):
   ```
   Next steps:
   - Update appsettings.json with your SMTP credentials
   - Consider adding retry logic for failed emails
   - Add integration tests for email sending
   ```

5. **Invariants followed** (if notable):
   ```
   Adhered to:
   - Invariant #1: Controllers don't access DB directly (used service layer)
   - Invariant #4: All IDs are UUIDv7 strings
   ```

Do NOT run git commands. Let user review and commit.

---

### Best Practices

1. **Prefer coherence over artificial limits**:
   - Don't stop at 5 files if the feature needs 7 files to work
   - DO stop if you're adding unrelated features

2. **Create working vertical slices**:
   - Domain → Data → API → Config in one pass
   - Feature should be runnable after execution

3. **Use existing abstractions**:
   - Don't invent new patterns when existing ones work
   - Match the style and conventions already present

4. **Handle missing infrastructure gracefully**:
   - If working path doesn't exist, create it
   - If required projects don't exist, scaffold them
   - Document what was created vs. what was modified

5. **When in doubt, under-generate**:
   - Minimal working code > comprehensive boilerplate
   - User can always ask for more

6. **Use glossary terminology**:
   - Use terms from `spec/glossary.md` when naming things
   - Helps maintain consistency across codebase

---

## Path Parameter Definition

**Purpose**: Specifies the primary area of the codebase where new code should be created.

**Behavior**:
1. **Analysis starts here**: Agent examines code under this path to understand context
2. **Creation happens here**: New files are primarily created in this area
3. **Natural dependencies allowed**: Agent can modify files outside this path if directly needed (DI registration, DTOs, migrations, etc.)
4. **Scope control**: Agent will NOT add unrelated features, refactor distant code, or generate beyond what's needed

**Think of it as**: "Create this feature, centered in this area, but don't wander off into unrelated work"
