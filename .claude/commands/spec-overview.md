Plan or edit the overview specification (`spec/overview.md`) cooperatively.

**What this command does:**
1. Loads `spec/overview.md` (or uses `spec/overview-template.md` if missing).
2. Optionally loads `spec/invariants.json`, `spec/glossary.md`, and `spec/change-log.md` for additional context.
3. Shows the current overview and asks what area to focus on (if not specified).
4. Helps you draft or refine specific sections (Purpose, Modules, Flows, etc.) while staying small and narrative.
5. Suggests follow-up actions (e.g., updating invariants, glossary, or creating tasks).

**Usage:**
```text
/spec-overview         # Create or refine the overall overview
/spec-overview Modules # Focus only on the modules/components section
/spec-overview Flows   # Focus on key flows
```

---

**Implementation instructions for Claude Code:**

## When Invoked

1.  **Load or initialize files:**
    
    - Try to open spec/overview.md.        
    - Try to open spec/change-log.md.
    - If not found:        
        - Try spec/overview-template.md.            
        - If template exists, copy its contents into a new spec/overview.md.            
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
            
    - Once agreed, write the updated section back to spec/overview.md.

4.  **Update change log:**
    - If this is the first initialization of the overview, initialize the change-log template and save to spec/change-log.md.
    - If this is a meaningful architectural change, suggest appending an entry to spec/change-log.md.

5. **Suggest invariants and glossary updates**
    - If new constraints or hard rules were introduced in the narrative briefly identify them.
    - If new terms were coined or identified, briefly identify them.
    - Suggest adding or updating entries in either spec/invariants.md or spec/glossary.md. Create them from templates if they don't exist.
    - Remind user they can update them manually via /spec-invariants and /spec-glossary.        
        
6.  **Suggest next steps:**    
    - If there are open questions from planning, suggest areas of research to explore..
    - Suggest 2-3 concrete implementation options using /spec-code-new or /spec-code-change.
