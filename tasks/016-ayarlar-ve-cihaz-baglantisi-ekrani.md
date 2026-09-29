# Task 016 — Ayarlar ve Cihaz Bağlantısı Ekranı

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Kullanıcının bağlantı, uygulama ve girdi ayarlarını arayüzden görüntüleyip değiştirebilmesini sağlamak.

Bu ekranla birlikte `settings.json` dosyasını elle düzenleme ihtiyacı ortadan kalkmalıdır.

## Onaylanan Teknik Kararlar

- Ekran Task 015 iskeletindeki Ayarlar sayfasına yerleşecektir.
- MVVM yapısı için CommunityToolkit.Mvvm kullanılacaktır.
- Değişiklikler açık **Kaydet** ile uygulanacaktır; İptal taslağı geri alır.
- Doğrulama ve kaydetme Task 014 yönetim API'si üzerinden yapılacaktır.
- Yeni NuGet paketi eklenmeyecektir.

## Ekran Bölümleri

### Bağlantı

- Bağlantı türü: Wi-Fi / USB Serial.
- Wi-Fi: cihaz adresi (Host) ve port.
- USB Serial: seri port açılır listesi, listeyi yenileme butonu ve baud rate.
- Yalnızca seçili bağlantı türüne ait alanlar gösterilmelidir.
- Bağlantı durumu göstergesi ve tanınan cihazın bilgileri.
- "Şimdi bağlan" butonu (Task 014 `ReconnectAsync`).

### Uygulama

- Windows başlangıcında çalıştır.
- Sistem tepsisinde çalış.
- Otomatik yeniden bağlan.

### Girdi

- FN tuşu numarası.
- Uzun basma süresi (milisaniye).
- FN + tuş → profil eşleştirme tablosu (satır ekle/sil, profil açılır listeden seçilir).

## Davranış Kuralları

- Sayfa açıldığında mevcut ayarlar Runtime'dan okunur ve taslağa kopyalanır.
- Taslakta değişiklik yoksa Kaydet pasif olmalıdır.
- Kaydet'te doğrulama hatası dönerse hata ilgili alanın yanında gösterilmeli, sayfa kapanmamalıdır.
- Kaydedilmemiş değişiklik varken başka sayfaya geçilirse kullanıcıya uyarı gösterilmelidir (Kaydet / Vazgeç / İptal).
- Başarılı kayıttan sonra kullanıcıya kısa bir onay gösterilmelidir.

## Davranış Değişikliği

`RunInSystemTray` ayarı şu an uygulamada kullanılmamaktadır; pencere kapatıldığında uygulama her zaman tepsiye küçülür.

Bu task'ta ayar düzenlenebilir hale geldiği için:

- `RunInSystemTray = true`: pencereyi kapatmak tepsiye küçültür (mevcut davranış).
- `RunInSystemTray = false`: pencereyi kapatmak uygulamayı tamamen kapatır.

Varsayılan değer `true` olduğundan mevcut kullanıcılar için davranış değişmez.

## Geliştirme Adımları

### 1. ViewModel

- Ayarlar ViewModel'ini taslak modeliyle oluştur.
- Kaydet, İptal, port listesini yenile ve Şimdi bağlan komutlarını ekle.

### 2. Görünüm

- Bağlantı, Uygulama ve Girdi bölümlerini oluştur.
- Bağlantı türüne göre alan görünürlüğünü uygula.
- Alan bazlı doğrulama mesajlarını göster.

### 3. Kaydedilmemiş Değişiklik Koruması

- Navigasyon sırasında taslak kontrolünü Task 015 kabuğuna bağla.

### 4. Sistem Tepsisi Ayarı

- `RunInSystemTray` davranışını `App.xaml.cs` pencere kapatma akışına uygula.

## Kapsam Dışı

- Profil ve tuş düzenleme (017).
- Protokol veya ayar JSON formatı değişikliği.
- Cihaz arama/otomatik keşif (ağda ESP32 bulma).
- Firmware güncelleme.
- Bağlantı ayarlarının cihaza gönderilmesi.

## Riskler ve Koruma Kuralları

- Geçersiz ayar kaydedilmemeli; çalışan bağlantı bozulmamalıdır.
- Arayüz transport oluşturmamalı; seri port listesi ve yeniden bağlanma Runtime API'si üzerinden yapılmalıdır.
- Bağlantı bilgileri loglanmamalıdır.
- `RunInSystemTray` dışındaki mevcut pencere/tray davranışı değişmemelidir.

## Kabul Kriterleri

- [ ] Bağlantı türü, Host, Port, seri port ve baud rate düzenlenebiliyor.
- [ ] Seri port listesi alınabiliyor ve yenilenebiliyor.
- [ ] Uygulama ayarları düzenlenebiliyor.
- [ ] FN tuşu, uzun basma süresi ve FN profil eşleştirmeleri düzenlenebiliyor.
- [ ] Kaydet doğrulama yapıyor; hatalar alanların yanında gösteriliyor.
- [ ] İptal taslağı geri alıyor.
- [ ] Kaydedilmemiş değişiklikte sayfa değişiminde uyarı veriliyor.
- [ ] Kaydedilen bağlantı ayarları uygulama yeniden başlatılmadan uygulanıyor.
- [ ] "Şimdi bağlan" çalışıyor ve bağlantı durumu ekranda güncelleniyor.
- [ ] Windows başlangıç ayarı kaydedildiğinde uygulanıyor.
- [ ] `RunInSystemTray` ayarı pencere kapatma davranışını belirliyor.
- [ ] Mevcut otomatik testler geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, ayarlar ekranı üzerinden yapılan değişiklikler kaydedilip uygulandığında, otomatik testler geçtiğinde ve kullanıcı ekranı onayladığında tamamlanmış sayılacaktır.

Gerçek cihazla bağlantı testi Task 010 kapsamında açık kalan fiziksel testlerle birlikte yapılabilir; bu task'ın tamamlanmasını engellemez.
