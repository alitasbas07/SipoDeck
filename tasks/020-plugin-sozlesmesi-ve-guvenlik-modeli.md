# Task 020 — Plugin Sözleşmesi ve Güvenlik Modeli

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

SipoDeck plugin sisteminin sözleşmesini (API), plugin tanım dosyasını (manifest), yetki modelini ve izolasyon sınırlarını belirlemek.

Bu task bir tasarım ve sözleşme task'ıdır. Plugin yükleme ve çalıştırma Task 021 kapsamındadır.

## Onaylanan Teknik Kararlar

- Plugin sözleşmesi ayrı bir proje olarak yayınlanacaktır (önerilen ad: `SipoDeck.Plugin.Abstractions`).
- Plugin'ler Runtime'ın iç bileşenlerine (transport, Input Engine, profil yöneticisi) doğrudan erişemeyecektir.
- Plugin eylemleri Task 019 eylem kataloğuna eklenecek ve Task 012 eylem kuyruğu üzerinden çalışacaktır.
- Bütün plugin kararları bu task'ta dokümante edilecektir: `docs/plugin-mimarisi.md`.

## Karar Bekleyen Konular

Bu task'ın en önemli çıktısı aşağıdaki kararlardır. Her biri task başlangıcında karar formatıyla kullanıcıya sorulacaktır.

### 1. İzolasyon Modeli

Seçenek 1: **Aynı süreçte yükleme (AssemblyLoadContext)**

- Basit; plugin'ler .NET sınıfı olarak doğrudan çalışır, performans yüksektir.
- .NET'te süreç içi güvenlik sandbox'ı yoktur; yetkiler yalnızca beyan ve kullanıcı onayı düzeyinde kalır.
- Hatalı bir plugin (sonsuz döngü, bellek taşması, çökme) uygulamanın tamamını etkileyebilir.

Seçenek 2: **Ayrı süreçte çalıştırma (plugin host + IPC)**

- Her plugin ayrı bir süreçte çalışır; uygulamayla named pipe veya yerel soket üzerinden konuşur.
- Plugin çökmesi uygulamayı etkilemez; süreç sonlandırılabilir.
- Daha karmaşıktır; mesaj sözleşmesi, süreç yönetimi ve gecikme maliyeti vardır.

Seçenek 3: **Script tabanlı plugin**

- Plugin'ler gömülü bir betik motoruyla çalışır; erişim motor seviyesinde sınırlanabilir.
- Yeni dependency gerektirir; .NET kütüphaneleri doğrudan kullanılamaz.

### 2. Yetki Modeli

Önerilen yetki listesi (kararla kesinleşecek):

- `process.run` — program çalıştırma.
- `network` — ağ erişimi.
- `filesystem.read` / `filesystem.write` — plugin veri klasörü dışındaki dosyalara erişim.
- `notifications` — bildirim gösterme.
- `input.send` — klavye/metin gönderme.
- `device.events` — cihaz tuş olaylarını dinleme.

Karar verilecek konular:

- Yetkiler plugin ilk etkinleştirilirken kullanıcıya gösterilip onay alınacak mı?
- Yeni sürüm yeni yetki isterse tekrar onay istenecek mi?
- Seçilen izolasyon modelinde yetkiler teknik olarak mı zorlanacak, beyan olarak mı kalacak?

### 3. Güven ve Dağıtım

