# Task 001 — Proje Temel Mimarisi

## Amaç

SipoDeck Windows uygulamasının ileride geliştirilecek özelliklere uygun temel proje yapısını oluşturmak.

Bu task kapsamında özellik geliştirilmemelidir. Amaç yalnızca temel mimari iskeleti oluşturmaktır.

## Teknoloji

- C#
- .NET 10
- WPF
- Windows

## Temel Mimari

Uygulamanın temel veri akışı şu yapıya uygun olmalıdır:

```text
Transport
    ↓
Device
    ↓
Device Event
    ↓
Input Engine
    ↓
Profile
    ↓
Action
```

## Oluşturulacak Temel Yapılar

Aşağıdaki kavramlar için temel arayüz veya modeller oluşturulmalıdır:

- `ITransport`
- `IDevice`
- `IDeviceEvent`
- `IInputEngine`
- `IAction`

Gerekli görülen yardımcı modeller oluşturulabilir.

## Kurallar

- Gereksiz abstraction oluşturulmayacak.
- Henüz kullanılmayacak özellikler implement edilmeyecek.
- Plugin sistemi bu task kapsamında implement edilmeyecek.
- ESP32 iletişimi bu task kapsamında implement edilmeyecek.
- UI bu task kapsamında geliştirilmeyecek.
- Mevcut WPF başlangıç ekranı korunabilir.
- Mimari ileride genişletilebilir olacak ancak gereksiz şekilde karmaşıklaştırılmayacak.
- Core kod ile UI kodu mümkün olduğunca birbirinden ayrılacak.

## Beklenen Sonuç

Proje, ilerleyen tasklerde;

```text
ESP32
  ↓
Transport
  ↓
Device Event
  ↓
Input Engine
  ↓
Profile
  ↓
Action
```

akışının üzerine özellik eklenebilecek durumda olmalıdır.

## Kabul Kriterleri

- [ ] Proje derleniyor.
- [ ] WPF uygulaması çalışıyor.
- [ ] Temel mimari arayüzleri oluşturuldu.
- [ ] UI ile core yapı birbirine gereksiz şekilde bağımlı değil.
- [ ] Gereksiz özellik veya servis eklenmedi.
- [ ] Mevcut proje yapısı bozulmadı.
