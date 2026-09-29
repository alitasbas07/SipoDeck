# SipoDeck Development Rules

Bu kurallar yalnızca `~/apps/SipoDeck` projesi için geçerlidir.

## Proje İçinde Serbest Yapabileceklerin

Görev kapsamında gerekiyorsa kullanıcıdan ayrıca izin almadan şunları yapabilirsin:

- Dosya oluşturma, düzenleme, silme
- Yeni klasör oluşturma
- Kod refactor etme, ancak yalnızca görev için gerçekten gerekiyorsa
- Git branch oluşturma
- Branch değiştirme
- Commit oluşturma
- `git add`
- `git commit`
- `git pull`
- `git fetch`
- `git push`
- Build çalıştırma
- Test çalıştırma
- Proje içindeki config dosyalarını düzenleme
- Projenin mevcut dependency'lerini kullanma
- Task kapsamında gerekli geliştirmeyi baştan sona tamamlama

Main/master üzerinde doğrudan geliştirme yapmak yerine mümkün olduğunda görev için ayrı branch oluştur.

## Kullanıcıdan Önce İzin Alınması Gerekenler

Aşağıdaki işlemleri yapmadan önce kullanıcıya sor ve onay bekle:

### Yeni Dependency / Paket

Projede daha önce bulunmayan herhangi bir dependency kurulacaksa:

- NuGet package
- npm package
- pip package
- global CLI
- başka herhangi bir harici dependency

Önce paket adını ve neden gerektiğini söyle.

Kullanıcı onayından sonra kurulumu yapabilirsin.

### Sunucu Seviyesinde Değişiklikler

`~/apps/SipoDeck` dışındaki sistemi etkileyen işlemler için önce izin al:

- `apt install`
- `apt remove`
- servis oluşturma/değiştirme
- systemd işlemleri
- nginx değişiklikleri
- firewall değişiklikleri
- SSH ayarları
- kullanıcı/grup değişiklikleri
- global environment değişiklikleri
- Docker daemon veya başka projelere ait container değişiklikleri
- sistem genelinde dosya oluşturma/silme
- proje dışındaki dosyaları değiştirme

## Git Kuralları

Görev kapsamında branch, commit ve push işlemlerini yapabilirsin.

Ancak:

- `git push --force`
- `git reset --hard`
- branch silme
- commit history rewrite
- main/master'a force push

işlemlerini kullanıcı onayı olmadan yapma.

## Geliştirme Yaklaşımı

- Sadece verilen göreve odaklan.
- Gereksiz kapsam genişletme.
- Çalışan yapıyı bozma.
- Mevcut mimariyi mümkün olduğunca koru.
- Gereksiz yeni abstraction veya katman ekleme.
- Mevcut kod stilini takip et.
- Önce ilgili kodu incele, sonra uygula.
- Görevi tamamlamak için gereken tüm proje içi işlemleri kendin yap.

## Görev Sonu

Görev tamamlandığında kısa şekilde bildir:

- Açılan branch
- Değiştirilen dosyalar
- Yapılan geliştirme
- Build/test sonucu
- Commit hash
- Push yapıldıysa remote branch adı
- Kullanıcıdan onay bekleyen herhangi bir işlem varsa belirt