- Plugin'ler yalnızca kullanıcının plugin klasörüne elle koyduğu dosyalardan mı yüklenecek?
- İmzasız plugin'ler için uyarı gösterilecek mi?
- Plugin klasörünün konumu (önerilen: `%LOCALAPPDATA%\SipoDeck\plugins\`).

## Plugin Sözleşmesi (Taslak)

Karara göre kesinleşecek temel yapı:

- Plugin yaşam döngüsü: başlat, durdur (iptal edilebilir, zaman sınırlı).
- Eylem sağlayıcı: plugin'in kataloğa eklediği eylem tanımları (Task 019 katalog modeliyle uyumlu) ve bu eylemlerin çalıştırılması.
- Plugin bağlamı: plugin'e verilen sınırlı servisler (günlük kaydı, kendi veri klasörü, yetkiye bağlı servisler).
- Plugin ayarları: plugin'in kendi ayarlarını kendi veri klasöründe saklaması.
- API sürümü: sözleşmenin sürüm numarası ve uyumluluk kuralı.

## Manifest (Taslak)

Her plugin bir tanım dosyası içermelidir (önerilen: `plugin.json`):

```json
{
  "id": "ornek.plugin",
  "name": "Örnek Plugin",
  "version": "1.0.0",
  "author": "YOUR_NAME",
  "description": "Kısa açıklama",
  "apiVersion": 1,
  "entry": "Ornek.Plugin.dll",
  "permissions": ["notifications"]
}
```

Kurallar:

- `id` benzersiz ve değişmez olmalıdır.
- `apiVersion` uygulamanın desteklediği sürümle uyumlu değilse plugin yüklenmemelidir.
- Manifestte beyan edilmeyen yetki kullanılamamalıdır.

## İzolasyon Sınırları

Seçilen modelden bağımsız olarak:

- Plugin hatası Runtime'ı veya diğer plugin'leri durdurmamalıdır.
- Plugin başlatma/durdurma ve eylem çalıştırma zaman sınırına tabi olmalıdır.
- Plugin'ler kullanıcı ayarlarına, profillere ve bağlantı bilgilerine doğrudan erişememelidir.
- Plugin günlük kayıtları plugin kimliğiyle ayrıştırılmalıdır.

## Geliştirme Adımları

### 1. Kararlar

- İzolasyon, yetki ve güven kararlarını kullanıcıya sor.

### 2. Mimari Dokümanı

- `docs/plugin-mimarisi.md` dosyasında kararları, sözleşmeyi, manifesti, yetkileri ve sınırları yaz.

### 3. Sözleşme Projesi

- `SipoDeck.Plugin.Abstractions` projesini oluştur.
- Yaşam döngüsü, eylem sağlayıcı, plugin bağlamı ve manifest modellerini tanımla.
- Projeyi solution'a ekle.

### 4. Doğrulama

- Manifest çözümleme ve doğrulama kurallarını uygula ve test et (yükleyici olmadan).

## Kapsam Dışı

- Plugin keşfetme, yükleme ve çalıştırma (021).
- Plugin yönetim ekranı (022).
- Örnek plugin ve geliştirici dokümanı (023).
- Plugin mağazası veya çevrimiçi kurulum.
- Plugin otomatik güncelleme.

## Riskler ve Koruma Kuralları

- Süreç içi modelde yetkilerin gerçek bir güvenlik sınırı olmadığı kullanıcıya ve dokümana açıkça yazılmalıdır.
- Sözleşme yayınlandıktan sonra kırıcı değişiklik yalnızca `apiVersion` artırılarak yapılmalıdır.
- Sözleşme projesi Runtime veya WPF projelerine bağımlı olmamalıdır.
- Sözleşme Task 019 katalog modeliyle uyumlu olmalı; aynı yapı iki kez tanımlanmamalıdır.

## Kabul Kriterleri

- [ ] İzolasyon modeli kullanıcı tarafından seçildi.
- [ ] Yetki modeli ve onay akışı kullanıcı tarafından belirlendi.
- [ ] Güven ve dağıtım kararları verildi.
- [ ] `docs/plugin-mimarisi.md` yazıldı.
- [ ] `SipoDeck.Plugin.Abstractions` projesi oluşturuldu ve yalnızca gerekli bağımlılıklara sahip.
- [ ] Manifest modeli ve doğrulama kuralları uygulandı ve test edildi.
- [ ] Plugin eylemlerinin katalog ve eylem kuyruğuyla ilişkisi tanımlandı.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
```

## Tamamlanma Sınırı

Bu task, plugin kararları kullanıcı tarafından verilip dokümante edildiğinde, sözleşme projesi derlendiğinde ve manifest doğrulama testleri geçtiğinde tamamlanmış sayılacaktır.
