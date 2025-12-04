# spec-notes

Add or update module-specific notes documenting decisions, patterns, and constraints.

## Parameters

- `module` (required) - Module/feature name (e.g., "authentication", "user-management")
- `type` (required) - Entry type: "decision", "pattern", "constraint", or "question"
- `content` (required) - The note content to add

## Behavior

You will:

1. **Locate or create notes file**
   - Check if `spec/notes/{module}.md` exists
   - If not, instantiate from `spec/notes/notes-template.md`
   - Replace `[Module Name]` with the actual module name

2. **Format the entry**
   - **decision**: Add under "## Decisions" with format: `### {title} - {today's date}\n{content}`
   - **pattern**: Add under "## Patterns" with format: `### {title}\n{content}`
   - **constraint**: Add under "## Constraints" with format: `### {title}\n{content}`
   - **question**: Add under "## Open Questions" with format: `- [ ] {content}`

3. **Extract title from content**
   - For decision/pattern/constraint: Use first few words as title
   - Keep content as the full explanation

4. **Append to appropriate section**
   - Add entry to the correct section
   - Maintain existing entries

5. **Report**
   - Confirm what was added and where
   - Show the file path

## Example Usage

```
/spec-notes module="authentication" type="decision" content="Used JWT over sessions because API is stateless and needs to scale horizontally"

/spec-notes module="user-management" type="pattern" content="Email normalization: Always lowercase and trim before storage. See src/domain/user/email.ts"

/spec-notes module="payments" type="constraint" content="Cannot process refunds older than 90 days due to payment processor limitations"

/spec-notes module="search" type="question" content="Should we implement fuzzy matching or stick with exact prefix matching?"
```

## Important

- This is the **only** command for managing notes (no separate create/update)
- Notes are **optional** - only create when module has significant decisions/patterns
- Keep entries **concise** - 1-2 sentences per decision
- Link from overview.md when notes exist: `Notes: [📝](notes/module-name.md)`
