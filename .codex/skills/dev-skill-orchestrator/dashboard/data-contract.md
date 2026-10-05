# Dashboard veri sözleşmesi

Dashboard mevcut logları okur; agentlardan otomatik token telemetrisi toplamaz. Sayılar yalnızca ölçülmüşse kaydedilir. Eksik ölçümleri `null` kullanarak veya alanı atlayarak belirtin; bilinmeyeni sıfır yazmayın.

## Run

`runs/<run-id>/events.jsonl`: her satır mevcut event sözleşmesinde bir JSON nesnesidir. `correction_started` düzeltme başlangıcını belirtir. `actor`, `model`, `effort`, `evidence_refs` ve `data` task detayında görüntülenir. Log içeriği metin olarak gösterilir; HTML çalıştırılmaz.

`runs/<run-id>/summary.json`:

```json
{
  "task_id": "task-001",
  "task_name": "Örnek görev",
  "state": "READY_FOR_ACCEPTANCE",
  "model": "gerçekte kullanılan model",
  "effort": "medium",
  "difficulty": 6,
  "risk": 4,
  "test_status": "PASS",
  "review_status": "PASS",
  "metrics": {
    "total_tokens": null,
    "duration_seconds": null
  }
}
```

`total_tokens` tüm manager/worker kullanımının doğrulanmış toplamıdır. Cache ve reasoning token sayılarının dahil edilme biçimini `data` veya benchmark koşullarında açıklayın. Abonelik kalan yüzdesi token sayısı yerine geçmez.

## Benchmark

Her çalışmayı `benchmarks/<case-id>/<variant>-<repeat>.json` olarak kaydedin. `variant`, `baseline` veya `devskill` olur. Aynı case/tekrar/variant için tek kayıt bulunmalıdır.

```json
{
  "case_id": "task-001",
  "repeat": 1,
  "variant": "baseline",
  "run_id": "run-001",
  "conditions": {
    "base_commit": "başlangıç SHA",
    "model": "gerçekte kullanılan model",
    "effort": "medium",
    "skill_revision": null,
    "token_source": "session kullanım raporu"
  },
  "metrics": {
    "total_tokens": null,
    "duration_seconds": null,
    "quality_score": null,
    "retry_count": 0,
    "scope_violations": 0,
    "review_findings": 0,
    "user_interventions": 0,
    "test_status": "PASS"
  },
  "evidence_refs": []
}
```

Kalite puanı 0–100 aralığında ortak rubric ve kör review ile verilir. Fark sütunu Dev_skill eksi Baseline değeridir; kalite için yüksek, token/süre/hata için düşük değer tercih edilir. Ekran tek denemeden kazanan ilan etmez. Farklı başlangıç SHA veya model koşullarında tablolar tek başına nedensel karşılaştırma sağlamaz.

Bozuk JSON kayıtları atlanır ve dashboardda uyarı gösterilir. Aynı case/tekrar için yinelenen sonuçlarda karşılaştırma yapılmaz. Veri dosyalarına secret, kart bilgisi veya kişisel veri yazmayın.
