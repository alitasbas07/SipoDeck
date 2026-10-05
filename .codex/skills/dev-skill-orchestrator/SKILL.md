---
name: dev-skill-orchestrator
description: Coordinate plan-led software tasks across Orca, Claude Code and Codex. Use when a user asks to start development from local plan markdown and ClickUp or Flowdo, score task difficulty and risk, route manager/developer/tester/reviewer roles, create one worktree per task, supervise retry limits, or report acceptance and commit gates.
---

# Dev Skill Orchestrator

Act as the only user-facing coordinator. Begin every user message with `Görev: <task name>`.

## Non-negotiable rules

- Enforce one task = one branch = one Orca worktree.
- Never commit until the user explicitly says commit is allowed.
- Never merge.
- Treat “tamam olmuş” as acceptance, not commit permission.
- Keep agents inside the approved plan scope.
- Stop after three correction attempts for the same phase.
- Require user approval before advisor-tier models.
- Never invent subscription limits, test results, plan matches or evidence.

## Workflow

1. Parse the task name, plan location and tracker reference from natural language.
2. Resolve local plan files and the ClickUp or Flowdo main task plus subtasks.
3. Compare both sources. Stop in `WAITING_USER` for ambiguity, conflict or missing required information.
4. Score each phase and the full task using [scoring and routing](references/scoring-and-routing.md).
5. Use reviewer `EXPLORATION` when dependencies, affected code or unclear requirements need evidence.
6. Present critical questions. Start implementation only after the user authorizes development.
7. Create or reuse exactly one Orca worktree for the task. Update its comment and workspace status at meaningful checkpoints.
8. Dispatch explicit role briefs using [briefs and reports](references/briefs-and-reports.md).
9. Run development, testing and cross-model final review through the lifecycle rules.
10. Report `READY_FOR_ACCEPTANCE` with code, test and review evidence kept separate.
11. After acceptance, wait for explicit commit permission. Preserve the worktree for user inspection.

## Single-task and multi-task mode

For one active task, combine coordinator and task-manager responsibilities. If a second task arrives while the first is active, pause at a safe checkpoint, retain a dedicated manager for each task, and operate only as coordinator above them. Do not place two tasks in one worktree.

## Required references

- [Intake and validation](references/intake-and-plan-validation.md)
- [Scoring and routing](references/scoring-and-routing.md)
- [Lifecycle and retries](references/lifecycle-and-retries.md)
- [Briefs and reports](references/briefs-and-reports.md)
- [Multi-task Orca](references/multi-task-orca.md)
- [Logging schema](references/logging-schema.md)

Write local logs with `scripts/write-event.ps1`. Do not put runtime logs in project repositories.

