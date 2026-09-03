# Task 002 — İletişim Protokolü

## Amaç

ESP32 makro deck ile SipoDeck Windows uygulaması arasındaki iletişim için temel protokolü tanımlamak.

Bu task kapsamında gerçek Wi-Fi veya USB bağlantısı implement edilmeyecektir.

## Protokol

Cihaz ve masaüstü uygulaması arasındaki mesajlar JSON formatında olacaktır.

Her mesajda protokol sürümü bulunmalıdır.

Örnek tuş basma olayı:

```json
{
  "version": 1,
  "device_id": "ornek-cihaz",
  "type": "button",
  "button": 1,
  "state": "pressed",
  "timestamp": 123456
}
```

Tuş bırakma:

```json
{
  "version": 1,
  "device_id": "ornek-cihaz",
  "type": "button",
  "button": 1,
  "state": "released",
  "timestamp": 123789
}
```

## Cihaz Bilgileri

Masaüstü uygulaması cihazdan aşağıdaki bilgileri alabilecek şekilde tasarlanmalıdır:

- Cihaz kimliği
- Cihaz adı
- Firmware sürümü
- Protokol sürümü

## Durumlar

Temel tuş durumları:

- `pressed`
- `released`

Gelecekte yeni olay türleri eklenebilmelidir.

Örneğin:

- Cihaz bağlandı
- Cihaz ayrıldı
- Encoder hareketi
- Encoder butonu
- Cihaz bilgisi
- Hata

## Kurallar

- Protokol sürümü mesajlarda bulunmalıdır.
- Cihaz kimliği bulunmalıdır.
- Tuş basma ve bırakma olayları ayrı mesajlar olmalıdır.
- ESP32, tuşun hangi işlemi yapacağına karar vermemelidir.
- Protokol mümkün olduğunca basit tutulmalıdır.
- Gelecekte yeni olay türleri eklenmesine uygun olmalıdır.
- Wi-Fi ve USB iletişimi protokolden bağımsız olmalıdır.
- Transport katmanı JSON protokolünün detaylarını değiştirmemelidir.

## Kabul Kriterleri

- [ ] Protokol veri modelleri oluşturuldu.
- [ ] Tuş basma olayı destekleniyor.
- [ ] Tuş bırakma olayı destekleniyor.
- [ ] Cihaz bilgileri için model oluşturuldu.
- [ ] Protokol sürümü tanımlandı.
- [ ] Wi-Fi veya USB bağlantısı implement edilmedi.
- [ ] Proje derleniyor.
