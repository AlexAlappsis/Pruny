View or edit the system invariants (`spec/invariants.json`).

**What this command does:**
1. Loads `spec/invariants.json` (or initializes it from `spec/invariants-template.json`).
2. Shows a summarized list of invariants grouped by category.
3. Lets you add, edit, or deprecate invariants.
4. Ensures the JSON stays valid and stable over time.

**Usage:**
```text
/spec-invariants                  # List and edit invariants interactively
/spec-invariants add              # Add a new invariant
/spec-invariants edit 2    # Edit a specific invariant by id
/spec-invariants deprecate 3 # Mark an invariant as deprecated by id
```

---

**Implementation instructions for Claude Code:**

## When Invoked

1.  **Load or initialize files:**
    
    - Try to open spec/invariants.json.        
    - If not found:        
        - Try spec/invariants-template.md.            
        - If template exists, copy its contents into a new spec/invariants.md.            
        - **If template not found:** Inform user to run `install.sh` first and exit with error message:
            ```
            Error: Templates not found. Please run the install script first:

            bash .specd/install.sh

            This will copy templates to your project's spec/ directory.
            ```

2.  **Parse invariants:**    
    - Parse the JSON.        
    - Validate that it contains an "invariants" array; if not, create one.        
    - Build an in-memory list keyed by "id".
        
3.  **Determine mode and edit cooperatively:**    
    - If add argument is present:        
        - Ask the user for:            
            - category                
            - summary                
            - details (optional but recommended)                
            - appliesTo (optional list)                
        - Auto-suggest a new id that does not conflict with existing ones.
            
    - If edit ID is present:        
        - Show the current invariant.            
        - Ask what fields to change.
            
    - If deprecate ID is present:        
        - Set status to "deprecated" (create the property if missing).
            
    - If no subcommand is given:        
        - Show a grouped summary (by category) and offer interactive options (add/edit/deprecate).
            
4.  **Write back file:**    
    - Update the in-memory structure.        
    - Serialize the JSON with stable formatting (2-space indentation).        
    - Write back to spec/invariants.json.