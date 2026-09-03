# Task 007 — Ayar Sistemi

## Amaç

SipoDeck içerisinde kullanıcı tarafından değiştirilebilecek ayarların merkezi bir yapı üzerinden yönetilmesini sağlamak.

## Ayar Grupları

Temel olarak aşağıdaki ayarlar desteklenmelidir:

### Bağlantı

- Bağlantı türü
- IP adresi / cihaz adresi
- Port
- Serial port
- Baud rate

### Girdi

- FN tuşu
- Uzun basma süresi

### Uygulama

- Windows başlangıcında çalıştırma
- Sistem tepsisinde çalışma
- Otomatik yeniden bağlanma

## Profil Ayarları

Profil ve profil içerisindeki tuş eşleştirmeleri ayrı bir yapı olarak tutulmalıdır.

Profil verileri genel uygulama ayarlarıyla birbirine karıştırılmamalıdır.

## Kalıcılık

Kullanıcı ayarları uygulama kapatılıp açıldığında korunmalıdır.

Ayarların nerede ve hangi formatta saklanacağı uygulama mimarisine uygun şekilde belirlenmelidir.

## Varsayılan Değerler

Gerekli ayarlar için varsayılan değerler bulunmalıdır.

Örneğin:

```text
Uzun basma süresi → varsayılan değer
FN tuşu → varsayılan fiziksel tuş
Otomatik yeniden bağlanma → etkin
```

Varsayılan değerler kodun farklı bölümlerine dağılmamalıdır.

## Kurallar

- Ayarlar merkezi bir yapı üzerinden yönetilmelidir.
- IP, port ve Serial bilgileri kod içine sabitlenmemelidir.
- Profil verileri uygulama ayarlarından ayrı tutulmalıdır.
- Ayarlar uygulama yeniden başlatıldığında korunmalıdır.
- Varsayılan değerler merkezi olarak tanımlanmalıdır.
- Hassas bilgiler loglara yazılmamalıdır.
- Gereksiz karmaşık bir configuration sistemi oluşturulmamalıdır.

## Kabul Kriterleri

- [ ] Temel ayar modeli oluşturuldu.
- [ ] Bağlantı ayarları destekleniyor.
- [ ] Girdi ayarları destekleniyor.
- [ ] Uygulama ayarları destekleniyor.
- [ ] Ayarlar kalıcı olarak saklanıyor.
- [ ] Varsayılan değerler tanımlandı.
- [ ] Profil verileri ayrı tutuluyor.
- [ ] Hassas ayarlar loglanmıyor.
- [ ] Proje derleniyor.
