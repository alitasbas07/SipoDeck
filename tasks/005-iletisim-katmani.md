# Task 005 — İletişim Katmanı

## Amaç

SipoDeck'in cihazlarla farklı bağlantı yöntemleri üzerinden iletişim kurabilmesini sağlayacak temel transport yapısını oluşturmak.

## Desteklenecek Bağlantılar

İlk aşamada:

- Wi-Fi
- USB Serial

desteklenmelidir.

Mimari ileride Bluetooth gibi farklı bağlantı türlerinin eklenmesine uygun olmalıdır.

## Transport Yapısı

Her bağlantı yöntemi ortak bir `ITransport` arayüzü üzerinden çalışmalıdır.

```text
ITransport
├── WiFiTransport
└── SerialTransport
```

Üst katman, kullanılan bağlantı yönteminin detaylarını bilmemelidir.

## Bağlantı Ayarları

Bağlantı için gerekli bilgiler yapılandırılabilir olmalıdır.

Örneğin:

- Bağlantı türü
- IP adresi / cihaz adresi
- Port
- Serial port
- Baud rate

Port gibi değerler kod içerisinde sabit olarak tutulmamalıdır.

## Bağlantı Durumu

Transport aşağıdaki durumları bildirebilmelidir:

- Bağlanıyor
- Bağlandı
- Bağlantı kesildi
- Hata

## Yeniden Bağlanma

Bağlantı kesildiğinde masaüstü uygulamasının yeniden bağlanabilmesine uygun bir yapı oluşturulmalıdır.

Otomatik yeniden bağlanmanın detayları daha sonraki tasklerde ele alınabilir.

## Veri İletimi

Transport katmanı:

- Veri gönderebilmeli
- Veri alabilmeli
- Bağlantıyı açabilmeli
- Bağlantıyı kapatabilmeli

Protokolün JSON yapısını yorumlamak transport katmanının sorumluluğu değildir.

```text
Transport
    ↓
Ham veri
    ↓
Protokol
    ↓
Cihaz olayı
```

## Kurallar

- Wi-Fi ve Serial implementasyonları birbirinden bağımsız olmalıdır.
- Üst katman `ITransport` üzerinden çalışmalıdır.
- Transport katmanı profil veya eylem sistemini bilmemelidir.
- IP, port ve Serial ayarları hard-code edilmemelidir.
- Bluetooth için şimdilik implementasyon yapılmayacaktır.
- Gereksiz bağlantı yönetimi altyapısı oluşturulmayacaktır.

## Kabul Kriterleri

- [ ] `ITransport` oluşturuldu.
- [ ] Wi-Fi transport temel yapısı oluşturuldu.
- [ ] Serial transport temel yapısı oluşturuldu.
- [ ] Bağlantı durumları tanımlandı.
- [ ] Yapılandırılabilir bağlantı ayarları oluşturuldu.
- [ ] Transport ile üst katman arasındaki bağımlılık azaltıldı.
- [ ] Bluetooth implementasyonu yapılmadı.
- [ ] Proje derleniyor.
