# Specification System README

This document explains how the specd specification system works.

---

## Core Files

Your spec lives in these files:

- **`overview.md`** – 1-3 page narrative overview of the system
- **`invariants.json`** – machine-readable hard constraints
- **`glossary.md`** – shared domain vocabulary
- **`change-log.md`** – history of meaningful spec changes
- **`agent-contract.md`** – behavioral rules for LLM agents

---

## Templates

Each file has a corresponding `*-template.md` or `*-template.json` file with:
- YAML front matter (for .md files)
- Standardized structure
- Usage notes

Templates are used when creating new projects or initializing missing files.

---

## Available Commands

### Specification Management

**`/spec-overview [section]`**
- Create or refine `overview.md`
- Work cooperatively on specific sections
- Keep it narrative and concise (1-3 pages)

**`/spec-invariants [add|edit ID|deprecate ID]`**
- List, add, or edit invariants in `invariants.json`
- View by category
- Maintain stable JSON format

**`/spec-glossary [term|add term]`**
- List terms, show definition, or add/update terms
- Keeps alphabetical order
- Ensures consistent vocabulary

**`/spec-sync-spec path=...`**
- Sync spec FROM code (code → spec)
- Updates architecture, glossary, change log
- Only modifies relevant sections

### Code Generation

**`/spec-code-new feature="..." [path=...]`**
- Create new code FROM spec (spec → code)
- Greenfield work: new modules, features, services
- Can create projects and run CLI commands
- Works cooperatively: analyze → confirm → execute

**`/spec-code-change feature="..." [path=...]`**
- Modify existing code FROM spec (spec → code)
- Incremental changes: add features, fix issues
- Preserves patterns and style
- Works cooperatively: analyze → confirm → execute

---

## Typical Workflow

### 1. Start with the spec
```bash
/spec-overview
# Cooperatively write 1-3 page system overview

/spec-invariants add
# Add hard constraints (layering, formats, etc.)

/spec-glossary add Task
# Define domain terms
```

### 2. Generate code from spec
```bash
/spec-code-new feature="Task tagging system" path=src/tasks
# Create new features grounded in the spec

/spec-code-change feature="Add priority field" path=src/domain
# Modify existing code while respecting invariants
```

### 3. Keep spec in sync with reality
```bash
/spec-sync-spec path=src/tasks
# Update spec to reflect what code actually does
```

### 4. Maintain as you go
- When architecture changes, update `change-log.md`
- When new terms emerge, add to `glossary.md`
- When new constraints arise, add to `invariants.json`

---

## Key Concepts

### Overview
A **narrative document** (not a rigid spec) describing:
- System purpose and scope
- Module responsibilities
- Key flows
- Technology choices
- Constraints and risks

Keep it **1-3 pages**. Reference invariants instead of duplicating them.

### Invariants
**Hard rules** in machine-readable JSON format:
- Layering constraints
- ID/date formats
- Error handling patterns
- Cross-cutting requirements

LLM agents must follow these. If code violates an invariant, agents should follow the invariant and note the conflict.

### Glossary
**Shared vocabulary** for consistency:
- Domain terms and definitions
- Naming conventions
- Relationships between concepts

Used by both humans and agents when writing code.

### Change Log
**Short history** of meaningful spec changes:
- When architecture evolved
- When invariants were added/changed
- Context for why changes were made

Helps understand how the system evolved.

---

## Bidirectional Sync

specd supports both directions:

**Spec → Code**:
- `/spec-code-new` - create new features
- `/spec-code-change` - modify existing code

**Code → Spec**:
- `/spec-sync-spec` - update spec to match reality

This keeps spec and code aligned without heavy process.

---

## For LLM Agents

If you're an LLM agent:

1. **Always read** `agent-contract.md` first
2. **Load these files** before any code work:
   - `overview.md`
   - `invariants.json`
   - `glossary.md`
3. **Follow invariants strictly** (hard rules)
4. **Use glossary terms** for naming
5. **Match existing patterns** in the codebase

See [../CLAUDE.md](../CLAUDE.md) for full agent guide.
