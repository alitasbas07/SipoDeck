# Task 019 — Eylem Kataloğu ve Makro Editörü

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Yerleşik eylemleri tek bir katalogda tanımlamak ve kullanıcının sıralı eylem zincirleri (makrolar) hazırlayabileceği bir düzenleyici sağlamak.

Eylem kataloğu, ileride plugin'lerin kendi eylemlerini ekleyeceği ortak yapı olacaktır (Task 020–021).

## Onaylanan Teknik Kararlar

- Eylemler mevcut `ActionConfig` JSON türleriyle saklanmaya devam edecektir.
- Yeni eylem türleri mevcut polimorfik yapıya (`JsonDerivedType`) eklenerek tanımlanacaktır; eski profiller sorunsuz açılmalıdır.
- Arayüz eylem düzenleyicilerini katalogdaki tanımlardan üretecektir; her eylem türü için ayrı sabit ekran yazılmayacaktır.
- MVVM yapısı için CommunityToolkit.Mvvm kullanılacaktır.

## Karar Bekleyen Konular

### 1. Yerleşik Eylem Listesi

Task başlangıcında kullanıcıya sorulacaktır. Mevcut eylemler:

- Program çalıştır, URL aç, Bekle, Bildirim, Ses, Medya, Zincir.

Eklenebilecek aday eylemler:

- Klavye kısayolu gönder (örn. Ctrl+Shift+M).
- Metin yaz.
- Dosya veya klasör aç.
- Profil değiştir.

### 2. Windows'a Özgü Eylemlerin Yeri

Ses, medya, bildirim ve klavye eylemlerinin gerçek davranışı Windows API'leri gerektirir. `SipoDeck.Core` platformdan bağımsızdır.

Seçenek 1: **Ayrı `SipoDeck.Platform.Windows` projesi**

- Windows'a özgü eylem uygulamaları ayrı projede bulunur; Core bağımsız kalır.
- Yeni proje ve katman eklenir.

Seçenek 2: **WPF projesinde uygulamak**

- Yeni proje gerekmez.
- Eylemler arayüz katmanına bağlanır; Runtime'ın başka istemcilerle kullanımı zorlaşır.

### 3. Eylem Gerçek Davranışları

Ses, medya ve bildirim eylemleri şu an temel yapı düzeyindedir. Bu task'ta gerçek davranışlarının uygulanıp uygulanmayacağı kullanıcıya sorulacaktır.

## Eylem Kataloğu

Her eylem türü için katalog tanımı şunları içermelidir:

- Benzersiz tür kimliği (JSON `type` değeri ile aynı).
- Görünen ad ve kısa açıklama.
- Kategori (Sistem, Uygulama, Medya, Akış vb.).
- Parametre tanımları: ad, tür (metin, sayı, seçim, dosya yolu, tuş kısayolu), zorunluluk, varsayılan değer, doğrulama kuralı.

Kurallar:

- Katalog Runtime üzerinden okunabilmelidir.
- Arayüz eylem seçiciyi ve parametre formlarını katalogdan üretmelidir.
- Parametre doğrulaması Task 014 doğrulama yapısıyla birleştirilmelidir.
- Katalog ileride plugin eylemlerinin eklenebileceği şekilde tasarlanmalı, ancak bu task'ta plugin desteği yazılmamalıdır.

## Makro Editörü

- Zincir eylemini düzenlemek için sıralı adım listesi.
- Adım ekle (katalogdan eylem seçerek), sil, kopyala, yukarı/aşağı taşı.
- Adımlar arasına bekleme eklemek.
- Her adımın parametrelerini katalog formuyla düzenlemek.
- Zincirin toplam adım sayısı ve tahmini bekleme süresinin gösterilmesi.
- Task 018 atama panelinden ve kombinasyonlardan açılabilmelidir.

Kurallar:

- Zincir iç içe zincir içerebilir; ancak sonsuz döngü oluşturacak yapı kabul edilmemelidir.
- Zincir çalışma davranışı Task 012 kurallarıyla aynı kalır: sıralı çalışma, hata sonrası devam, iptalde durma.
- Aşırı uzun zincirler için makul bir adım sınırı doğrulamaya eklenmelidir.

## Geliştirme Adımları

### 1. Kararlar

- Yerleşik eylem listesi, Windows eylemlerinin yeri ve gerçek davranış kararlarını kullanıcıya sor.

### 2. Katalog Modeli

- Eylem ve parametre tanım modellerini oluştur.
- Mevcut eylem türlerini kataloğa kaydet.
- Katalog okuma yeteneğini Runtime'a ekle.

### 3. Yeni Eylemler

- Karara göre yeni eylem türlerini ve `ActionConfig` kayıtlarını ekle.
- Karara göre Windows'a özgü eylemlerin gerçek davranışını uygula.

### 4. Düzenleyiciler

- Katalogdan üretilen eylem seçici ve parametre formlarını oluştur.
- Makro editörünü oluştur.
- Task 018 atama panelindeki temel eylem formlarını katalog tabanlı düzenleyiciyle değiştir.

### 5. Testler

- Katalog, parametre doğrulama, yeni eylemlerin JSON uyumluluğu ve zincir döngü kontrolü için testler ekle.

## Kapsam Dışı

- Plugin eylemleri (020–021).
- Eylem kaydetme (tuş/fare hareketlerini kaydederek makro oluşturma).
- Koşullu adımlar ve döngüler (if/repeat).
- Eylemi arayüzden "test et" butonu.
- Profil içe/dışa aktarma.

## Riskler ve Koruma Kuralları

- Mevcut `profiles.json` dosyalarındaki eylemler aynı şekilde çalışmaya devam etmelidir.
- Klavye/metin gönderme eylemleri odaktaki uygulamaya girdi gönderir; kullanıcıya bu davranış açıkça belirtilmelidir.
- Program çalıştırma ve dosya açma eylemleri yalnızca kullanıcının girdiği hedefleri kullanmalıdır.
- Core katmanına Windows veya WPF bağımlılığı eklenmemelidir.
- Katalog tasarımı plugin sözleşmesini (020) önceden bağlayıcı şekilde belirlememelidir.

## Kabul Kriterleri

- [ ] Yerleşik eylem listesi ve platform kararları kullanıcı tarafından verildi.
- [ ] Bütün yerleşik eylemler katalogda tanımlı.
- [ ] Eylem seçici ve parametre formları katalogdan üretiliyor.
- [ ] Makro editöründe adım eklenip silinebiliyor, kopyalanabiliyor ve sıralanabiliyor.
- [ ] Zincir döngüsü ve adım sınırı doğrulanıyor.
- [ ] Karara göre eklenen yeni eylemler çalışıyor.
- [ ] Eski profillerdeki eylemler değişmeden çalışıyor.
- [ ] Yeni davranışlar için otomatik testler eklendi ve geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, eylem kataloğu ve makro editörü çalıştığında, otomatik testler geçtiğinde ve kullanıcı yerleşik eylemleri ve makro düzenlemeyi onayladığında tamamlanmış sayılacaktır.
