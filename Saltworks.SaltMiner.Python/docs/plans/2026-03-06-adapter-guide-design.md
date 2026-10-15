# Design: Adapter Guide System
**Date:** 2026-03-06
**Author:** Lead Developer
**Status:** Approved

---

## Problem

Creating a new source adapter requires deep knowledge of SaltMiner's data model, queueing pattern, DTO structure, and project conventions. That knowledge currently lives in one developer's head. When delegating adapter development — to a teammate or to Claude Code — there is no single artifact that conveys all of it.

## Goal

Produce two reusable documents that allow a teammate (or Claude Code) to build a fully functional, production-ready source adapter by supplying only:
- The vendor's API documentation URL(s)
- Source name, instance name, assessment type
- Auth method
- Whether issues should be replaced on each sync
- Optional: GUI URL pattern

## Solution: Option A — Prompt + Reference Doc

### Artifact 1: `docs/adapter-guide/ADAPTER_REFERENCE.md`

The comprehensive "textbook." Contains the complete architectural knowledge needed to build an adapter correctly: data model, DTO fields, queueing pattern, state tracking, configuration format, naming conventions, assessment type catalog, and annotated code skeletons. Maintained in the repo — updated as patterns evolve.

### Artifact 2: `docs/adapter-guide/KICKOFF_PROMPT.md`

A lean fill-in-the-blank template. A teammate fills in the 6–7 adapter-specific values and pastes the completed document to Claude Code. The prompt instructs Claude to read the reference doc first, then build all required files.

---

## Files Created

```
docs/adapter-guide/
├── ADAPTER_REFERENCE.md    ← Claude's reference (this is the "mini me")
└── KICKOFF_PROMPT.md       ← Teammate-facing template
```

## Files Per New Adapter (Claude's Output)

```
Sources/<SourceName>/
├── <SourceName>Client.py
└── <SourceName>Adapter.py

Config/Sources/<SourceName>.json
Run<SourceName>Adapter.py
```

---

## Why Not the Other Options

| Option | Rejected Because |
|--------|-----------------|
| Checklist/Template only | Embeds all knowledge inline — template bloats, harder to maintain, Claude gets less context |
| Extended CLAUDE.md | Pollutes project-level Claude instructions; requires repo access for every teammate |
