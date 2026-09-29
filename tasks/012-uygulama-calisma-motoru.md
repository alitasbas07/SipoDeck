# Task 012 — Uygulama Çalışma Motoru

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

SipoDeck'in ayar, profil, cihaz bağlantısı, girdi işleme ve eylem çalıştırma parçalarını WPF uygulamasından bağımsız tek bir çalışma motoru altında toplamak.

Çalışma motoru; ileride WPF arayüzü, plugin sistemi ve farklı istemciler tarafından kullanılabilecek uygulama katmanı olacaktır.

## Onaylanan Teknik Kararlar

- Çalışma motoru ayrı bir `SipoDeck.Runtime` projesinde bulunacaktır.
- Cihaz bağlı olmasa veya bağlantı ayarları eksik olsa bile Runtime çalışmaya devam edecektir.
- Eylem sözleşmesi async ve iptal edilebilir olacaktır.
- Runtime durumları ve hataları yeni paket eklenmeden tipli olaylarla dışarı bildirilecektir.
- Yeni NuGet paketi eklenmeyecektir.

## Hedef Mimari

```text
SipoDeck (WPF / Tray)
        ↓
SipoDeck.Runtime
        ↓
SipoDeck.Core
        ↓
Transport → Device Event → Input Engine → Active Profile → Action Queue
```

Katman sorumlulukları:

- `SipoDeck.Core`: Protokol, transport, cihaz, profil, input ve eylem sözleşmeleri.
- `SipoDeck.Runtime`: Uygulama bileşenlerini oluşturma, başlatma, durdurma ve koordine etme.
- `SipoDeck`: WPF pencere ve sistem tepsisi yaşam döngüsü.

WPF katmanı transport, Input Engine veya yeniden bağlanma bileşenlerini doğrudan oluşturmamalıdır.

## Runtime Sorumlulukları

Runtime aşağıdaki işlemleri yönetmelidir:

1. Ayarları yüklemek.
2. Profilleri yüklemek ve aktif profili seçmek.
3. Ayarlara göre Wi-Fi veya Serial transport oluşturmak.
4. `DeviceConnection`, `ReconnectService` ve `InputEngine` yaşam döngüsünü yönetmek.
5. Cihaz olaylarını Input Engine'e aktarmak.
6. Eylemleri sıralı ve kontrol edilebilir şekilde çalıştırmak.
7. Başlatma, çalışma ve kapanma durumlarını dışarı bildirmek.
8. Cihaz, bağlantı, reddedilen mesaj ve eylem hatalarını dışarı bildirmek.
9. Kapanışta aktif işlemleri iptal edip kaynakları güvenli şekilde serbest bırakmak.

## Runtime Yaşam Döngüsü

Runtime için aşağıdaki temel durumlar kullanılmalıdır:

```text
Stopped
Starting
Running
Stopping
Faulted
```

Kurallar:

- Yeni Runtime başlangıçta `Stopped` durumunda olmalıdır.
- `StartAsync` ve `StopAsync` tekrar çağrıldığında sistemi bozmamalıdır.
- Bağlantı ayarı eksikse Runtime `Running` durumuna geçmeli, cihaz bağlantısı `Disconnected` kalmalıdır.
- Cihaz bağlantısının kesilmesi Runtime'ı durdurmamalıdır.
- Beklenmeyen ve devam etmeyi engelleyen başlangıç hatası `Faulted` durumuyla bildirilmelidir.
- Runtime durumu ile cihaz bağlantı durumu birbirinden ayrı tutulmalıdır.

## Runtime Sözleşmesi

Runtime dışarıya en az aşağıdaki yetenekleri sağlamalıdır:

```text
State
StartAsync(CancellationToken)
StopAsync(CancellationToken)
```

Tipli olaylar en az şu alanları kapsamalıdır:

- Runtime durumu değişti.
- Cihaz bağlantı durumu değişti.
- Cihaz tanındı.
- Cihaz mesajı reddedildi.
- Eylem çalıştırılamadı.
- Runtime kritik hata verdi.

Olaylarda hassas bağlantı bilgileri veya kullanıcı verileri yayınlanmamalıdır.

## Async Eylem Sistemi

Mevcut:

```csharp
void Execute();
```

Hedef:

```csharp
Task ExecuteAsync(CancellationToken cancellationToken = default);
```

Kurallar:

