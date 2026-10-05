# Scoring and routing

Difficulty scores code scope, dependencies, ambiguity, implementation complexity and test complexity from 0-2 each. Risk scores business criticality, data/security impact, contract/schema impact, blast radius and reversibility from 0-2 each.

Risk floors: payment/security/authorization/sensitive data at least 8; database/API/tenant isolation at least 7; irreversible or unknown external outcome at least 9. Simple isolated text or UI may stay at most 3 only without critical effects. Score each phase; overall score is the highest phase. Include evidence and confidence.

Bands: 1-3 easy/low, 4-6 medium, 7-8 hard/high, 9-10 very hard/critical.

Model tiers: Efficient for discovery/mechanical work; Standard for normal development/testing; Strong for hard development/diagnosis/deep review; Advisor for critical work only with user approval.

Default mapping: Claude Efficient Haiku low/medium, Standard Sonnet medium, Strong Sonnet high or Opus high, Advisor Fable high/xhigh. Codex Efficient GPT-6 Luna low/medium, Standard GPT-6.1 Sol medium, Strong GPT-6.1 Sol high, Advisor GPT-6 Astra high/xhigh/max.

Never silently downgrade or use paid API fallback. Log requested and resolved models. Treat unknown limits as unknown. Limit bands: 50%+ normal, 25-49% saving, 10-24% restricted, below 10% no new agent and inform the user. V1 historical performance is logged but does not affect routing.

