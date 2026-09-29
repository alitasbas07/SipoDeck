# Task 017 — Profil Yönetimi

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Kullanıcının profilleri ve tuş görevlerini arayüzden oluşturabilmesini, düzenleyebilmesini, silebilmesini ve aktif profili seçebilmesini sağlamak.

Bu ekranla birlikte `profiles.json` dosyasını elle düzenleme ihtiyacı ortadan kalkmalıdır.

## Onaylanan Teknik Kararlar

- Ekran Task 015 iskeletindeki Profiller sayfasına yerleşecektir.
- MVVM yapısı için CommunityToolkit.Mvvm kullanılacaktır.
- Değişiklikler açık **Kaydet** ile uygulanacaktır; İptal taslağı geri alır.
- Doğrulama ve kaydetme Task 014 yönetim API'si üzerinden yapılacaktır.
- Mevcut `ActionConfig` JSON türleri aynen kullanılacak; profil JSON formatı değişmeyecektir.
- Yeni NuGet paketi eklenmeyecektir.

## Karar Bekleyen Konular

### Tuş Sayısı ve Düzeni

Cihazın `hello` mesajı tuş sayısını bildirmemektedir. Task başlangıcında kullanıcıya karar formatıyla sorulacaktır.

Seçenek 1: **Serbest tuş listesi**

- Kullanıcı "Tuş ekle" ile istediği tuş numarasını ekler.
- Protokol ve ayar değişikliği gerekmez; görsel tuş ızgarası olmaz.

Seçenek 2: **Ayarlarda sabit tuş sayısı**

- Ayarlara tuş sayısı eklenir; profil ekranı bu sayıda tuş ızgarası gösterir.
- Ayar modeline yeni alan eklenir (geriye dönük uyumlu).

Seçenek 3: **Protokole `button_count` eklemek**

- Cihaz `hello` mesajında tuş sayısını bildirir; ızgara otomatik oluşur.
- Protokol ve firmware değişikliği gerektirir.

Karar verilmeden tuş düzenleyici görünümü geliştirilmemelidir.

## Ekran Bölümleri

### Profil Listesi

- Profilleri listele; aktif profil belirgin gösterilmelidir.
- Profil oluştur.
- Profili yeniden adlandır.
- Profili etkinleştir / pasifleştir.
- Profili sil (onay istenir; son profil silinemez).
- Profili aktif yap.

### Tuş Görevleri

- Tuş seçilir; her tuş için:
  - Normal eylem (tuş bırakılınca çalışır).
  - Uzun basma eylemi (isteğe bağlı).
- Eylem türü seçilir ve türe göre alanlar gösterilir:
  - Program çalıştır: dosya yolu (gözat), argümanlar, çalışma klasörü.
  - URL aç: adres.
  - Bekle: süre (ms).
  - Bildirim: başlık, mesaj.
  - Ses: komut.
  - Medya: komut.
  - Zincir: sıralı eylem listesi (ekle, sil, yukarı/aşağı taşı).
- Tuşun görevini temizleme.

### Kombinasyonlar

- Kombinasyon ekle: en az 2 tuş + eylem.
- Kombinasyonu düzenle ve sil.

## Davranış Kuralları

- Sayfa açıldığında profiller Runtime'dan okunur ve taslağa kopyalanır.
- Kaydet'te doğrulama hatası dönerse hata ilgili alanın yanında gösterilmelidir.
- Kaydedilmemiş değişiklik varken sayfa veya profil değişiminde kullanıcı uyarılmalıdır.
- Aktif profil değişimi kalıcıdır (Task 014).
- Bir tuş hem tekli eylem hem kombinasyon içinde kullanılabilir; mevcut Input Engine önceliği (FN → kombinasyon → tekli) değişmez.
- Ses, medya ve bildirim eylemlerinin gerçek davranışı henüz temel yapı düzeyindedir; arayüzde seçilebilir olmaları bu davranışı değiştirmez.

## Geliştirme Adımları

### 1. Tuş Düzeni Kararı

- Tuş sayısı ve düzeni kararını kullanıcıya sor.

### 2. ViewModel'ler

- Profil listesi, profil düzenleyici, tuş görevi, eylem düzenleyici ve kombinasyon ViewModel'lerini oluştur.
- Eylem türüne göre düzenleyici seçimini `ActionConfig` türleriyle eşle.

### 3. Görünümler

- Profil listesi, tuş görevleri ve kombinasyon görünümlerini oluştur.
- Eylem türüne özel alan şablonlarını oluştur.
- Zincir düzenleyicide sıralama işlemlerini uygula.

### 4. Kaydetme ve Doğrulama

- Kaydet / İptal ve kaydedilmemiş değişiklik korumasını uygula.
- Task 014 alan hatalarını ilgili alanlara bağla.

## Kapsam Dışı

- Profil içe/dışa aktarma.
- Eylemi arayüzden "test et" butonu.
- Plugin eylemleri.
- Yeni eylem türleri.
- Ses, medya ve bildirim eylemlerinin gerçek sistem davranışı.
- Uygulamaya göre otomatik profil değiştirme.

## Riskler ve Koruma Kuralları

- Mevcut `profiles.json` dosyaları sorunsuz açılmaya devam etmelidir.
- Geçersiz profil kaydedilmemeli; çalışan profil bozulmamalıdır.
- Profil düzenlenirken çalışan eylem zinciri kesilmemelidir.
- Input Engine değerlendirme sırası değiştirilmemelidir.
- Program çalıştırma yolu kullanıcıdan alınır; arayüz kendiliğinden program çalıştırmamalıdır.

## Kabul Kriterleri

- [ ] Tuş düzeni kararı kullanıcı tarafından verildi ve uygulandı.
- [ ] Profil oluşturulabiliyor, yeniden adlandırılabiliyor, etkin/pasif yapılabiliyor ve silinebiliyor.
- [ ] Son profil silinemiyor.
- [ ] Aktif profil seçilebiliyor ve kalıcı.
- [ ] Tuşlara normal ve uzun basma eylemi atanabiliyor.
- [ ] Bütün mevcut eylem türleri düzenlenebiliyor.
- [ ] Eylem zinciri oluşturulabiliyor ve sıralanabiliyor.
- [ ] Kombinasyonlar eklenip düzenlenebiliyor.
- [ ] Kaydet doğrulama yapıyor; İptal taslağı geri alıyor.
- [ ] Kaydedilen profil değişiklikleri uygulama yeniden başlatılmadan çalışıyor.
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

Bu task, profil ve tuş görevleri arayüzden yönetilebildiğinde, otomatik testler geçtiğinde ve kullanıcı ekranı onayladığında tamamlanmış sayılacaktır.

Tuş görevlerinin gerçek cihazla denenmesi Task 010 kapsamında açık kalan fiziksel testlerle birlikte yapılabilir; bu task'ın tamamlanmasını engellemez.
