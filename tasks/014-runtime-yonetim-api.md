# Task 014 — Runtime Yönetim API'si

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Ayar ve profil değişikliklerinin Runtime üzerinden doğrulanarak kaydedilmesini ve çalışan sisteme güvenli şekilde uygulanmasını sağlamak.

Bu API, sonraki task'lardaki WPF ekranlarının (016 Ayarlar, 017 Profil Yönetimi) kullanacağı tek yönetim noktası olacaktır.

## Onaylanan Teknik Kararlar

- Değişiklikler **açık "Kaydet"** ile uygulanacaktır:
  1. Değişiklikler arayüzde taslak olarak tutulur.
  2. Kaydet ile taslak tek çağrıyla Runtime'a gönderilir.
  3. Runtime doğrular, diske yazar ve çalışan sisteme uygular.
  4. İptal edilen taslak hiçbir etki bırakmaz.
- Ayar ve profil JSON formatları değişmeyecektir.
- Yeni NuGet paketi eklenmeyecektir.

## Yönetim Yetenekleri

### Ayarlar

- Mevcut ayarların kopyasını (snapshot) okumak.
- Ayarları güncellemek (`UpdateSettingsAsync`).
- Kullanılabilir seri port listesini almak.
- Cihaz bağlantısını elle yeniden başlatmak (`ReconnectAsync`).

### Profiller

- Profil listesini okumak.
- Profil oluşturmak.
- Profil güncellemek (ad, etkinlik, tuş eşleştirmeleri, kombinasyonlar).
- Profil silmek.
- Aktif profili seçmek.

Arayüz dışarıya Runtime'ın iç nesnelerini (transport, Input Engine, `ProfileManager`) açmamalıdır; yalnızca kopya veri modelleri ve sonuç nesneleri kullanılmalıdır.

## Kaydetme Akışı

```text
Taslak (UI)
    ↓
Doğrulama
    ↓ (hata varsa: alan bazlı hata listesi döner, hiçbir şey değişmez)
Atomik dosya yazma
    ↓
Runtime'a uygulama
    ↓
Tipli değişiklik olayı
```

Kurallar:

- Doğrulama hatası olduğunda dosya ve çalışan sistem değişmemelidir.
- Dosya yazma atomik olmalıdır (geçici dosyaya yaz, sonra değiştir); yarım yazılmış dosya oluşmamalıdır.
- Diske yazma başarısız olursa çalışan sistem değişmemeli ve hata döndürülmelidir.
- Eşzamanlı yönetim çağrıları sıralanmalıdır (`SemaphoreSlim`).

## Değişikliklerin Uygulanması

- Bağlantı ayarları değiştiyse mevcut bağlantı kontrollü şekilde kapatılır ve yeni ayarlarla transport yeniden oluşturulur.
- Girdi ayarları (FN tuşu, uzun basma süresi, FN profil eşleştirmesi) değiştiyse Input Engine yeni ayarlarla yenilenir.
- Profil değişiklikleri çalışan profil yöneticisine uygulanır.
- Çalışan bir eylem zinciri ayar veya profil kaydı nedeniyle yarıda kesilmemelidir.
- Değişmeyen bileşenler yeniden oluşturulmamalıdır.

## Doğrulama Kuralları

Ayarlar:

- Wi-Fi seçiliyse Host boş olamaz, Port 1–65535 aralığında olmalıdır.
- Serial seçiliyse port adı boş olamaz, baud rate sıfırdan büyük olmalıdır.
- Uzun basma süresi pozitif olmalıdır.
- FN profil eşleştirmesindeki profil kimlikleri var olan profilleri göstermelidir.

Profiller:

- Profil adı boş olamaz.
- Profil kimliği benzersiz olmalıdır.
- Tuş numarası negatif olamaz.
- Kombinasyon en az 2 farklı tuş içermelidir.
- Eylem alanları geçerli olmalıdır (ör. boş URL veya boş program yolu kabul edilmez, bekleme süresi negatif olamaz).
- Son profil silinemez.
- Aktif profil silinirse başka bir etkin profil aktif yapılır.

Doğrulama sonucu, arayüzün ilgili alanın yanında gösterebileceği şekilde alan bazlı hata listesi döndürmelidir.

## Aktif Profil Kalıcılığı

- Aktif profil değişimi (arayüzden veya FN + tuş ile) `profiles.json` içindeki `ActiveProfileId` alanına kaydedilmelidir.
- Uygulama yeniden başladığında son aktif profil seçili gelmelidir.

