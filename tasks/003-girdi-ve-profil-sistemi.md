# Task 003 — Girdi ve Profil Sistemi

## Amaç

ESP32'den gelen tuş olaylarının aktif profil üzerinden değerlendirilmesini sağlamak.

## Profil Sistemi

Sistem birden fazla profil desteklemelidir.

Her profil:

- Benzersiz bir kimliğe sahip olmalıdır.
- Bir ada sahip olmalıdır.
- Etkin veya devre dışı olabilir.
- Tuşlara farklı eylemler atanabilmelidir.

Aynı fiziksel tuş farklı profillerde farklı görevler gerçekleştirebilir.

## Aktif Profil

Aynı anda yalnızca bir profil aktif olmalıdır.

Gelen tuş olayları yalnızca aktif profil üzerinden değerlendirilmelidir.

## FN Tuşu

FN tuşu sabit bir fiziksel tuşa bağlı olmamalıdır.

Kullanıcı herhangi bir fiziksel tuşu FN tuşu olarak belirleyebilmelidir.

FN tuşu:

- Normal bir makro eylemi çalıştırmaz.
- Sistem genelinde kombinasyon oluşturmak için kullanılabilir.

Örnek:

```text
FN + 1 → Profil 1
FN + 2 → Profil 2
FN + 3 → Profil 3
FN + 4 → Profil 4
```

Bu kombinasyonlar kullanıcı tarafından değiştirilebilir olmalıdır.

## Tuş Kombinasyonları

Sistem birden fazla tuşun birlikte kullanılmasını desteklemelidir.

Örneğin:

```text
FN + 1
FN + 5
FN + 8
```

Kombinasyonlar, tekli tuş eylemlerinden öncelikli değerlendirilmelidir.

Örneğin `FN + 1` tanımlıysa:

```text
FN + 1 → Kombinasyon eylemi
1     → Tekli tuş eylemi
```

kullanıcı `FN + 1` bastığında yalnızca kombinasyon eylemi çalışmalıdır.

## Tuş Durumu

Input Engine aşağıdaki durumları takip edebilmelidir:

- Tuşa basılması
- Tuşun bırakılması
- Tuşun basılı tutulması

Tuş bırakma olayları uzun basma süresinin hesaplanabilmesi için kullanılmalıdır.

## Uzun Basma

Uzun basma süresi kullanıcı tarafından yapılandırılabilir olmalıdır.

Örneğin:

```text
Tuş basıldı
    ↓
Basılı tutuluyor
    ↓
Belirlenen süre aşıldı
    ↓
Uzun basma eylemi
```

Varsayılan değer kodun içine sabit olarak gömülmemelidir.

## Kurallar

- ESP32 profil veya eylem bilgisi taşımaz.
- Input Engine cihaz olaylarını yorumlar.
- Profil sistemi Input Engine'den bağımsız tutulmalıdır.
- Kombinasyonlar tekli tuşlardan önce değerlendirilmelidir.
- FN tuşu kullanıcı tarafından değiştirilebilir olmalıdır.
- Uzun basma süresi yapılandırılabilir olmalıdır.
- Gereksiz karmaşık bir input sistemi oluşturulmamalıdır.

## Kabul Kriterleri

- [ ] Profil modeli oluşturuldu.
- [ ] Aktif profil desteği oluşturuldu.
- [ ] Tuş → eylem eşleştirmesi destekleniyor.
- [ ] FN tuşu yapılandırılabiliyor.
- [ ] Tuş kombinasyonları destekleniyor.
- [ ] Kombinasyonlar tekli tuşlardan önce değerlendiriliyor.
- [ ] Tuş basma/bırakma durumu takip ediliyor.
- [ ] Uzun basma süresi yapılandırılabiliyor.
- [ ] Proje derleniyor.
