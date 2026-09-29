# Task 021 — Plugin Yükleme ve Yaşam Döngüsü

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Task 020'de belirlenen sözleşme ve güvenlik modeline göre plugin'lerin keşfedilmesini, doğrulanmasını, yüklenmesini, başlatılmasını ve durdurulmasını sağlamak.

## Onaylanan Teknik Kararlar

- Plugin yükleme Task 020'de seçilen izolasyon modeline göre yapılacaktır.
- Plugin yönetimi Runtime'ın sorumluluğunda olacaktır; WPF plugin yüklemeyecektir.
- Plugin eylemleri Task 019 kataloğuna eklenecek ve Task 012 eylem kuyruğu üzerinden çalışacaktır.

## Karar Bekleyen Konular

### Plugin Durumlarının Saklanması

Hangi plugin'in etkin olduğu ve verilen yetki onayları kalıcı olarak saklanmalıdır.

Seçenek 1: **`settings.json` içinde yeni bölüm**

- Ayrı dosya gerekmez.
- Ayar modeli büyür; ayar formatına yeni alan eklenir (geriye dönük uyumlu).

Seçenek 2: **Ayrı `plugins.json` dosyası**

- Plugin verisi uygulama ayarlarından ayrı kalır (profillerin ayrı tutulmasıyla tutarlı).
- Yeni bir veri dosyası eklenir.

## Plugin Durumları

```text
Discovered
    ↓
Validated / Invalid
    ↓
Disabled ⇄ Enabled
    ↓
Starting → Running → Stopping → Stopped
    ↓
Faulted
```

Kurallar:

- Geçersiz manifest, uyumsuz `apiVersion` veya eksik dosya plugin'i `Invalid` yapar; uygulama çalışmaya devam eder.
- Yeni keşfedilen plugin varsayılan olarak devre dışıdır; kullanıcı etkinleştirmeden çalışmaz.
- Başlatma sırasında hata veren veya zaman sınırını aşan plugin `Faulted` olur.
- Plugin durumu Runtime durumundan ayrı tutulmalıdır.

## Yaşam Döngüsü Akışı

1. Runtime başlarken plugin klasörü taranır.
2. Her plugin'in manifesti okunur ve doğrulanır.
3. Etkin olarak işaretlenmiş plugin'ler yüklenir ve başlatılır.
4. Plugin'in sağladığı eylemler kataloğa eklenir.
5. Plugin devre dışı bırakıldığında durdurulur, eylemleri katalogdan çıkarılır ve kaynakları serbest bırakılır.
6. Runtime kapanırken bütün plugin'ler zaman sınırıyla durdurulur.

## Eylem Entegrasyonu

- Plugin eylemleri katalogda plugin kimliğiyle ayrıştırılmalıdır.
- Profilde kullanılan bir plugin eylemi, plugin yüklü değil veya devre dışıysa çalışmamalı, tipli hata olayı üretmelidir.
- Plugin devre dışı bırakıldığında profillerdeki ilgili atamalar silinmemelidir; plugin tekrar etkinleştirildiğinde çalışmaya devam etmelidir.
- Plugin eylemleri eylem kuyruğunda iptal ve zaman sınırı kurallarına tabidir.
- Plugin eylemlerinin profil JSON'unda nasıl saklanacağı (plugin kimliği + eylem kimliği + parametreler) mevcut eylemlerin formatını değiştirmeden tanımlanmalıdır.

## Tipli Olaylar

- Plugin keşfedildi / geçersiz.
- Plugin durumu değişti.
- Plugin hatası.

## Geliştirme Adımları

### 1. Saklama Kararı

- Plugin durumlarının saklanma yerini kullanıcıya sor.

### 2. Keşif ve Doğrulama

- Plugin klasörü tarama ve manifest doğrulamayı uygula.

### 3. Yükleme ve Yaşam Döngüsü

- Seçilen izolasyon modeline göre yükleyiciyi uygula.
- Başlatma, durdurma, zaman sınırı ve hata yönetimini uygula.

### 4. Eylem Entegrasyonu

- Plugin eylemlerini katalog ve eylem kuyruğuna bağla.
- Profil JSON'unda plugin eylemi saklama biçimini ekle.

### 5. Runtime Entegrasyonu

- Plugin yöneticisini Runtime başlangıç ve kapanışına bağla.
- Etkinleştirme/devre dışı bırakma yönetim API'sini ekle (Task 022 kullanacak).

### 6. Testler

- Test amaçlı küçük plugin'lerle keşif, geçersiz manifest, başlatma hatası, zaman aşımı, devre dışı bırakma ve kapanış testlerini ekle.

## Kapsam Dışı

- Plugin yönetim ekranı (022).
- Örnek plugin ve geliştirici dokümanı (023).
- Plugin mağazası, çevrimiçi kurulum ve otomatik güncelleme.
- Uygulama çalışırken plugin dosyalarındaki değişiklikleri otomatik algılama.

## Riskler ve Koruma Kuralları

- Hiçbir plugin hatası Runtime'ı veya uygulamayı kapatmamalıdır.
- Kullanıcı onayı olmadan plugin çalıştırılmamalıdır.
- Onaylanmamış yetki kullanılamamalıdır.
- Devre dışı bırakılan plugin'in kaynakları ve (süreç dışı modelde) süreci kalmamalıdır.
- Mevcut profil ve ayar dosyaları plugin olmadan sorunsuz açılmaya devam etmelidir.

## Kabul Kriterleri

- [ ] Plugin durum saklama kararı kullanıcı tarafından verildi.
- [ ] Plugin klasörü taranıyor ve manifestler doğrulanıyor.
- [ ] Geçersiz plugin uygulamayı etkilemeden reddediliyor.
- [ ] Yeni plugin varsayılan olarak devre dışı.
- [ ] Etkin plugin'ler Runtime ile başlıyor ve kapanışta duruyor.
- [ ] Plugin eylemleri katalogda görünüyor ve eylem kuyruğunda çalışıyor.
- [ ] Plugin devre dışı bırakılınca eylemleri çalışmıyor, profil atamaları korunuyor.
- [ ] Başlatma hatası ve zaman aşımı `Faulted` durumuyla bildiriliyor.
- [ ] Plugin durumları kalıcı.
- [ ] Yeni davranışlar için otomatik testler eklendi ve geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
```

## Tamamlanma Sınırı

Bu task, plugin'ler keşfedilip yüklenebildiğinde, yaşam döngüsü testleri geçtiğinde ve plugin hataları uygulamayı etkilemediğinde tamamlanmış sayılacaktır.
