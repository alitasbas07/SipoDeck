# Task 015 — WPF Ana Uygulama İskeleti

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

SipoDeck masaüstü uygulamasının temel arayüz kabuğunu oluşturmak: ana pencere, navigasyon, içerik alanı ve durum alanı.

Bu iskelet, sonraki task'lardaki Ayarlar (016) ve Profil Yönetimi (017) ekranlarının yerleşeceği yapı olacaktır.

## Onaylanan Teknik Kararlar

- MVVM yapısı için **CommunityToolkit.Mvvm** paketi kullanılacaktır (yalnızca WPF projesine eklenir).
- Bağımlılık enjeksiyonu için paket eklenmeyecek; ViewModel ve servisler uygulama açılışında elle oluşturulacaktır.
- Arayüz Runtime ile yalnızca Runtime sözleşmesi ve Task 014 yönetim API'si üzerinden konuşacaktır.

## Karar Bekleyen Konular

### Tasarım Dili

Task başlangıcında kullanıcıya karar formatıyla sorulacaktır.

Seçenek 1: **WPF-UI (Fluent) paketi**

- Windows 11 görünümü (Mica, NavigationView, açık/koyu tema) hazır gelir.
- Ek NuGet paketi gerektirir; paketin bileşen yapısına bağımlılık oluşur.

Seçenek 2: **Paketsiz özel stil**

- Proje içi renk, yazı ve kontrol stilleri (ResourceDictionary) tanımlanır.
- Bağımlılık yoktur ve tam kontrol sağlar; daha fazla stil çalışması gerektirir.

Karar verilmeden görsel stil geliştirmesine başlanmamalıdır.

## Arayüz Yapısı

```text
┌──────────────────────────────────────────────┐
│ SipoDeck                                     │
├────────────┬─────────────────────────────────┤
│ Genel Bakış│                                 │
│ Profiller  │        İçerik Alanı             │
│ Ayarlar    │                                 │
├────────────┴─────────────────────────────────┤
│ Runtime: Çalışıyor · Cihaz: Bağlı · Profil: X│
└──────────────────────────────────────────────┘
```

### Navigasyon

- Sol menü: Genel Bakış, Profiller, Ayarlar.
- İçerik alanı `ContentControl` ile gösterilir; ViewModel → View eşlemesi DataTemplate ile yapılır.
- Profiller ve Ayarlar sayfaları bu task'ta yer tutucu olarak oluşturulur.

### Durum Alanı

- Runtime durumu (Çalışıyor, Durduruluyor, Hata vb.).
- Cihaz bağlantı durumu.
- Tanınan cihazın adı ve firmware sürümü.
- Aktif profil adı.

Durum alanında IP, port veya COM port bilgisi gösterilmemelidir.

### Genel Bakış Sayfası

- Bağlantı durumu, cihaz bilgisi ve aktif profilin özeti.
- Runtime kritik hatası varsa anlaşılır bir mesaj.

## Uygulama Yaşam Döngüsü

- Mevcut tray ve pencere davranışı korunmalıdır: pencereyi kapatmak tepsiye küçültür, "Tamamen Kapat" uygulamayı sonlandırır.
- Runtime olayları arka plan thread'lerinden gelir; arayüze `Dispatcher` üzerinden aktarılmalıdır.
- Tray simgesinin ipucu metninde bağlantı durumu gösterilmelidir.
- Pencere gizliyken ViewModel'ler Runtime olaylarını almaya devam edebilir; gereksiz kaynak tüketimi olmamalıdır.

## Geliştirme Adımları

### 1. Paket ve Klasör Yapısı

- CommunityToolkit.Mvvm paketini WPF projesine ekle.
- `ViewModels/`, `Views/` klasörlerini oluştur.

### 2. Tasarım Dili Kararı

- Tasarım dili kararını kullanıcıya sor ve karara göre temel stil kaynaklarını hazırla.

### 3. Kabuk

- `MainWindow`'u kabuk olarak düzenle: navigasyon, içerik alanı, durum alanı.
- `ShellViewModel` ve sayfa ViewModel'lerini oluştur.

### 4. Runtime Bağlantısı

- Runtime olaylarını ViewModel'lere bağla (UI thread'ine aktararak).
- Tray ipucu metnini bağlantı durumuna göre güncelle.

### 5. Sayfalar

- Genel Bakış sayfasını oluştur.
- Profiller ve Ayarlar yer tutucu sayfalarını oluştur.

## Kapsam Dışı

- Ayar formları (016).
- Profil ve tuş düzenleme (017).
- Plugin arayüzü.
- Çoklu dil desteği.
- Arayüz otomatik testleri.

## Riskler ve Koruma Kuralları

- Arayüz transport, Input Engine veya yeniden bağlanma bileşenlerini doğrudan oluşturmamalıdır.
- Runtime olaylarından UI nesnelerine doğrudan (Dispatcher dışında) erişilmemelidir.
- Tasarım dili kararı verilmeden stil geliştirmesi yapılmamalıdır.
- Tray ve pencere davranışı değiştirilmemelidir.
- `App.xaml.cs` yalnızca uygulama yaşam döngüsü ve kompozisyonla sınırlı kalmalıdır.

## Kabul Kriterleri

- [ ] CommunityToolkit.Mvvm paketi yalnızca WPF projesine eklendi.
- [ ] Tasarım dili kararı kullanıcı tarafından verildi ve uygulandı.
- [ ] Ana pencerede navigasyon, içerik alanı ve durum alanı var.
- [ ] Genel Bakış sayfası Runtime ve cihaz durumunu gösteriyor.
- [ ] Profiller ve Ayarlar yer tutucu sayfaları açılıyor.
- [ ] Runtime ve bağlantı durumu değişiklikleri arayüze anlık yansıyor.
- [ ] Tray ipucu bağlantı durumunu gösteriyor.
- [ ] Hassas bağlantı bilgileri arayüzde gösterilmiyor.
- [ ] Mevcut tray ve pencere davranışı korunuyor.
- [ ] Mevcut otomatik testler geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, arayüz iskeleti çalıştığında, otomatik testler geçtiğinde ve kullanıcı pencere, navigasyon, durum alanı ve tray davranışını onayladığında tamamlanmış sayılacaktır.
