# Failure classification

- `CODE_DEFECT`: implementation violates expected behavior.
- `TEST_DEFECT`: test or fixture is independently incorrect.
- `ENVIRONMENT`: infrastructure, Docker, dependency or setup failure.
- `FLAKY`: non-deterministic failure supported by rerun evidence.
- `EXTERNAL_UNKNOWN`: external outcome cannot be confirmed.
- `PLAN_CONFLICT`: acceptance sources disagree.
- `PREEXISTING`: failure demonstrably predates the change.

Do not assign a class without evidence; report uncertainty when incomplete.

