# Task 008 — Uygulama Yaşam Döngüsü

## Amaç

SipoDeck masaüstü uygulamasının Windows üzerinde arka planda çalışabilmesini ve uygulama yaşam döngüsünü düzgün yönetmesini sağlamak.

## Sistem Tepsisi

Uygulama ana penceresi kapatıldığında uygulama tamamen kapanmamalıdır.

Uygulama sistem tepsisinde çalışmaya devam edebilmelidir.

Temel işlemler:

- Uygulamayı aç
- Ana pencereyi göster
- Uygulamayı gizle
- Uygulamayı tamamen kapat

## Windows Başlangıcı

Uygulamanın Windows açılışında otomatik çalıştırılması kullanıcı tarafından açılıp kapatılabilmelidir.

Bu ayar `007 — Ayar Sistemi` üzerinden yönetilmelidir.

## Uygulama Kapanışı

Kullanıcı uygulamayı tamamen kapattığında:

- Aktif bağlantılar düzgün şekilde kapatılmalıdır.
- Çalışan kaynaklar serbest bırakılmalıdır.
- Uygulama arka planda kalmamalıdır.

Ana pencerenin kapatılması ile uygulamanın tamamen kapatılması birbirinden ayrılmalıdır.

## Otomatik Yeniden Bağlanma

Cihaz bağlantısı kesildiğinde uygulama yeniden bağlanmayı deneyebilmelidir.

Yeniden bağlanma:

- Kullanıcı tarafından açılıp kapatılabilmelidir.
- Başarısız bağlantılarda uygulamayı kilitlememelidir.
- Uygulamanın geri kalan işleyişini engellememelidir.

Yeniden bağlanma stratejisinin detayları bu task kapsamında mümkün olduğunca basit tutulmalıdır.

## Uygulama Durumu

Uygulama temel durumları takip edebilmelidir:

```text
Başlatılıyor
    ↓
Çalışıyor
    ↓
Arka planda
    ↓
Kapanıyor
```

Bağlantı durumu cihaz durumundan ayrı tutulmalıdır.

## Kurallar

- Ana pencerenin kapanması uygulamayı tamamen sonlandırmamalıdır.
- Sistem tepsisi desteği oluşturulmalıdır.
- Tamamen kapatma için ayrı bir işlem bulunmalıdır.
- Windows başlangıcı kullanıcı tarafından kontrol edilebilmelidir.
- Yeniden bağlanma uygulamanın ana iş parçacığını engellememelidir.
- Uygulama kapanırken kaynaklar düzgün şekilde serbest bırakılmalıdır.
- Gereksiz servis veya arka plan sistemi oluşturulmamalıdır.

## Kabul Kriterleri

- [ ] Sistem tepsisi desteği oluşturuldu.
- [ ] Ana pencere gizlenebiliyor.
- [ ] Uygulama sistem tepsisinden tekrar açılabiliyor.
- [ ] Uygulama tamamen kapatılabiliyor.
- [ ] Windows başlangıcı ayarı destekleniyor.
- [ ] Otomatik yeniden bağlanma desteği oluşturuldu.
- [ ] Uygulama kapanırken bağlantılar düzgün kapatılıyor.
- [ ] Proje derleniyor.
