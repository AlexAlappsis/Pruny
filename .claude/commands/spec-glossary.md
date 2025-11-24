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

1. **Find term (case-insensitive search)**
2. **Show full definition with context:**

```
Recipe
======

**Definition:**
A formulation specification that includes:
- List of ingredients with precise quantities
- Production parameters (temperature, time, mixing speed)
- Expected yield and quality criteria
- Version history for traceability

**Related Terms:**
- Formulation (synonym)
- Batch (uses a Recipe)

**Used in:**
- FEAT-0001: Recipe Management
- COMP-0002: Recipe Database
- IMPL-0003: Recipe ORM Models

---
Last updated: 2025-11-19
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
   - If exists: "Recipe is already defined. Update it?"
   - If new: "Adding new term: Recipe"

2. **Prompt for definition:**
   ```
   Let's define "Recipe" for your glossary.

   Please provide:
   1. A concise one-line summary
   2. Detailed definition (optional)
   3. Related terms or synonyms (optional)
   ```

3. **Work cooperatively:**
   - Ask for summary
   - Ask if more detail needed
   - Ask about related terms, synonyms, or acronyms
   - Show drafted entry
   - Confirm before saving

4. **Update glossary:**
   - If glossary doesn't exist, create from template
   - Add term in **alphabetical order**
   - Update `last_updated` in front matter
   - Save file

5. **Report completion:**
   ```
   ✓ Added "Recipe" to glossary
   ✓ Updated spec/glossary.md

   View all terms: /spec-glossary
   ```

## Glossary Format

Terms should be added in alphabetical order using this format:

```markdown
### Recipe

**Summary:** A formulation specification including ingredients, quantities, and production parameters.

**Details:**
A Recipe defines exactly how to manufacture a product, including:
- Ingredient list with precise quantities and tolerances
- Production parameters (temperature, time, mixing speeds, etc.)
- Expected yield and quality criteria
- Version history for regulatory traceability (FDA 21 CFR Part 11)

**Related Terms:**
- Formulation (synonym)
- Batch (a production run of a Recipe)

**Acronyms:** None
```

## Smart Suggestions

When adding a term, make smart suggestions based on domain:

- **Manufacturing/Food:** Suggest related terms (Batch, SKU, Yield, Quality Control)
- **Finance:** Suggest (Portfolio, Position, Settlement, Counterparty)
- **Healthcare:** Suggest (Patient, Encounter, Diagnosis, Treatment Plan)
- **SaaS:** Suggest (Tenant, Subscription, User, Workspace)

## Cross-Reference Discovery

When adding a term, scan specs to find where it's used:

1. Search `spec/charter/` for term mentions
2. Search `spec/architecture/` for term mentions
3. Search `spec/implementation/` for term mentions
4. Add "Used in" section listing relevant IDs

Example:
```markdown
**Used in:**
- FEAT-0001: Recipe Management & Versioning
- COMP-0003: Recipe Database Component
- IMPL-0005: Recipe ORM Models
```

## Example Interaction - ADD MODE

```
User: /spec-glossary add Recipe

Claude: Adding new term "Recipe" to your glossary.

Let's define it. First, a concise one-line summary:

User: A formulation specification with ingredients and production parameters

Claude: Good! Would you like to add more detailed definition, or is the summary sufficient?

User: Add more detail - needs to include version history for FDA compliance

Claude: Got it. Here's the full entry I'll add:

---
### Recipe

**Summary:** A formulation specification including ingredients, quantities, and production parameters.

**Details:**
A Recipe defines exactly how to manufacture a product, including:
- Ingredient list with precise quantities
- Production parameters (temperature, time, mixing speeds)
- Expected yield and quality criteria
- Version history for regulatory traceability (FDA 21 CFR Part 11)

**Related Terms:**
- Formulation (synonym)
- Batch (a production run using this Recipe)

**Used in:**
- FEAT-0001: Recipe Management & Versioning
- FEAT-0002: Production Cost Analysis
- COMP-0002: Recipe Database
---

Does this look correct?

[User confirms]

✓ Added "Recipe" to glossary (alphabetically sorted)
✓ Updated spec/glossary.md

Your glossary now has 1 term. Add more: /spec-glossary add [term]
```

## Example Interaction - LIST MODE

```
User: /spec-glossary

Claude: System Glossary
===============

Found 4 terms:

- **Batch**: A production run of a specific Recipe
- **Formulation**: Synonym for Recipe
- **Recipe**: A formulation specification including ingredients and production parameters
- **SKU**: Stock Keeping Unit; unique identifier for finished product

To see details: /spec-glossary Recipe
To add term: /spec-glossary add Yield
```

## Related Commands

- `/spec-plan-charter` - Plan charter (suggests glossary terms)
- `/spec-plan-arch` - Plan architecture (suggests technical terms)
- `/spec-plan-impl` - Plan implementation (suggests technical terms)
