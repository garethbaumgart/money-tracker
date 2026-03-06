# Developer Guide

This guide defines the core architectural patterns and workflow rules that coding agents and contributors must follow when working in this repository.

It is intentionally short. The canonical standards live in the main architecture documents.

## Primary Standards

Backend architecture  
→ `docs/Backend-DDD-Vertical-Slice-Standards.md`

Application build guardrails  
→ `docs/App-Build-GuideRails.md`

Agent workflow and skills  
→ `AGENTS.md`

Workflow execution contract (startup, routing, PR modes)  
→ `docs/dev-guide/workflow-catalog.md`

## Principles

### Follow existing patterns

Do not invent new architectural patterns. Use the standards documents and templates.

### Stay within a lane

Workers must operate within one lane per task.

Backend lane → `backend/**`  
Mobile lane → `mobile/**`  
Platform lane → `.github/**` and root tooling

Cross-lane work must be explicitly approved.

### Use templates and skills

Backend features should start from the vertical slice template.

`backend/templates/vertical-slice-template/`

UI features should follow the Flutter theming standard.

PR workflows should use the `$github-pr` skill as defined in the workflow catalog.

### Prefer minimal correct change

Avoid drive-by refactors or unrelated improvements.

Keep PRs small and atomic.

### Verification is mandatory

All changes require tests and verification evidence before creating a pull request. See the [Verification guide](docs/dev-guide/verification.md)

Verification in this guide uses the canonical contract from:

- `docs/dev-guide/verification.md` for detailed requirements.

### When patterns are unclear

Stop and propose options rather than inventing a new pattern.
