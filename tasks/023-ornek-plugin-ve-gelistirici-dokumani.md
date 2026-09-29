# Task 023 — Örnek Plugin ve Geliştirici Dokümanı

## Durum

Planlandı. Kodlama başlamadı.

## Amaç

Plugin sisteminin gerçek bir örnek plugin ile uçtan uca çalıştığını doğrulamak ve plugin geliştirmek isteyenler için bir geliştirici dokümanı hazırlamak.

## Onaylanan Teknik Kararlar

- Örnek plugin yalnızca `SipoDeck.Plugin.Abstractions` sözleşmesini kullanacaktır; Runtime veya Core iç yapılarına başvurmayacaktır.
- Örnek plugin repodaki mevcut `plugins/` klasöründe bulunacaktır (önerilen: `plugins/SipoDeck.Plugins.Sample`).
- Geliştirici dokümanı `docs/plugin-gelistirme.md` olarak yazılacaktır.
- Doküman Task 020 mimari dokümanına (`docs/plugin-mimarisi.md`) başvuracak, aynı içeriği tekrar etmeyecektir.

## Karar Bekleyen Konular

### Örnek Plugin'in İşlevi

Task başlangıcında kullanıcıya sorulacaktır. Örnek plugin; en az bir parametreli eylem sağlamalı, en az bir yetki kullanmalı ve kendi ayarını saklamalıdır.

Aday örnekler:

- **Sayaç / Pomodoro:** Tuşa basınca zamanlayıcı başlatır, bitince bildirim gösterir (`notifications`).
- **HTTP isteği gönder:** Tanımlı bir adrese istek gönderir, sonucu bildirimle gösterir (`network`, `notifications`).
- **Rastgele metin / not:** Kendi veri dosyasından bir metni bildirimle gösterir (yalnızca kendi veri klasörü).

## Örnek Plugin Gereksinimleri

- Geçerli bir `plugin.json` manifesti.
- Plugin yaşam döngüsünün (başlat/durdur) uygulanması.
- En az bir parametreli eylemin kataloğa eklenmesi.
- Plugin ayarının plugin veri klasöründe saklanması.
- Hata durumunda anlamlı hata mesajı üretilmesi.
- İptal isteğine uyulması.
- Derlendiğinde plugin klasörüne kopyalanabilecek bir çıktı üretmesi.

## Geliştirici Dokümanı İçeriği

1. Plugin nedir, ne yapabilir, ne yapamaz.
2. Gereksinimler (.NET sürümü, sözleşme paketi/projesi).
3. Yeni plugin projesi oluşturma adımları.
4. Manifest alanları ve örnek.
5. Yaşam döngüsü ve zaman sınırları.
6. Eylem tanımlama: katalog tanımı, parametreler, çalıştırma, iptal.
7. Yetkiler: hangi yetki ne için, kullanıcı onayı nasıl işler.
8. Plugin ayarları ve veri klasörü.
9. Hata yönetimi ve günlük kaydı.
10. Plugin'i kurma, etkinleştirme ve hata ayıklama (debug) adımları.
11. Sürümleme ve `apiVersion` uyumluluğu.
12. Güvenlik uyarıları ve iyi uygulamalar (gizli bilgi saklamama, kullanıcı verisine saygı).

## Uçtan Uca Doğrulama

```text
Örnek plugin derlenir
    ↓
Plugin klasörüne kopyalanır
    ↓
Plugin Yönetim Ekranında görünür (022)
    ↓
Yetki onayıyla etkinleştirilir
    ↓
Plugin eylemi bir tuşa atanır (018 / 019)
    ↓
Tuş olayı → eylem kuyruğu → plugin eylemi çalışır
    ↓
Devre dışı bırakılır → eylem çalışmaz, atama korunur
```

Tuş olayı donanım olmadan Runtime doğrulama senaryosuyla (Task 012/013) üretilebilir.

## Geliştirme Adımları

### 1. Karar

- Örnek plugin'in işlevini kullanıcıya sor.

### 2. Örnek Plugin

- `plugins/SipoDeck.Plugins.Sample` projesini oluştur ve solution'a ekle.
- Manifest, yaşam döngüsü, eylem, ayar ve hata yönetimini uygula.
- Derleme çıktısının plugin klasörüne kopyalanma yöntemini belirle ve dokümana yaz.

### 3. Doküman

- `docs/plugin-gelistirme.md` dosyasını yaz.
- Dokümandaki adımları sıfırdan takip ederek doğrula.

### 4. Doğrulama

- Uçtan uca doğrulama akışını çalıştır.
- Örnek plugin için otomatik test ekle (manifest geçerliliği, eylem çalıştırma, iptal).
- README'ye plugin geliştirme dokümanı bağlantısını ekle.

## Kapsam Dışı

- Plugin proje şablonu (`dotnet new` şablonu).
- Plugin sözleşmesinin NuGet'te yayınlanması.
- Plugin mağazası.
- Birden fazla örnek plugin.

## Riskler ve Koruma Kuralları

- Örnek plugin'de gerçek adres, token, şifre veya kişisel bilgi bulunmamalıdır; placeholder kullanılmalıdır.
- Örnek plugin sözleşme dışında hiçbir iç API kullanmamalıdır; kullanması gerekiyorsa sözleşmede eksiklik vardır ve kullanıcıya bildirilmelidir.
- Doküman ile gerçek davranış arasında fark bulunursa doküman değil davranış doğrulanmalı, fark kullanıcıya bildirilmelidir.
- Örnek plugin varsayılan olarak kullanıcının plugin klasörüne kurulmamalıdır.

## Kabul Kriterleri

- [ ] Örnek plugin işlevi kullanıcı tarafından seçildi.
- [ ] Örnek plugin yalnızca sözleşmeyi kullanıyor.
- [ ] Örnek plugin manifest, yaşam döngüsü, eylem, ayar ve yetki kullanımını gösteriyor.
- [ ] Uçtan uca doğrulama akışı başarılı.
- [ ] `docs/plugin-gelistirme.md` yazıldı ve adımları sıfırdan takip edilerek doğrulandı.
- [ ] Örnek plugin için otomatik testler eklendi ve geçiyor.
- [ ] README'de doküman bağlantısı var.
- [ ] Solution hatasız derleniyor.

## Test Komutları

```powershell
dotnet build SipoDeck.sln --no-restore
dotnet test SipoDeck.sln --no-build
dotnet run --project src\SipoDeck\SipoDeck.csproj --no-build
```

## Tamamlanma Sınırı

Bu task, örnek plugin uçtan uca çalıştığında, geliştirici dokümanı doğrulandığında ve kullanıcı plugin'i uygulama içinden etkinleştirip bir tuşa atayarak denediğinde tamamlanmış sayılacaktır.
