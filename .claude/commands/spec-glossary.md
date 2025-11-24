View or edit the system glossary (`spec/glossary.md`).

**What this command does:**
1. Loads `spec/glossary.md` (or initializes it from `spec/glossary-template.md`).
2. Summarizes current terms.
3. Helps you add or refine definitions.
4. Encourages reuse of existing terms to keep vocabulary consistent.

**Usage:**
```text
/spec-glossary              # Show glossary and suggest edits
/spec-glossary Recipe       # Show definition of "Recipe"
/spec-glossary add Recipe   # Add/update "Recipe" definition
```

---

**Implementation instructions for Claude Code:**

## When Invoked

### Mode Detection

Parse the command arguments to determine mode:

1. **No arguments:** List all terms
2. **One argument (term name):** Show specific term
3. **"add" + term name:** Add or update term

### Phase 1: Load or Create Glossary

1. **Try to load existing glossary:**
   - Try to open `spec/glossary.md`
   - If found, parse terms
   - If not found, CREATE mode

2. **If not found:**
   - Check for template `spec/glossary-template.md`
   - If template missing, inform user to run `install.sh`

### Phase 2A: LIST MODE (No Arguments)

Show all terms in the glossary:

```
System Glossary
===============

Found 5 terms:

- **Batch**: A production run of a specific Recipe, tracked for quality control
- **Formulation**: Synonym for Recipe; the exact specification of ingredients and parameters
- **Recipe**: A formulation specification including ingredients, quantities, and production parameters
- **SKU**: Stock Keeping Unit; a unique identifier for a finished product
- **Yield**: The amount of finished product produced from a Batch

To see details: /spec-glossary [term]
To add/update: /spec-glossary add [term]
```

If glossary doesn't exist or is empty:
```
No glossary found.

A glossary helps maintain consistent domain terminology across specs.

Create your first term: /spec-glossary add [term-name]
```

### Phase 2B: SHOW MODE (One Argument)

Show specific term definition:

1. **Find term (case-insensitive search)** in both table and concept sections
2. **Show full definition with context:**

```
Recipe
======

**Table definition:**
A formulation specification with ingredients and parameters

**Detailed concept:**
A formulation specification that includes ingredient lists with precise quantities,
production parameters (temperature, time, mixing speeds), expected yield, and
version history for regulatory traceability (FDA 21 CFR Part 11). Used to create
production Batches.

**Related terms:** Formulation (synonym), Batch

---
Found in: spec/glossary.md
Last updated: 2025-11-24
```

If term not found:
```
Term "Recipe" not found in glossary.

Did you mean one of these?
- Formulation
- Batch

Add it: /spec-glossary add Recipe
```

### Phase 2C: ADD MODE (add + term)

Add or update a term:

1. **Check if term already exists:**
   - If exists in table or concepts: "Recipe is already defined. Update it?"
   - If new: "Adding new term: Recipe"

2. **Determine placement:**
   ```
   Is this a:
   1. Simple term (quick definition in table)
   2. Complex concept (detailed explanation needed)
   3. Both (brief table entry + detailed concept section)
   ```

3. **Work cooperatively:**
   - For table entries: Ask for concise definition and related terms
   - For concept sections: Ask which concept group it belongs to (or suggest creating one)
   - Ask about synonyms, related terms, or notes
   - Show drafted entry (table row and/or concept bullet)
   - Confirm before saving

4. **Update glossary:**
   - If glossary doesn't exist, create from template
   - For table entries: Add row in **alphabetical order**
   - For concept sections: Add to appropriate section or create new concept group
   - Update `updated` date in front matter
   - Save file

5. **Report completion:**
   ```
   ✓ Added "Recipe" to glossary table
   ✓ Added detailed concept to "Production Concepts" section
   ✓ Updated spec/glossary.md

   View all terms: /spec-glossary
   ```

## Glossary Format

The glossary uses a simple table format for quick reference terms, plus optional concept groupings for complex domain concepts:

```markdown
---
title: Glossary
updated: 2025-11-24
---

# System Glossary

> Define the core vocabulary of the system.
> Keep this tight and focused on terms that matter for humans and agents.

| Term         | Definition | Notes / Related |
|--------------|------------|-----------------|
| `Batch`      | A production run of a specific recipe | Related: Recipe, SKU |
| `Recipe`     | A formulation specification with ingredients and parameters | Synonym: Formulation |
| `SKU`        | Stock Keeping Unit; unique product identifier | |

## Domain-specific concepts

### Production Concepts

- **Recipe:** A formulation specification that includes ingredient lists with precise quantities, production parameters (temperature, time, mixing speeds), expected yield, and version history for regulatory traceability.
- **Batch:** A production run that executes a specific Recipe, tracked for quality control and traceability.
```

**When to use table vs. concept sections:**
- **Table:** Simple terms, IDs, abbreviations, quick lookups
- **Concept sections:** Complex domain concepts that need detailed explanation

## Smart Suggestions

When adding a term, make smart suggestions based on domain:

- **Manufacturing/Food:** Suggest related terms (Batch, SKU, Yield, Quality Control)
- **Finance:** Suggest (Portfolio, Position, Settlement, Counterparty)
- **Healthcare:** Suggest (Patient, Encounter, Diagnosis, Treatment Plan)
- **SaaS:** Suggest (Tenant, Subscription, User, Workspace)

Ask user whether the term is:
- A simple term (add to table)
- A complex concept (add to concept section)
- Both (add brief version to table, detailed version to concept section)

## Example Interaction - ADD MODE

```
User: /spec-glossary add Recipe

Claude: Adding new term "Recipe" to your glossary.

Is this a:
1. Simple term (quick definition in table)
2. Complex concept (detailed explanation needed)
3. Both (brief table entry + detailed concept section)

User: Both - it's important but needs detail for FDA compliance

Claude: Got it. Let me draft entries for both sections.

First, the table entry - a concise definition:

User: A formulation specification with ingredients and production parameters

Claude: Perfect. Now for the detailed concept section, what additional context is important?

User: Include that it has version history for FDA compliance and is used to create Batches

Claude: Here's what I'll add:

**Table entry:**
| `Recipe` | A formulation specification with ingredients and parameters | Synonym: Formulation, Related: Batch |

**Concept section (under "Production Concepts"):**
- **Recipe:** A formulation specification that includes ingredient lists with precise quantities, production parameters (temperature, time, mixing speeds), expected yield, and version history for regulatory traceability (FDA 21 CFR Part 11). Used to create production Batches.

Does this look correct?

[User confirms]

✓ Added "Recipe" to glossary table
✓ Added detailed explanation to "Production Concepts" section
✓ Updated spec/glossary.md

View glossary: /spec-glossary
```

## Example Interaction - LIST MODE

```
User: /spec-glossary

Claude: System Glossary
===============

Quick Reference (4 terms):

| Term         | Definition |
|--------------|------------|
| `Batch`      | A production run of a specific recipe |
| `Recipe`     | A formulation specification with ingredients and parameters |
| `SKU`        | Stock Keeping Unit; unique product identifier |
| `Yield`      | Amount of finished product from a batch |

Domain Concepts:

**Production Concepts**
- Recipe (detailed formulation specification)
- Batch (production run with traceability)

To see details: /spec-glossary Recipe
To add term: /spec-glossary add [term]
```
