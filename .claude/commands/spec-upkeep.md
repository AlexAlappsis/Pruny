# spec-upkeep

Review recent conversation context and suggest updates to specification files.

## Purpose

Similar to `/spec-sync-spec` (which scans code for updates), this command scans the **conversation history** for decisions, patterns, and terminology that should be captured in spec files.

## Behavior

### Phase 1: Analyze Context

Scan recent chat history for:

1. **Code changes made**
   - Files created or modified
   - New modules or features added
   - Architectural decisions implemented

2. **Decisions discussed**
   - Technology choices (libraries, patterns, approaches)
   - Trade-offs evaluated
   - Constraints discovered

3. **Patterns established**
   - Coding conventions adopted
   - Structural patterns repeated
   - Cross-cutting concerns addressed

4. **Terminology introduced**
   - Domain terms defined
   - Technical vocabulary used consistently
   - Abbreviations or shorthand established

### Phase 2: Suggest Updates

Present potential updates to:

- **`spec/overview.md`** - If system boundaries or module descriptions changed
- **`spec/invariants.json`** - If new rules or constraints emerged
- **`spec/glossary.md`** - If new terms were consistently used
- **`spec/notes/{module}.md`** - If module-specific decisions were made
- **`spec/change-log.md`** - Document what was updated and why

Format suggestions clearly:

```
Found in recent conversation:
- [Brief description of what was discussed/implemented]

Suggested updates:
1. overview.md: [Specific change]
2. notes/authentication.md: [Specific addition]
3. glossary.md: [New term to add]

Proceed with these updates? (y/n/selective)
```

### Phase 3: Apply Updates

- If user approves, apply updates to suggested files
- If user says "selective", ask which specific updates to apply
- Update `spec/change-log.md` with a summary of changes
- Report what was updated

## Guidelines

**Suggest updates when:**
- New architectural decisions were made and implemented
- Consistent patterns emerged across multiple changes
- Domain terminology was defined and used
- System boundaries or responsibilities shifted
- New constraints or rules were established

**Don't suggest when:**
- Changes are trivial or routine
- Decisions are temporary or experimental
- Patterns are standard conventions (not project-specific)
- Terms are common industry vocabulary

**Be selective:**
- Aim for signal, not noise
- Focus on non-obvious, project-specific knowledge
- Prioritize information that helps future work
- Max 5-7 suggestions per run (don't overwhelm)

## Example

```
User: /spec-upkeep

Agent: Reviewing recent conversation...

Found:
- Implemented authentication module using JWT
- Established pattern: All API routes use auth middleware
- Defined terms: "refresh token", "access token", "token rotation"
- Constraint: Tokens expire in 1 hour, refresh tokens in 7 days

Suggested updates:
1. overview.md: Add Authentication module section under Modules
2. notes/authentication.md: Add decision about JWT vs sessions
3. notes/authentication.md: Add constraint about token expiry
4. glossary.md: Add "refresh token" and "access token" definitions

Proceed? (y/n/selective)
```

## When to Use

Run this command:
- **After completing a feature** - Capture decisions made during implementation
- **After significant discussions** - Document architectural choices
- **Periodically** - Every 5-10 interactions to keep specs current
- **Before switching contexts** - Ensure current work is documented

## Important

- This is **conversational context scanning**, not code scanning
- Look back ~10-20 messages for recent context
- Be **judicious** - suggest only noteworthy updates
- Always include change-log.md update when modifying specs
- Cross-reference with existing spec content to avoid duplication
