# Severity rubric

- `CRITICAL`: security, payment, authorization, data loss or system-wide correctness failure.
- `HIGH`: material functional defect, tenant leak, contract break or likely production regression.
- `MEDIUM`: scoped correctness, resilience or maintainability issue to fix before acceptance.
- `LOW`: minor limited-impact issue; never demand unrelated polish.

Every finding needs evidence, impact and minimal correction direction. Return `PASS` when there is no actionable finding.

