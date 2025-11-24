---
title: Living Architecture Spec
updated: 2025-11-10
status: draft
---

<!--
VALIDATION RULES:
- Required fields: title, status, updated
- Status values: draft | active | deprecated
-->

# System Overview

> This is the living, narrative description of the system.
> It should stay small, opinionated, and up to date.
> Think 1–3 pages, not a book.

---

## 1. Purpose & Scope

**System name:**  
**Owner(s):**

### 1.1 Problem & context

- What problem does this system solve?
- For whom?
- In what environment (org, product, game, etc.)?

### 1.2 Goals

- [ ] Primary goal 1
- [ ] Primary goal 2
- [ ] Primary goal 3

### 1.3 Non-goals

- Things this system explicitly will *not* do.

---

## 2. System Boundaries & External Context

### 2.1 External systems

List the external systems / services this system talks to.

| External system | Direction | Purpose / notes |
|-----------------|-----------|-----------------|
|                 | in/out    |                 |

### 2.2 User types

Briefly describe the actors:

- **Actor 1** – what they do
- **Actor 2** – what they do

---

## 3. Architecture Overview

### 3.1 Modules / components

> This is a lightweight map, not a full-blown component spec.

| Module / area | Responsibility | Notes |
|---------------|----------------|-------|
|               |                |       |

### 3.2 Key flows

Describe 2–4 core flows at a narrative level:

- **Flow 1:** Short paragraph of “when X happens, the system does Y…”
- **Flow 2:** …

### 3.3 Data at a glance

List the core domain entities and a sentence each:

- **EntityName** – what it represents and a couple of key fields

---

## 4. Technology & Runtime

- Language / framework(s)
- Data stores
- Message buses / queues
- Hosting / runtime environment

---

## 5. Constraints, Risks & Tradeoffs

### 5.1 Constraints

Tie to invariants where relevant (but don’t repeat them verbatim).

- Latency / throughput constraints
- Regulatory or security constraints
- Deployment constraints

### 5.2 Risks

What are the biggest risks in this design?

- Technical risk 1
- Product risk 2

### 5.3 Open questions

Unresolved decisions, places to spike, etc.

---

## 6. Links

- `/spec/invariants.json`
- `/spec/glossary.md`
- Repos, ADRs, diagrams, etc.
