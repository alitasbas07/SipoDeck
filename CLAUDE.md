# SipoDeck Development Rules

Bu kurallar yalnızca SipoDeck projesi için geçerlidir.

## 1. Genel Çalışma Şekli

Claude aktif task kapsamında geliştirmeyi baştan sona yürütebilir.

Görev kapsamında kullanıcıdan ayrıca izin almadan:

- İlgili kodları okuyabilir
- Dosya oluşturabilir, düzenleyebilir ve gerekirse silebilir
- Yeni branch oluşturabilir
- Branch değiştirebilir
- `git add` yapabilir
- `git commit` yapabilir
- `git fetch` / `git pull` yapabilir
- Task branch'ini remote'a `git push` yapabilir
- Pull Request oluşturabilir
- Task kapsamında gerekli refactorları yapabilir

Gereksiz kapsam genişletme, büyük refactor veya mimari değişiklik yapma.

---

## 2. Git Workflow

Her geliştirme task'ı ayrı branch üzerinde yapılmalıdır.

Tercih edilen akış:

main
↓
yeni task branch'i
↓
geliştirme
↓
kontrol
↓
commit
↓
push
↓
Pull Request

Branch isimlerini göreve göre Claude belirleyebilir.

Örnek:

- `feature/open-folder-action`
- `fix/input-release`
- `task/profile-editor`

Claude task branch'i üzerinde commit ve push işlemlerini kullanıcıdan ayrıca izin almadan yapabilir.

### Main Branch

`main` stabil branch'tir.

Claude:

- `main` üzerinde doğrudan geliştirme yapmamalıdır
- `main` branch'ine doğrudan push yapmamalıdır
- `main` branch'ine force push yapmamalıdır
- PR'ı kendi başına merge etmemelidir

Main'e geçiş Pull Request üzerinden yapılır.

### Yasak Git İşlemleri

Kullanıcıdan açık izin almadan:

- `git push --force`
- `git push --force-with-lease`
- `git reset --hard`
- branch silme
- commit history rewrite
- rebase ile geçmiş değiştirme

yapma.

---

## 3. Task Kapsamı

Sadece verilen görevin kapsamı içinde çalış.

- Gereksiz feature ekleme
- İlgisiz dosyalara dokunma
- Gelecekte lazım olur diye kod ekleme
- Gereksiz abstraction oluşturma
- Gereksiz interface/service/helper oluşturma
- Mevcut çalışan yapıyı sebepsiz değiştirme

Task sırasında kapsam dışı bir problem fark edilirse kullanıcıya bildir ancak kendiliğinden çözme.

---

## 4. Mevcut Yapıyı Koruma

Yeni geliştirmeden önce ilgili mevcut kodu incele.

Tercih sırası:

1. Mevcut yapıyı kullan
2. Mevcut sınıf/metodu genişlet
3. Minimum yeni kod ekle
4. Yeni mimari veya dependency son seçenek olsun

Mevcut kod stilini takip et.

---

## 5. Kullanıcıdan Onay Gerektiren İşlemler

### Yeni Paket / Dependency

Projede daha önce bulunmayan herhangi bir dependency eklemeden önce kullanıcıdan izin al.

Örnek:

- NuGet package
- npm package
- pip package
- CLI tool
- harici library
- global package

Önce şunları bildir:

- Paket adı
- Neden gerekli
- Mevcut yapı ile çözülebilir mi
- Eklenecek komut

Sonra kullanıcı onayını bekle.

### Sunucu Seviyesinde Değişiklik

`~/apps/SipoDeck` dışındaki sistemi etkileyen işlemlerden önce kullanıcıya sor.

Örnek:

- `apt install`
- `apt remove`
- systemd değişiklikleri
- nginx değişiklikleri
- firewall değişiklikleri
- SSH ayarları
- kullanıcı/grup izinleri
- global environment değişiklikleri
- Docker daemon değişiklikleri
- proje dışındaki dosyaları değiştirme

---

## 6. Mimari Kararlar

Aşağıdaki gibi önemli bir karar gerekiyorsa kullanıcıya sor:

- Yeni framework
- Yeni mimari yaklaşım
- Core yapısının değiştirilmesi
- Plugin API değişikliği
- Veri formatı değişikliği
- ESP32 protokol değişikliği
- Büyük UI akışı değişikliği
- Güvenlik modeli değişikliği

Birden fazla mantıklı çözüm varsa 2-3 seçenek sun ve seçim iste.

---

## 7. Build ve Test Ortamı

SipoDeck Windows üzerinde çalışan WPF / .NET 10 uygulamasıdır.

Geliştirme ortamı Ubuntu sunucusudur.

Bu nedenle:

- Windows gerektiren WPF build/test işlemlerini Ubuntu üzerinde zorla çalıştırma
- Platform nedeniyle çalışmayacak `dotnet build` / `dotnet test` işlemlerini tekrar tekrar deneme
- Wine, Windows VM, yeni container veya ek build ortamı kullanıcı istemeden kurma

Kod değişikliğinden sonra Ubuntu üzerinde mümkün olan kontrolleri yap:

- Değiştirilen dosyaları tekrar incele
- Syntax ve namespace kullanımını kontrol et
- Referansları kontrol et
- Mevcut akışla uyumluluğu kontrol et
- Graft veya mevcut analiz araçlarını gerektiğinde kullan

Windows üzerinde doğrulanması gereken noktaları görev sonunda belirt.

Gerçek Windows/WPF/ESP32 testi kullanıcı tarafından yapılacaktır.

Windows testi task branch'inin commit/push edilmesini engellemez.

---

## 8. ESP32

ESP32 temel olarak fiziksel input event üretmelidir.

Profil ve desktop action mantığı mümkün olduğunca Windows uygulamasında kalmalıdır.

Bu yapıyı değiştirecek bir task varsa kullanıcıya sor.

---

## 9. Güvenlik

Public repository içine gerçek:

- API key
- token
- password
- Wi-Fi şifresi
- private key
- credential
- secret
- sunucu erişim bilgisi

ekleme.

Placeholder kullan.

---

## 10. Task Sonu

Task tamamlandığında:

1. Değişiklikleri kontrol et
2. Task branch'inde commit oluştur
3. Remote'a push et
4. Mümkünse Pull Request oluştur
5. PR oluşturulamıyorsa PR linkini kullanıcıya ver
6. Kısa rapor yaz

Rapor:

- Branch
- Yapılan değişiklikler
- Değiştirilen dosyalar
- Ubuntu üzerinde yapılan kontroller
- Windows üzerinde test edilmesi gerekenler
- Commit hash
- Remote branch
- PR linki

Task bittikten sonra otomatik olarak başka bir task'a geçme.

---

## 11. Temel Kurallar

1. Task kapsamını büyütme.
2. Gereksiz refactor yapma.
3. Mevcut çalışan yapıyı koru.
4. Yeni dependency eklemeden önce sor.
5. Sunucu seviyesinde değişiklik yapmadan önce sor.
6. Önemli mimari kararlarda kullanıcıya sor.
7. Her task için ayrı branch kullan.
8. Task branch'inde commit ve push yap.
9. PR oluştur.
10. Main'e doğrudan push veya merge yapma.
11. Windows gerektiren testleri Ubuntu'da zorla çalıştırma.
12. Task bitince raporla ve dur.
