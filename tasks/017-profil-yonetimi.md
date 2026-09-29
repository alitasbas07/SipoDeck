# Task 017 — Profil Yönetimi

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Kullanıcının profilleri arayüzden oluşturabilmesini, düzenleyebilmesini, silebilmesini ve aktif profili seçebilmesini sağlamak.

Tuş atamaları Task 018, eylem ve makro düzenleme Task 019 kapsamındadır. Bu task profil düzeyindeki yönetimle sınırlıdır.

## Onaylanan Teknik Kararlar

- Ekran Task 015 iskeletindeki Profiller sayfasına yerleşecektir.
- MVVM yapısı için CommunityToolkit.Mvvm kullanılacaktır.
- Değişiklikler açık **Kaydet** ile uygulanacaktır; İptal taslağı geri alır.
- Doğrulama ve kaydetme Task 014 yönetim API'si üzerinden yapılacaktır.
- Profil JSON formatı değişmeyecektir.
- Yeni NuGet paketi eklenmeyecektir.

## Ekran Bölümleri

### Profil Listesi

- Profilleri listele; aktif profil belirgin gösterilmelidir.
- Her profilde ad, etkin/pasif durumu ve atanmış tuş sayısı gösterilmelidir.
- Profil oluştur.
- Profili kopyala (tuş atamalarıyla birlikte, yeni kimlikle).
- Profili sil (onay istenir; son profil silinemez).
- Profili aktif yap.

### Profil Detayı

- Profil adını değiştir.
- Profili etkinleştir / pasifleştir.
- Profilin tuş atamalarını düzenlemek için Task 018 ekranına geçiş noktası (018 tamamlanana kadar pasif).

## Davranış Kuralları

- Sayfa açıldığında profiller Runtime'dan okunur ve taslağa kopyalanır.
- Kaydet'te doğrulama hatası dönerse hata ilgili alanın yanında gösterilmelidir.
- Kaydedilmemiş değişiklik varken sayfa veya profil değişiminde kullanıcı uyarılmalıdır.
- Aktif profil değişimi kalıcıdır (Task 014).
- Pasif profil aktif yapılamaz; kullanıcıya nedeni gösterilmelidir.
- Aktif profil pasifleştirilirse ya da silinirse Task 014 kuralına göre başka bir etkin profil aktif olur ve kullanıcı bilgilendirilir.
- FN profil eşleştirmesinde (Task 016) kullanılan bir profil silinmek istenirse kullanıcı uyarılmalıdır.

## Geliştirme Adımları

### 1. ViewModel'ler

- Profil listesi ve profil detay ViewModel'lerini oluştur.
- Oluştur, kopyala, sil, aktif yap, Kaydet ve İptal komutlarını ekle.

### 2. Görünümler

- Profil listesi ve profil detay görünümlerini oluştur.
- Silme onayı ve uyarı pencerelerini ekle.

### 3. Kaydetme ve Doğrulama

- Kaydedilmemiş değişiklik korumasını uygula.
- Task 014 alan hatalarını ilgili alanlara bağla.

## Kapsam Dışı

- Tuş atamaları ve kombinasyonlar (018).
- Eylem düzenleme ve makro zincirleri (019).
- Profil içe/dışa aktarma.
- Uygulamaya göre otomatik profil değiştirme.

## Riskler ve Koruma Kuralları

- Mevcut `profiles.json` dosyaları sorunsuz açılmaya devam etmelidir.
- Geçersiz profil kaydedilmemeli; çalışan profil bozulmamalıdır.
- Profil düzenlenirken çalışan eylem zinciri kesilmemelidir.
- Profil kopyalanırken tuş atamaları derin kopyalanmalı; kopya ile asıl profil birbirini etkilememelidir.

## Kabul Kriterleri

- [ ] Profiller listeleniyor; aktif profil belirgin gösteriliyor.
- [ ] Profil oluşturulabiliyor, kopyalanabiliyor ve silinebiliyor.
- [ ] Son profil silinemiyor.
- [ ] Profil adı ve etkinlik durumu düzenlenebiliyor.
- [ ] Aktif profil seçilebiliyor ve kalıcı.
- [ ] Kaydet doğrulama yapıyor; İptal taslağı geri alıyor.
- [ ] Kaydedilmemiş değişiklikte uyarı veriliyor.
- [ ] Profil değişiklikleri uygulama yeniden başlatılmadan çalışıyor.
- [ ] Profil JSON formatı değişmedi.
- [ ] Mevcut otomatik testler geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, profiller arayüzden yönetilebildiğinde, otomatik testler geçtiğinde ve kullanıcı ekranı onayladığında tamamlanmış sayılacaktır.
