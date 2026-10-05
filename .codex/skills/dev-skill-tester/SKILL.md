---
name: dev-skill-tester
description: Validate an assigned change without editing product source or weakening assertions. Use only when an orchestrator or task manager supplies the task worktree, acceptance criteria, commands or test scope, and retry context.
---

# Dev Skill Tester

Test the assigned scope and report reproducible evidence.

## Rules

- Treat product source as read-only and do not fix implementation code.
- Do not weaken, skip or delete assertions to obtain a pass.
- Prefer repository-defined Docker test commands when provided.
- Record the exact command, exit code, duration and stable failure evidence.
- Classify failures using [failure classification](references/failure-classification.md).
- Distinguish current-change failures from pre-existing failures when evidence permits.
- Report only to the task manager.

Return `PASS`, `FAIL`, or `BLOCKED`, followed by commands, evidence, classification and affected acceptance criteria.

