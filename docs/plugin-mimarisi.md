# SipoDeck Plugin Mimarisi

Bu doküman Task 020 kararlarını, plugin sözleşmesini, manifesti, yetki modelini ve izolasyon sınırlarını tanımlar.
Plugin keşfi/yüklemesi Task 021, yönetim ekranı Task 022, örnek plugin ve geliştirici rehberi Task 023 kapsamındadır.

## 1. Kararlar

| Konu | Karar |
|---|---|
| İzolasyon | **Aynı süreçte yükleme** (`AssemblyLoadContext`, plugin başına ayrı bağlam). |
| Yetkiler | Manifestte **beyan** edilir; **ilk etkinleştirmede kullanıcıya gösterilip onay** alınır; yeni sürüm **yeni yetki** isterse **tekrar onay** istenir. |
| Zorlama | Yetkiye bağlı **servisler** (bildirim, girdi gönderme, program çalıştırma, cihaz olayları) yalnızca onaylı yetki varsa plugin'e verilir. `network` ve `filesystem.*` teknik olarak zorlanamaz (aşağıya bakın). |
| Dağıtım | Yalnızca kullanıcının plugin klasörüne **elle koyduğu** dosyalar. Çevrimiçi kurulum/mağaza yoktur. |
| Güven | İmza doğrulaması yoktur; etkinleştirmede **"doğrulanmamış kod"** uyarısı gösterilir. |
| Konum | `%LOCALAPPDATA%\SipoDeck\plugins\<id>\` (içinde `plugin.json` ve giriş DLL'i). |

## 2. Güvenlik modeli ve sınırları (önemli)

.NET'te süreç içi güvenlik sandbox'ı yoktur. Bu nedenle:

- Plugin, uygulamayla **aynı yetkilerle** çalışan koddur. Kötü niyetli bir plugin dosya sistemine, ağa ve
  sürece yetki sisteminden bağımsız olarak erişebilir.
- Yetkiler bir **güvenlik sınırı değil, şeffaflık ve onay mekanizmasıdır**: kullanıcı plugin'in ne yapmayı amaçladığını görür;
  uygulama yalnızca onaylı yetkilere bağlı **kendi servislerini** verir.
- Bu durum kullanıcıya etkinleştirme onayında açıkça belirtilir: *"Plugin'ler bilgisayarınızda tam yetkiyle çalışan koddur. Yalnızca güvendiğiniz kaynaklardan yükleyin."*
- Hatalı plugin'in uygulamayı çökertme riski (sonsuz döngü, bellek taşması, işlenmeyen istisna) tam olarak ortadan kalkmaz;
  aşağıdaki koruma önlemleriyle azaltılır.

### Koruma önlemleri (Task 021'de uygulanacak)

- Plugin'den gelen her çağrı (başlat, durdur, eylem çalıştır, olay işleyici) `try/catch` içinde yapılır; hata plugin'e atfedilir, Runtime ve diğer plugin'ler etkilenmez.
- Başlatma, durdurma ve eylem çalıştırma **zaman sınırına** tabidir; süre aşılırsa iptal istenir ve plugin hatalı işaretlenir.
- Çok kez hata veren plugin otomatik devre dışı bırakılabilir.
- Plugin eylemleri Runtime'ın **eylem kuyruğunda** çalışır; kullanıcı ayarlarına, profillere, bağlantı bilgilerine, transport'a ve Input Engine'e **doğrudan erişemez** (bağlam yalnızca aşağıdaki servisleri sunar).
- Günlük kayıtları plugin kimliğiyle ayrıştırılır.
- İleride daha güçlü izolasyon gerekirse ayrı süreç modeli ayrı bir task ve `apiVersion` artışıyla değerlendirilir.

## 3. Yetkiler

| Yetki | Anlamı | Zorlama |
|---|---|---|
| `process.run` | Program çalıştırma (`IProcessService`) | Servis yalnızca onayla verilir |
| `network` | Ağ erişimi | Yalnızca beyan |
| `filesystem.read` | Plugin veri klasörü dışını okuma | Yalnızca beyan |
| `filesystem.write` | Plugin veri klasörü dışına yazma | Yalnızca beyan |
| `notifications` | Bildirim gösterme (`INotificationService`) | Servis yalnızca onayla verilir |
| `input.send` | Klavye/metin gönderme (`IInputService`) | Servis yalnızca onayla verilir |
| `device.events` | Ham tuş olaylarını dinleme (`IDeviceEventService`) | Servis yalnızca onayla verilir |

Kurallar:

- Manifestte beyan edilmeyen yetkiye bağlı servis `IPluginContext.GetService<T>()` ile alınamaz (`null` döner).
- Bilinmeyen veya tekrarlı yetki beyanı manifesti geçersiz kılar.
- Onay, plugin kimliği + onaylanan yetki kümesi olarak uygulamanın kendi ayarlarında (Task 021) saklanır.
  Güncellemede `PluginManifestParser.RequiresReapproval` true dönerse plugin yeni onay alınana kadar etkinleştirilmez.
  Yetkinin **azalması** tekrar onay gerektirmez.

## 4. Manifest (`plugin.json`)

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

| Alan | Kural |
|---|---|
| `id` | Zorunlu, benzersiz ve değişmez. Küçük harf, rakam, `-`, `_`; `.` ile ayrılmış bölümler; en fazla 64 karakter. |
| `name` | Zorunlu. |
| `version` | Zorunlu, anlamsal sürüm (`1.2.3`, `1.0.0-beta.1`). |
| `author`, `description` | İsteğe bağlı. |
| `apiVersion` | Uygulamanın desteklediği sürümle (`PluginApi.Current`) eşit olmalı; değilse plugin yüklenmez. |
| `entry` | Zorunlu, `.dll`, plugin klasörüne göreli; mutlak yol, sürücü harfi ve `..` kabul edilmez. |
| `permissions` | İsteğe bağlı; yalnızca bilinen yetkiler, tekrar yok. |

Doğrulama `PluginManifestParser` (SipoDeck.Plugin.Abstractions) içindedir ve yükleyiciden bağımsızdır.

## 5. Sözleşme (`SipoDeck.Plugin.Abstractions`)

Ayrı, bağımlılıksız bir projedir; Runtime, Core veya WPF'e bağımlı değildir. Plugin geliştiricisi yalnızca bunu referans alır.

- **`IPlugin`**: `StartAsync(IPluginContext, ct)`, `StopAsync(ct)`, `GetActions()`, `ExecuteActionAsync(PluginActionRequest, ct)`.
  Plugin DLL'i `IPlugin`'i uygulayan tam olarak bir somut sınıf (genel, parametresiz kurucu) içermelidir.
- **`IPluginContext`**: `PluginId`, `DataDirectory`, `GrantedPermissions`, `Logger`, `Settings`, `GetService<T>()`.
- **Ayarlar**: `IPluginSettings` anahtar/değer; plugin'in kendi veri klasöründe saklanır. Uygulama ayarlarına erişim yoktur.
- **Yetkiye bağlı servisler**: `INotificationService`, `IInputService`, `IProcessService`, `IDeviceEventService`.
- **Sürümleme**: `PluginApi.Current = 1`. Kırıcı değişiklik yalnızca `apiVersion` artırılarak yapılır; eski sürüm plugin'leri sürüm uyuşmazlığı nedeniyle yüklenmez.

## 6. Eylem kataloğu ve eylem kuyruğuyla ilişki

- Plugin eylemleri `PluginActionDescriptor` ile tanımlanır; parametreler `PluginParameterDescriptor` ile (metin, uzun metin, sayı, seçim, dosya yolu, kısayol).
- Task 021, bu tanımları Task 019 `ActionCatalog` kayıtlarına **dönüştürür**. Katalog yapısı ikinci kez tanımlanmaz: plugin tanımı yalnızca plugin yazarına gösterilen sade bir DTO'dur.
  Kataloğa kimlik `pluginId/actionId` olarak kaydedilir; böylece yerleşik türlerle ve diğer plugin'lerle çakışmaz.
- Kullanıcı profilinde saklanan plugin eylemi (Task 021'de eklenecek) şu biçimdedir: tür `plugin`, `pluginId`, `actionId` ve metin değerli `parameters` sözlüğü.
  Plugin yüklü değilse eylem profilde korunur, çalıştırıldığında hata olarak raporlanır; profil dosyası bozulmaz.
- Çalıştırma: `ActionConfig.ToAction()` → eylem Runtime'ın eylem kuyruğuna gider (Task 012 kuralları: sıralı, hata sonrası devam, iptalde durma) → plugin `ExecuteActionAsync`.
  Parametre doğrulaması katalogdaki ortak doğrulamayla yapılır.

## 7. Yaşam döngüsü

1. Keşif: plugin klasöründe `plugin.json` okunur ve doğrulanır.
2. Onay: yetkiler ve "doğrulanmamış kod" uyarısı gösterilir; onay saklanır.
3. Yükleme: plugin'e özel `AssemblyLoadContext`; giriş DLL'inden `IPlugin` örneği oluşturulur.
4. `StartAsync` (zaman sınırlı) → `GetActions()` ile eylemler kataloğa eklenir.
5. Çalışma: eylemler kuyrukta `ExecuteActionAsync` ile yürütülür.
6. Durdurma: `StopAsync` (zaman sınırlı), katalogdan kaldırma, bağlamı boşaltma.

## 8. Kapsam dışı

Plugin mağazası, çevrimiçi kurulum, otomatik güncelleme, imza doğrulaması, ayrı süreç/IPC modeli.