- Mevcut eylemler async sözleşmeye uyarlanmalıdır.
- `WaitAction`, bloklayan bekleme yerine iptal edilebilir async bekleme kullanmalıdır.
- `ActionChain` eylemleri tanımlandıkları sırada çalıştırmalıdır.
- Zincirde bir eylem başarısız olduğunda sonraki eylemler çalışmaya devam etmelidir.
- Zincir tamamlandığında oluşan hatalar sessizce kaybolmamalı, Runtime tarafından bildirilmelidir.
- İptal isteği geldiğinde zincir durmalı ve kalan eylemler çalıştırılmamalıdır.

## Eylem Kuyruğu

Runtime tek okuyuculu sıralı bir eylem kuyruğu yönetmelidir.

Kurallar:

- Input Engine eylemi doğrudan çalıştırmak yerine kuyruğa aktarmalıdır.
- Eylemler kuyruğa giriş sırasına göre çalışmalıdır.
- Aynı anda birden fazla eylem zinciri çalıştırılmamalıdır.
- Bir eylem hatası kuyruğu veya Runtime'ı durdurmamalıdır.
- Runtime durdurulduğunda çalışan eylem iptal edilmeli ve bekleyen eylemler çalıştırılmamalıdır.
- Kuyruk için .NET'in mevcut altyapısı kullanılmalı, harici paket eklenmemelidir.

## WPF Entegrasyonu

`App.xaml.cs` yalnızca uygulama yaşam döngüsünü yönetmelidir:

1. Runtime oluşturulur.
2. Runtime olaylarına abone olunur.
3. Runtime başlatılır.
4. Ana pencere ve sistem tepsisi yönetilir.
5. Tam kapanışta Runtime durdurulur.

Aşağıdaki sorumluluklar `App.xaml.cs` içerisinden Runtime'a taşınmalıdır:

- Ayar ve profil yükleme koordinasyonu.
- Transport oluşturma.
- Input Engine oluşturma.
- DeviceConnection oluşturma.
- ReconnectService oluşturma.
- Cihaz olaylarını Input Engine'e bağlama.
- Cihaz iletişimini durdurma ve kaynakları serbest bırakma.

## Geliştirme Adımları

### 1. Runtime Projesi ve Sözleşmeleri

- `src/SipoDeck.Runtime` projesini oluştur.
- `SipoDeck.Core` referansını ekle.
- Runtime durum modelini oluştur.
- Runtime sözleşmesini ve tipli olay modellerini oluştur.
- Projeyi solution içerisine ekle.

### 2. Async Eylem Geçişi

- `IAction` sözleşmesini async ve iptal edilebilir hale getir.
- Mevcut eylemleri yeni sözleşmeye uyarla.
- `ActionChain` sıralama, hata devamlılığı ve iptal davranışını koruyacak şekilde güncelle.
- Test amaçlı eylemleri ve mevcut örnek projeleri yeni sözleşmeye uyarla.

### 3. Eylem Kuyruğu

- Core tarafında Input Engine'in kullanacağı dar bir eylem aktarım sözleşmesi oluştur.
- Runtime tarafında sıralı eylem kuyruğunu uygula.
- Eylem hatalarını tipli Runtime olayına dönüştür.
- Runtime kapanışında kuyruk ve aktif eylem iptalini uygula.

### 4. Runtime Koordinasyonu

- Ayar ve profil yükleme işlemlerini Runtime'a taşı.
- Transport üretimini Runtime sorumluluğuna taşı.
- DeviceConnection, InputEngine ve ReconnectService bileşenlerini Runtime altında birleştir.
- Cihazsız çalışma davranışını uygula.
- Tekrarlı başlatma ve durdurma çağrılarını güvenli hale getir.

### 5. WPF Bağlantısı

- WPF projesine `SipoDeck.Runtime` referansı ekle.
- `App.xaml.cs` içindeki koordinasyon kodunu Runtime kullanımına indirgeme.
- Mevcut tray ve pencere davranışını koru.
- Uygulama kapanışında Runtime'ın kontrollü şekilde durmasını sağla.

### 6. Teknik Doğrulama

- Solution build kontrolünü çalıştır.
- Mevcut EventFlowDemo akışını yeni async sözleşmeyle doğrula.
- Bağlantı ayarı olmadan Runtime'ın `Running` durumuna geçtiğini doğrula.
- Runtime başlatma/durdurma ve eylem sırası için donanımsız bir doğrulama senaryosu çalıştır.
- Hatalı bir test eyleminin sonraki eylemi engellemediğini ve hata olayının üretildiğini doğrula.
- Kapanış sırasında çalışan bekleme eyleminin iptal edildiğini doğrula.

