# Logging schema

Store logs outside repositories at `%USERPROFILE%\.agent-orchestrator\runs\<run-id>\events.jsonl` with a `summary.json`.

Events contain timestamp, run/task IDs, event, state, actor, model, effort, difficulty, risk, retry, error class/fingerprint, evidence references and a small data object. Record plan checks, scoring, routing, worktree and worker lifecycle, implementation, tests, reviews, corrections, user decisions, commit authorization and completion. There is no merge event.

Never log secrets, environment values, card data, personal data, full source or full prompts. Store long evidence separately. Risk 7+ cannot complete without a summary.

Dashboard summaries may include `task_name`, `test_status`, `review_status` and numeric `metrics.total_tokens` / `metrics.duration_seconds`. Record unknown values as null, not zero. Count all manager and worker token usage; do not convert subscription remaining percentages into tokens. Emit `correction_started` once per developer correction. The installed `dashboard/Build-Dashboard.ps1 -Open` builds the local HTML snapshot; see `dashboard/data-contract.md` for benchmark records.
