# Task 013 — Core ve Runtime Otomatik Testleri

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

`SipoDeck.Core` ve `SipoDeck.Runtime` katmanlarının temel davranışlarını otomatik testlerle güvence altına almak.

Sonraki task'larda (yönetim API'si ve WPF arayüzü) yapılacak değişikliklerin mevcut motoru bozmadığı her build'de doğrulanabilmelidir.

## Onaylanan Teknik Kararlar

- Test framework'ü olarak **xUnit** kullanılacaktır.
- Test paketleri yalnızca test projelerine eklenecektir: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`.
- Üretim projelerine (`SipoDeck.Core`, `SipoDeck.Runtime`, `SipoDeck`) yeni paket eklenmeyecektir.
- Zamana bağlı testler için test projesinde elle yazılmış küçük bir sahte `TimeProvider` kullanılacaktır; ek paket eklenmeyecektir.
- Testler gerçek donanım, gerçek `%LOCALAPPDATA%` klasörü veya dış ağ kullanmayacaktır.

## Proje Yapısı

```text
tests/
 ├── SipoDeck.Core.Tests      (net10.0, xUnit)
 └── SipoDeck.Runtime.Tests   (net10.0, xUnit)
```

- Test projeleri solution içinde `tests` klasörü altında yer almalıdır.
- `SipoDeck.Core.Tests` yalnızca `SipoDeck.Core` projesine referans vermelidir.
- `SipoDeck.Runtime.Tests`, `SipoDeck.Runtime` ve gerekirse `SipoDeck.Core` projesine referans vermelidir.
- Test projeleri WPF projesine referans vermemelidir.

## Test Kapsamı

### Protokol

- `ProtocolSerializer.TryDeserialize` geçerli `button` ve `hello` mesajlarını çözümlüyor.
- Geçersiz JSON, eksik veya bilinmeyen `type`, desteklenmeyen sürüm, eksik `button`/`state` alanları ve geçersiz `state` reddediliyor.
- `LineFramer` parçalı veriyi, CRLF satır sonlarını, boş satırları ve aşırı uzun satırları doğru işliyor.

### Cihaz Bağlantısı

Sahte bir `ITransport` ile:

- Bağlantı kurulduğunda `hello` isteği gönderiliyor.
- `hello` mesajı cihazı `DeviceManager`'a kaydediyor.
- Tuş mesajları `KeyEvent`'e dönüştürülüyor.
- Geçersiz mesajlar `MessageRejected` ile bildiriliyor ve istisna fırlatmıyor.
- Bağlantı kesildiğinde durum `Disconnected` oluyor.

### Input Engine

- Tekli tuş eylemi tuş bırakıldığında çalışıyor.
- Kombinasyon tekli tuştan önce değerlendiriliyor.
- Uzun basma eşik süresinde tetikleniyor ve bırakınca normal eylem çalışmıyor.
- FN + tuş ile profil değişiyor.
- Tanımsız tuş eylem üretmiyor.

### Eylemler

- `ActionChain` eylemleri tanımlandıkları sırada çalıştırıyor.
- Bir eylem hatası sonraki eylemleri durdurmuyor ve hata raporlanıyor.
- İptal isteğinde zincir duruyor, kalan eylemler çalışmıyor.
- `WaitAction` iptal edilebiliyor.

### Veri Saklama

- `SettingsStore` ve `ProfileStore` geçici klasörde kaydetme/yükleme yapıyor.
- Bozuk veya eksik dosyada varsayılan değerler dönüyor.
- Sabit örnek JSON dosyalarıyla geriye dönük uyumluluk doğrulanıyor (`ActionConfig` türleri dahil).

### Runtime

- Yeni Runtime `Stopped` durumunda başlıyor.
- Tekrarlı `StartAsync` / `StopAsync` çağrıları sistemi bozmuyor.
- Bağlantı ayarı yokken Runtime `Running` durumuna geçiyor, cihaz bağlantısı `Disconnected` kalıyor.
- Eylemler kuyruğa giriş sırasına göre çalışıyor.
- Hatalı eylem tipli hata olayı üretiyor ve kuyruğu durdurmuyor.
- Runtime durdurulunca çalışan eylem iptal ediliyor, bekleyen eylemler çalışmıyor.
- Runtime olaylarında IP, port veya COM port bilgisi bulunmuyor.

### Wi-Fi Entegrasyonu

- `WiFiTransport` yerel `HttpListener` WebSocket sunucusuna bağlanıyor.
- Sunucu bağlantıyı kapattığında ve bağlantı aniden koptuğunda durum `Disconnected` oluyor.
- `ReconnectService` bağlantıyı yeniden kuruyor.
- Ulaşılamayan adreste bağlantı denemesi zaman aşımıyla sonlanıyor.

## Geliştirme Adımları

### 1. Test Projeleri

- `tests/SipoDeck.Core.Tests` ve `tests/SipoDeck.Runtime.Tests` projelerini oluştur.
- Projeleri solution'a `tests` klasörü altında ekle.
- `dotnet test SipoDeck.sln` komutunun çalıştığını doğrula.

### 2. Test Yardımcıları

- Sahte `ITransport` oluştur (veri gönderme, durum değiştirme, gönderilen veriyi kaydetme).
- Sahte `TimeProvider` oluştur (zamanı elle ilerletme).
- Kaydeden ve hata fırlatan test eylemlerini oluştur.
- Geçici klasör yardımcısını oluştur (test sonunda silinir).

### 3. Core Testleri

- Protokol, cihaz bağlantısı, Input Engine, eylem ve veri saklama testlerini yaz.

### 4. Runtime Testleri

- Yaşam döngüsü, cihazsız çalışma, eylem kuyruğu, hata olayları ve iptal testlerini yaz.

### 5. Entegrasyon Testi

- Yerel WebSocket sunucusuyla `WiFiTransport` ve `ReconnectService` testlerini yaz.
- Sabit port yerine boş bir port seçilmeli; testler paralel çalışırken çakışmamalıdır.

## Kapsam Dışı

- Fiziksel ESP32 ve USB Serial testleri.
- WPF arayüz testleri.
- CI (GitHub Actions vb.) kurulumu; ayrı task olarak planlanabilir.
- Kod kapsama (coverage) raporu.
- Üretim kodunda davranış değişikliği.

## Riskler ve Koruma Kuralları

- Testler için üretim koduna test amaçlı bağımlılık veya public API eklenmemelidir.
- İç (internal) üyelere erişim gerekirse yalnızca `InternalsVisibleTo` kullanılabilir; bu durumda kullanıcıya bildirilmelidir.
- Test yazarken üretim kodunda hata bulunursa hata kendiliğinden düzeltilmemeli, kullanıcıya bildirilmelidir.
- Testler `Task.Delay` ile uzun beklemelere dayanmamalıdır; zamana bağlı testler sahte `TimeProvider` kullanmalıdır.
- Ağ testleri yalnızca `localhost` kullanmalıdır.
- Testler kararlı (deterministik) olmalı, tekrar çalıştırmada farklı sonuç vermemelidir.

## Kabul Kriterleri

- [ ] `SipoDeck.Core.Tests` ve `SipoDeck.Runtime.Tests` projeleri oluşturuldu.
- [ ] Test paketleri yalnızca test projelerine eklendi.
- [ ] Protokol ve satır ayırma testleri yazıldı.
- [ ] `DeviceConnection` testleri yazıldı.
- [ ] Input Engine testleri yazıldı.
- [ ] Eylem ve ActionChain testleri yazıldı.
- [ ] Veri saklama ve geriye dönük uyumluluk testleri yazıldı.
- [ ] Runtime yaşam döngüsü ve eylem kuyruğu testleri yazıldı.
- [ ] Wi-Fi entegrasyon testi yazıldı.
- [ ] Testler donanımsız ve gerçek kullanıcı verisine dokunmadan çalışıyor.
- [ ] `dotnet test SipoDeck.sln` hatasız tamamlanıyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
```

## Tamamlanma Sınırı

Bu task, belirtilen test kapsamı yazıldığında ve `dotnet test` bütün testleri hatasız geçtiğinde tamamlanmış sayılacaktır.

Test sırasında bulunan üretim kodu hataları ayrı olarak raporlanmalı ve düzeltme kararı kullanıcıya bırakılmalıdır.