## Kapsam Dışı

- WPF ana ekran tasarımı.
- Profil veya tuş düzenleme arayüzü.
- Plugin yükleme ve plugin API sistemi.
- Mobil uygulama entegrasyonu.
- ESP32 protokol değişikliği.
- Ayar veya profil JSON formatının değiştirilmesi.
- Gerçek ESP32 ve USB Serial fiziksel testleri.
- Yeni NuGet paketi eklenmesi.
- Otomatik test projesi kurulması; bu çalışma ayrı bir task olarak planlanacaktır.

## Riskler ve Koruma Kuralları

- `IAction` sözleşmesi değişeceği için bütün mevcut eylemler ve örnekler birlikte güncellenmelidir.
- Async geçiş sırasında kontrolsüz `async void` veya izlenmeyen fire-and-forget işlem kullanılmamalıdır.
- Mevcut eylem sırası değiştirilmemelidir.
- Cihaz bağlantı hatası uygulamanın kapanmasına neden olmamalıdır.
- Runtime durdurulmadan transport ve timer kaynakları bırakılmamalıdır.
- Mevcut JSON verileriyle geriye dönük uyumluluk korunmalıdır.
- Core katmanına WPF bağımlılığı eklenmemelidir.
- `WiFiTransport.Name` ve `SerialTransport.Name` IP/port ve COM port bilgisi içerir; Runtime olaylarında transport adı değil yalnızca `ConnectionType` yayınlanmalıdır.
- `DeviceManager` thread-safe değildir ve cihaz olayları transport okuma thread'inden gelir; Runtime cihaz kaydına erişimi tek noktadan ve kilitli yapmalıdır.
- `InputEngine` uzun basma eylemini zamanlayıcı thread'inde tetikler; eylem aktarım sözleşmesi bu yolu da kapsamalı, aktarım kilit dışında yapılmalıdır.
- FN ile aktif profil değişimi şu an kaydedilmemektedir; bu davranış Task 014 kapsamındadır.

## Kabul Kriterleri

- [ ] `SipoDeck.Runtime` ayrı proje olarak oluşturuldu.
- [ ] Runtime yalnızca `SipoDeck.Core` katmanına bağımlı.
- [ ] Runtime başlatılabiliyor ve kontrollü şekilde durdurulabiliyor.
- [ ] Tekrarlı başlatma/durdurma çağrıları sistemi bozmuyor.
- [ ] Bağlantı ayarı veya cihaz olmadığında uygulama çalışmaya devam ediyor.
- [ ] Runtime ve cihaz bağlantı durumları ayrı izlenebiliyor.
- [ ] Ayarlar ve profiller Runtime tarafından yükleniyor.
- [ ] Cihaz olayları Runtime üzerinden Input Engine'e ulaşıyor.
- [ ] `IAction` async ve iptal edilebilir çalışıyor.
- [ ] Eylemler sıralı kuyruk üzerinden çalışıyor.
- [ ] ActionChain sırası korunuyor.
- [ ] Bir eylem hatası sonraki eylemleri ve Runtime'ı durdurmuyor.
- [ ] Eylem hataları tipli olayla dışarı bildiriliyor.
- [ ] Runtime kapanışında aktif eylemler ve bağlantılar kontrollü şekilde durduruluyor.
- [ ] `App.xaml.cs` yalnızca WPF/tray yaşam döngüsü ve Runtime bağlantısını yönetiyor.
- [ ] Mevcut tray ve pencere davranışı korunuyor.
- [ ] Ayar ve profil JSON formatları değişmedi.
- [ ] Yeni NuGet paketi eklenmedi.
- [ ] EventFlowDemo yeni sözleşmeyle çalışıyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet run --project samples\SipoDeck.EventFlowDemo\SipoDeck.EventFlowDemo.csproj --no-build
```

Runtime'a özel donanımsız doğrulama komutu, uygulama sırasında oluşturulacak doğrulama projesinin adına göre bu bölüme eklenecektir. Önerilen proje adı: `samples/SipoDeck.RuntimeDemo`.

## Tamamlanma Sınırı

Bu task yalnızca Runtime motoru mevcut WPF uygulamasına bağlandığında, donanımsız teknik doğrulamalar geçtiğinde ve kullanıcı uygulamanın açılış/kapanış davranışını onayladığında tamamlanmış sayılacaktır.

Fiziksel cihaz testlerinin ertelenmiş olması Task 012'nin tamamlanmasını engellemez; bu testler Task 010 kapsamında açık kalacaktır.
