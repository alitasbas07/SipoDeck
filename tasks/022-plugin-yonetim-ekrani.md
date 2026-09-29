# Task 022 — Plugin Yönetim Ekranı

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Kullanıcının yüklü plugin'leri görebilmesini, etkinleştirip devre dışı bırakabilmesini, istenen yetkileri onaylayabilmesini ve plugin hatalarını görebilmesini sağlamak.

## Onaylanan Teknik Kararlar

- Ekran Task 015 iskeletine yeni bir "Plugin'ler" menü öğesi olarak eklenecektir.
- MVVM yapısı için CommunityToolkit.Mvvm kullanılacaktır.
- Bütün işlemler Task 021 Runtime plugin yönetim API'si üzerinden yapılacaktır; arayüz plugin yüklemeyecektir.
- Yetki onay akışı Task 020 kararına göre uygulanacaktır.

## Ekran Bölümleri

### Plugin Listesi

- Her plugin için: ad, sürüm, yazar, açıklama ve durum (Devre dışı, Çalışıyor, Hata, Geçersiz).
- Durum renk veya simgeyle ayırt edilebilir olmalıdır.
- Geçersiz plugin'ler listede nedeniyle birlikte gösterilmelidir.

### Plugin Detayı

- Manifest bilgileri ve `apiVersion`.
- İstenen yetkiler ve her yetkinin kullanıcıya anlaşılır açıklaması.
- Plugin'in sağladığı eylemler.
- Son hata mesajı ve hata zamanı.
- Plugin eylemlerini kullanan profillerin listesi.

### İşlemler

- Etkinleştir (yetki onayı gerekiyorsa önce onay penceresi gösterilir).
- Devre dışı bırak.
- Hata durumundaki plugin'i yeniden başlat.
- Plugin klasörünü aç.
- Plugin listesini yenile (klasörü yeniden tara).

## Davranış Kuralları

- Yetki onay penceresi istenen bütün yetkileri açıkça listelemeli; kullanıcı reddederse plugin etkinleşmemelidir.
- Seçilen izolasyon modelinde yetkiler yalnızca beyan düzeyindeyse bu durum onay penceresinde belirtilmelidir (Task 020 kararı).
- Devre dışı bırakılan plugin'in eylemlerini kullanan profiller varsa kullanıcı bilgilendirilmelidir; atamalar silinmez.
- Plugin durum değişiklikleri Runtime olaylarıyla anlık olarak ekrana yansımalıdır.
- Hata mesajları kullanıcıya anlaşılır şekilde, ayrıntı isteğe bağlı açılabilir olarak gösterilmelidir.
- Durum alanında (Task 015) hata durumundaki plugin sayısı gösterilmelidir.

## Geliştirme Adımları

### 1. ViewModel'ler

- Plugin listesi, plugin detayı ve yetki onayı ViewModel'lerini oluştur.

### 2. Görünümler

- Plugin listesi, detay ve yetki onay penceresini oluştur.
- Menü öğesini Task 015 kabuğuna ekle.

### 3. Runtime Bağlantısı

- Plugin durum olaylarını ekrana bağla (UI thread'ine aktararak).
- Durum alanına hata sayacını ekle.

## Kapsam Dışı

- Plugin mağazası, arama ve çevrimiçi kurulum.
- Plugin dosyası sürükle-bırak ile kurulum.
- Plugin otomatik güncelleme.
- Plugin'e özel ayar ekranları (ayrı task olarak planlanabilir).

## Riskler ve Koruma Kuralları

- Kullanıcı onayı olmadan hiçbir plugin etkinleştirilmemelidir.
- Arayüz plugin kodunu doğrudan çalıştırmamalıdır.
- Hata ayrıntılarında kullanıcının kişisel veya bağlantı bilgileri gösterilmemelidir.
- Mevcut navigasyon ve sayfalar değişmemelidir; yalnızca yeni menü öğesi eklenir.

## Kabul Kriterleri

- [ ] Plugin'ler durumlarıyla listeleniyor; geçersiz plugin'ler nedeniyle gösteriliyor.
- [ ] Plugin detayı, yetkiler ve sağlanan eylemler görüntülenebiliyor.
- [ ] Etkinleştirme yetki onayıyla çalışıyor; reddedilince plugin etkinleşmiyor.
- [ ] Devre dışı bırakma çalışıyor ve etkilenen profiller bildiriliyor.
- [ ] Hata durumundaki plugin yeniden başlatılabiliyor.
- [ ] Plugin klasörü açılabiliyor ve liste yenilenebiliyor.
- [ ] Durum değişiklikleri ekrana anlık yansıyor.
- [ ] Durum alanında hata sayısı gösteriliyor.
- [ ] Mevcut otomatik testler geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, plugin'ler arayüzden yönetilebildiğinde, otomatik testler geçtiğinde ve kullanıcı ekranı onayladığında tamamlanmış sayılacaktır.

Gerçek bir plugin ile uçtan uca deneme Task 023 örnek plugin'i ile yapılacaktır.
