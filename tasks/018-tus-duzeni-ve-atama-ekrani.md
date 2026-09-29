# Task 018 — Tuş Düzeni ve Atama Ekranı

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Fiziksel tuşların arayüzde gösterilmesini, seçilmesini ve profil bazlı olarak tuşlara eylem atanabilmesini sağlamak.

Bu task tuş → eylem eşleştirmesinin yapısını kurar. Eylem türlerinin kataloğu ve makro zinciri düzenleyicisi Task 019 kapsamındadır.

## Onaylanan Teknik Kararlar

- Ekran Task 017 profil detayından açılacaktır.
- MVVM yapısı için CommunityToolkit.Mvvm kullanılacaktır.
- Değişiklikler açık **Kaydet** ile uygulanacaktır; İptal taslağı geri alır.
- Doğrulama ve kaydetme Task 014 yönetim API'si üzerinden yapılacaktır.
- Profil JSON formatındaki tuş (`Keys`) ve kombinasyon (`Combinations`) yapısı korunacaktır.

## Karar Bekleyen Konular

### Tuş Sayısı ve Düzeni

Cihazın `hello` mesajı tuş sayısını bildirmemektedir. Task başlangıcında kullanıcıya karar formatıyla sorulacaktır.

Seçenek 1: **Serbest tuş listesi**

- Kullanıcı "Tuş ekle" ile istediği tuş numarasını ekler.
- Protokol ve ayar değişikliği gerekmez; görsel tuş ızgarası olmaz.

Seçenek 2: **Ayarlarda tuş düzeni**

- Ayarlara satır × sütun tuş düzeni eklenir; ekran bu düzende tuş ızgarası gösterir.
- Ayar modeline yeni alan eklenir (geriye dönük uyumlu).

Seçenek 3: **Protokole tuş bilgisi eklemek**

- Cihaz `hello` mesajında tuş sayısını (ve isteğe bağlı düzenini) bildirir; ızgara otomatik oluşur.
- Protokol ve firmware değişikliği gerektirir; bağlı cihaz yokken düzen bilinmez.

Karar verilmeden tuş ızgarası görünümü geliştirilmemelidir.

## Ekran Bölümleri

### Tuş Düzeni

- Tuşlar karara göre ızgara veya liste olarak gösterilir.
- Her tuşta atanmış eylemin kısa özeti ve uzun basma eylemi varsa işareti gösterilir.
- FN tuşu (Task 016 ayarı) ayrıca işaretlenir; FN tuşuna tekli eylem atanamaz.
- Bir tuş seçildiğinde atama paneli açılır.

### Fiziksel Tuşla Seçme

- "Tuşa basarak seç" modu açıldığında cihazdan gelen ilk tuş olayı ilgili tuşu seçer.
- Bu mod açıkken tuş olayları eylem çalıştırmamalıdır.
- Mod, seçim yapıldığında, kullanıcı iptal ettiğinde veya sayfadan çıkıldığında kapanmalıdır.
- Cihaz bağlı değilse mod kullanılamaz ve nedeni gösterilir.

### Tuş Atama Paneli

- Normal eylem (tuş bırakılınca çalışır).
- Uzun basma eylemi (isteğe bağlı).
- Eylem seçimi mevcut eylem türleriyle yapılır; her tür için temel alanlar gösterilir.
- Zincir eylemi bu task'ta yalnızca özet olarak gösterilir; düzenleyici Task 019'da eklenir.
- Tuşun atamasını temizle.
- Atamayı başka bir tuşa kopyala.

### Kombinasyonlar

- Kombinasyon ekle: en az 2 tuş seçilir + eylem atanır.
- Kombinasyonları listele, düzenle ve sil.
- Aynı tuş kümesine sahip ikinci kombinasyon eklenemez.

## Runtime Gereksinimleri

- Arayüzün tuş olaylarını dinleyebilmesi için Runtime tipli bir tuş olayı yayınlamalıdır.
- Runtime'a "tuş seçme modu" (eylemleri geçici olarak durdurma) desteği eklenmelidir.
- Tuş seçme modu açık kalırsa uygulama kapanışında ve sayfa değişiminde otomatik kapanmalıdır.

## Geliştirme Adımları

### 1. Tuş Düzeni Kararı

- Tuş sayısı ve düzeni kararını kullanıcıya sor.

### 2. Runtime Desteği

- Tipli tuş olayı ve tuş seçme modunu Runtime'a ekle.
- Bu davranışlar için Task 013 test projelerine test ekle.

### 3. ViewModel'ler

- Tuş düzeni, tuş, atama paneli ve kombinasyon ViewModel'lerini oluştur.

### 4. Görünümler

- Tuş düzeni, atama paneli ve kombinasyon görünümlerini oluştur.
- Tuşa basarak seçme modunu arayüze bağla.

### 5. Kaydetme ve Doğrulama

- Kaydet / İptal ve kaydedilmemiş değişiklik korumasını uygula.

## Kapsam Dışı

- Eylem kataloğu ve zincir/makro düzenleyici (019).
- Tuşlara simge veya renk atama.
- Cihaz üzerinde ekran/LED gösterimi.
- Profil yönetimi (017).

## Riskler ve Koruma Kuralları

- Tuş seçme modu açık kalıp eylemleri kalıcı olarak durdurmamalıdır.
- Input Engine değerlendirme sırası (FN → kombinasyon → tekli) değiştirilmemelidir.
- Mevcut `profiles.json` dosyaları sorunsuz açılmaya devam etmelidir.
- Tuş düzeni kararı protokol değişikliği içerirse firmware ile birlikte güncellenmeli ve Task 010 fiziksel testlerine eklenmelidir.

## Kabul Kriterleri

- [ ] Tuş düzeni kararı kullanıcı tarafından verildi ve uygulandı.
- [ ] Profilin tuşları ve atamaları görüntülenebiliyor.
- [ ] Tuşa normal ve uzun basma eylemi atanabiliyor ve temizlenebiliyor.
- [ ] Atama başka tuşa kopyalanabiliyor.
- [ ] FN tuşu işaretleniyor ve tekli eylem atanamıyor.
- [ ] Kombinasyonlar eklenip düzenlenebiliyor ve silinebiliyor.
- [ ] Tuşa basarak seçme modu çalışıyor ve bu modda eylem çalışmıyor.
- [ ] Kaydet doğrulama yapıyor; İptal taslağı geri alıyor.
- [ ] Kaydedilen atamalar uygulama yeniden başlatılmadan çalışıyor.
- [ ] Profil JSON formatı değişmedi (karar ayar/protokol değişikliği içeriyorsa geriye dönük uyumlu).
- [ ] Mevcut ve yeni otomatik testler geçiyor.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, tuş atamaları arayüzden yapılabildiğinde, otomatik testler geçtiğinde ve kullanıcı ekranı onayladığında tamamlanmış sayılacaktır.

Tuşa basarak seçme modunun gerçek cihazla denenmesi Task 010 fiziksel testleriyle birlikte yapılabilir; bu task'ın tamamlanmasını engellemez.
