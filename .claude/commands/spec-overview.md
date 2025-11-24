Plan or edit the living architecture specification (`spec/living-architecture.md`) cooperatively.

**What this command does:**
1. Loads `spec/living-architecture.md` (or uses `spec/living-architecture-template.md` if missing).
2. Optionally loads `spec/invariants.json`, `spec/glossary.md`, and `spec/change-log.md` for additional context.
3. Shows the current overview and asks what area to focus on (if not specified).
4. Helps you draft or refine specific sections (Purpose, Modules, Flows, etc.) while staying small and narrative.
5. Suggests follow-up actions (e.g., updating invariants, glossary, or creating tasks).

**Usage:**
```text
/spec-overview         # Create or refine the overall living architecture
/spec-overview Modules # Focus only on the modules/components section
/spec-overview Flows   # Focus on key flows
```

---

**Implementation instructions for Claude Code:**

## When Invoked

1.  **Load or initialize files:**
    
    - Try to open spec/living-architecture.md.        
    - Try to open spec/change-log.md.
    - If not found:        
        - Try spec/living-architecture-template.md.            
        - If template exists, copy its contents into a new spec/living-architecture.md.            
        - **If template not found:** Inform user to run `install.sh` first and exit with error message:
            ```
            Error: Templates not found. Please run the install script first:

            bash .specd/install.sh

            This will copy templates to your project's spec/ directory.
            ```
    - Try to open spec/change-log.md
        - If not found, try spec/change-log-template.md
        - **If template not found:** Same error message as above
    - Optionally (non-fatal if missing):
        - Load spec/invariants.json.
        - Load spec/glossary.md.
            
2.  **Determine focus:**    
    - If the user passed a argument, treat that as the primary editing target.        
    - Otherwise:        
        - Briefly summarize the existing document in a few bullet points.            
        - Propose 2–4 candidate areas to improve or fill in.            
        - Ask the user which area they want to work on.
            
3.  **Edit cooperatively:**    
    - Show the current section content (or a short excerpt if long).        
    - Ask clarifying questions only when necessary.        
    - Propose a revised version of the section that:        
        - Stays concise and narrative.            
        - References invariants when relevant but does not duplicate them.            
        - Uses existing terminology from spec/glossary.md when present.
            
    - Once agreed, write the updated section back to spec/living-architecture.md.

4.  **Update change log:**
    - If this is the first initialization of the living architecture, initialize the change-log template and save to spec/change-log.md.
    - If this is a meaningful architectural change, suggest appending an entry to spec/change-log.md.
        
4.  **Suggest next steps:**    
    - If new constraints or hard rules were introduced in the narrative, suggest adding or updating entries in spec/invariants.json via /spec-invariants.        
    - If new terms were coined or identified, suggest updating /spec-glossary.