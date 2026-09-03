# Task 006 — Cihaz Yönetimi

## Amaç

SipoDeck'in bağlı ESP32 cihazlarını tanımasını, yönetmesini ve cihaz durumunu takip etmesini sağlayacak temel yapıyı oluşturmak.

## Cihaz Bilgileri

Her cihaz aşağıdaki temel bilgilere sahip olmalıdır:

- Cihaz kimliği
- Cihaz adı
- Firmware sürümü
- Protokol sürümü
- Bağlantı türü
- Bağlantı durumu

## Cihaz Durumu

Cihazın temel bağlantı durumları takip edilebilmelidir:

- Bağlanıyor
- Bağlandı
- Bağlantı kesildi
- Hata

## Cihaz Kimliği

Her cihaz benzersiz bir `device_id` ile tanımlanmalıdır.

Cihaz adı kullanıcı tarafından değiştirilebilir olabilir ancak `device_id` değişmemelidir.

## Cihaz ve Transport İlişkisi

Bir cihaz bir transport üzerinden haberleşmelidir.

```text
Device
   ↓
ITransport
   ↓
Wi-Fi / USB
```

`Device` doğrudan Wi-Fi veya Serial implementasyonuna bağımlı olmamalıdır.

## Cihaz Keşfi

Sistem ileride otomatik cihaz keşfini destekleyebilecek şekilde tasarlanmalıdır.

Bu task kapsamında gelişmiş otomatik keşif sistemi yapılmasına gerek yoktur.

Temel olarak manuel cihaz bağlantısını destekleyecek yapı yeterlidir.

## Çoklu Cihaz

MVP arayüzünde tek cihaz gösterilebilir.

Ancak cihaz yönetimi iç yapısı ileride birden fazla cihaz desteklenebilecek şekilde tasarlanmalıdır.

## Kurallar

- Cihaz kimliği benzersiz olmalıdır.
- Cihaz bilgileri transport katmanından bağımsız modellenmelidir.
- Device sınıfı doğrudan Wi-Fi veya Serial koduna bağlanmamalıdır.
- Otomatik keşif bu task kapsamında implement edilmeyecektir.
- Çoklu cihaz için gereksiz UI oluşturulmayacaktır.
- Gereksiz cihaz yönetim sistemi oluşturulmayacaktır.

## Kabul Kriterleri

- [ ] `IDevice` oluşturuldu.
- [ ] Cihaz bilgi modeli oluşturuldu.
- [ ] Cihaz durumları tanımlandı.
- [ ] Device ve Transport ilişkisi oluşturuldu.
- [ ] `device_id` desteği oluşturuldu.
- [ ] Manuel cihaz bağlantısına uygun yapı oluşturuldu.
- [ ] Gelecekte çoklu cihaz desteğine uygun yapı oluşturuldu.
- [ ] Proje derleniyor.
