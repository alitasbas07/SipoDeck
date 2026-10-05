# Lifecycle and retries

Primary states: `RECEIVED -> PLAN_DISCOVERY -> PLAN_VALIDATION -> ASSESSED -> EXPLORATION -> DEVELOPMENT -> TESTING -> FINAL_REVIEW -> READY_FOR_ACCEPTANCE -> ACCEPTED -> COMMIT_AUTHORIZED -> COMPLETED`.

Correction loop: `TESTING -> DIAGNOSIS -> CORRECTION_1/2/3 -> TESTING`. The initial failed test is not a correction attempt; each developer correction consumes one. Changing model, developer or error message does not reset the counter. Stop after three failed corrections. A fourth requires explicit user override.

Waiting states: `WAITING_USER`, `WAITING_RESOURCE`, `PAUSED_BY_USER`, `BLOCKED`, `STOPPED`. Environment failures do not consume corrections. Suspected flaky tests get one validation rerun. Unknown external outcomes require reconciliation, never blind retry.

Fingerprint: command + failing suite/test + error class/code + stable stack frame + reviewer root cause, excluding timestamps, random IDs and temp paths. Acceptance without commit is `ACCEPTED_UNCOMMITTED`. Never auto-delete a worktree.

