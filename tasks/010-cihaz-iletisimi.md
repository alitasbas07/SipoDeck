# Task 010 — Cihaz İletişimi

## Amaç

ESP32 makro deck ile SipoDeck Windows uygulaması arasında gerçek veri iletişimini sağlamak.

## Kapsam

Bu task kapsamında:

- ESP32 üzerinden tuş olayı gönderilecek.
- Windows uygulaması bu olayı alacak.
- JSON mesajı çözümlenecek.
- Cihaz olayı oluşturulacak.
- Olay Input Engine'e aktarılabilecek.

## Wi-Fi

Wi-Fi bağlantısı üzerinden cihaz ile masaüstü uygulaması arasında iletişim kurulmalıdır.

IP adresi ve port kullanıcı ayarlarından alınmalıdır.

Port veya IP adresi kod içerisine sabit yazılmamalıdır.

## USB

USB Serial üzerinden cihaz iletişimi desteklenmelidir.

Serial port ve baud rate kullanıcı ayarlarından alınmalıdır.

## JSON Mesajları

Tuş basma:

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

## Cihaz El Sıkışması

Bağlantı kurulduğunda cihaz kendisini tanıtabilmelidir.

Masaüstü uygulaması aşağıdaki bilgileri alabilmelidir:

- `device_id`
- `device_name`
- `firmware_version`
- `protocol_version`

## Hatalı Mesajlar

Geçersiz veya desteklenmeyen mesajlar uygulamanın çökmesine neden olmamalıdır.

Hatalı mesajlar güvenli şekilde reddedilmelidir.

## Bağlantı Kesilmesi

Bağlantı kesildiğinde cihaz durumu güncellenmelidir.

Yeniden bağlanma mekanizması `008 — Uygulama Yaşam Döngüsü` kapsamında oluşturulan yapıyla uyumlu çalışmalıdır.

## Kurallar

- Wi-Fi ve USB aynı protokolü kullanmalıdır.
- Transport katmanı mesajın anlamını belirlememelidir.
- JSON çözümleme ayrı bir katmanda yapılmalıdır.
- Geçersiz mesajlar uygulamayı çökertmemelidir.
- IP, port, Serial port ve baud rate hard-code edilmemelidir.
- ESP32 tarafında profil veya eylem mantığı bulunmamalıdır.
- Gereksiz iletişim protokolü karmaşıklığı oluşturulmamalıdır.

## Kabul Kriterleri

- [ ] ESP32'den JSON tuş olayı alınabiliyor. (Ertelendi: donanım testi yapılmadı.)
- [x] Wi-Fi üzerinden veri alınabiliyor. (Yerel WebSocket test sunucusuyla doğrulandı.)
- [ ] USB Serial üzerinden veri alınabiliyor. (Kod hazır; seri port testi ertelendi.)
- [x] JSON mesajları cihaz olaylarına dönüştürülüyor.
- [x] Cihaz bilgileri alınabiliyor.
- [x] Geçersiz mesajlar güvenli şekilde reddediliyor.
- [x] Bağlantı kesilmesi algılanıyor.
- [x] Proje derleniyor.
- [ ] Gerçek cihaz ile temel tuş basma/bırakma testi yapılıyor. (Ertelendi: fiziksel test kullanıcı tarafından sonraya bırakıldı.)