## Windows Başlangıcı

- `RunAtStartup` ayarının kayıt defteri işlemi WPF katmanında (`WindowsStartup`) kalmalıdır.
- Runtime ayarlar değiştiğinde tipli olay yayınlar; WPF bu olayda `WindowsStartup.Apply` çağırır.
- Runtime Windows'a özgü API kullanmamalıdır.

## Tipli Olaylar

- Ayarlar değişti.
- Profiller değişti.
- Aktif profil değişti.

Olaylarda hassas bağlantı bilgisi (IP, port, COM port) yayınlanmamalıdır; arayüz gerektiğinde ayar kopyasını ayrıca okur.

## Geliştirme Adımları

### 1. Veri Modelleri ve Sonuç Tipleri

- Yönetim API'sinin kullanacağı kopya veri modellerini belirle (mevcut `AppSettings` ve `ProfilesData` modelleri tercih edilir).
- Doğrulama sonucu ve alan hatası tiplerini oluştur.

### 2. Doğrulama

- Ayar ve profil doğrulayıcılarını yaz.

### 3. Atomik Saklama

- `SettingsStore` ve `ProfileStore` yazma işlemlerini atomik hale getir; dosya formatı değişmeden.

### 4. Runtime Uygulama Mantığı

- Bağlantı yeniden kurma, Input Engine yenileme ve profil güncelleme işlemlerini uygula.
- `ProfileManager`'a profil kaldırma/değiştirme desteği ekle.
- Aktif profil kalıcılığını uygula.

### 5. WPF Bağlantısı

- `SettingsChanged` olayında `WindowsStartup.Apply` çağrısını bağla.

### 6. Teknik Doğrulama

- Doğrulama kurallarını, atomik yazmayı ve uygulama davranışını Task 013 test projelerine eklenen testlerle doğrula.

## Kapsam Dışı

- Ayarlar ve profil yönetimi arayüzleri (016, 017).
- Profil içe/dışa aktarma.
- Ayar veya profil JSON formatının değiştirilmesi.
- Geri alma (undo) geçmişi.
- Plugin ayarları.

## Riskler ve Koruma Kuralları

- Bağlantı yeniden kurulurken eski transport ve zamanlayıcılar serbest bırakılmadan yenisi oluşturulmamalıdır.
- Yarım kalan bir kayıt, çalışan sistemi tutarsız bırakmamalıdır.
- Mevcut `settings.json` ve `profiles.json` dosyaları sorunsuz okunmaya devam etmelidir.
- Yönetim API'si Runtime'ın iç bileşenlerini dışarı sızdırmamalıdır.
- Hassas bağlantı bilgileri loglanmamalıdır.

## Kabul Kriterleri

- [ ] Ayarlar okunabiliyor, doğrulanıp kaydedilebiliyor ve çalışan sisteme uygulanıyor.
- [ ] Profiller oluşturulabiliyor, güncellenebiliyor, silinebiliyor ve aktif yapılabiliyor.
- [ ] Geçersiz değişiklikler alan bazlı hatalarla reddediliyor; dosya ve sistem değişmiyor.
- [ ] Dosya yazma atomik.
- [ ] Bağlantı ayarı değişince bağlantı yeni ayarlarla yeniden kuruluyor.
- [ ] Girdi ayarı değişince Input Engine yenileniyor.
- [ ] Çalışan eylem zinciri kayıt sırasında kesilmiyor.
- [ ] Aktif profil değişimi kalıcı.
- [ ] Seri port listesi ve elle yeniden bağlanma API üzerinden kullanılabiliyor.
- [ ] Değişiklikler tipli olaylarla bildiriliyor.
- [ ] Windows başlangıç ayarı kaydedildiğinde WPF tarafından uygulanıyor.
- [ ] Ayar ve profil JSON formatları değişmedi.
- [ ] Yeni NuGet paketi eklenmedi.
- [ ] Yeni davranışlar için otomatik testler eklendi ve geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
```

## Tamamlanma Sınırı

Bu task, yönetim API'si Runtime üzerinde çalıştığında, otomatik testler geçtiğinde ve mevcut kullanıcı verileriyle uygulama sorunsuz açıldığında tamamlanmış sayılacaktır.

Arayüz olmadığı için kullanıcı testi, ayar/profil dosyalarının korunması ve uygulamanın açılış/kapanış davranışıyla sınırlıdır.
