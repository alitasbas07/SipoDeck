# Multi-task Orca behavior

Use Orca for worktrees, terminals, sessions and observable execution. Use this skill for decisions and policy.

For one task, coordinator and task manager are combined. When another task arrives, pause at a safe checkpoint, ensure each task has its own manager/branch/worktree, keep specialists inside their task worktree, and never mix state or retry counters.

Orca cards: planning `todo`; development/test/retry `in-progress`; final review and acceptance `in-review`; accepted plus committed or explicitly closed without commit `completed`.

Read current Orca CLI documentation at execution time. Prefer `orca worktree create --agent` for agent worktrees and structured Orca orchestration for supervised workers.

