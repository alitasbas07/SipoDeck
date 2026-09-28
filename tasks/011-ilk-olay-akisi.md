# Task 011 — İlk Olay Akışı

## Amaç

SipoDeck içerisindeki temel mimarinin uçtan uca çalıştığını doğrulamak.

## Akış

Test amacıyla oluşturulan bir cihaz olayı şu akıştan geçmelidir:

```text
Test Olayı
    ↓
Device Event
    ↓
Input Engine
    ↓
Aktif Profil
    ↓
Tuş Eşleştirmesi
```

Bu task kapsamında gerçek ESP32 bağlantısı kullanılmayacaktır.

## Test Olayı

Sistem içerisinde test amacıyla bir tuş basma ve bırakma olayı oluşturulabilmelidir.

Örneğin:

```text
Button 1 → pressed
Button 1 → released
```

## Input Engine

Input Engine gelen cihaz olayını almalı ve aktif profile ile değerlendirmelidir.

Tanımlı bir tuş eşleştirmesi bulunuyorsa ilgili eylem zincirine aktarılmalıdır.

## Test Amaçlı Eylem

Gerçek sistem eylemlerine henüz ihtiyaç yoktur.

Akışın çalıştığını doğrulamak için basit bir test eylemi kullanılabilir.

Örneğin:

```text
Button 1
    ↓
Test Action
    ↓
"Button 1 çalıştı" çıktısı
```

## Kurallar

- Gerçek ESP32 bağlantısı kullanılmayacaktır.
- Wi-Fi veya USB bağlantısı bu task kapsamında kullanılmayacaktır.
- Test kodu mimarinin içine kalıcı bağımlılık oluşturmamalıdır.
- UI geliştirilmesine başlanmayacaktır.
- Amaç yalnızca olay akışını doğrulamaktır.
- Mevcut mimari gereksiz şekilde değiştirilmemelidir.

## Kabul Kriterleri

- [x] Test cihaz olayı oluşturulabiliyor.
- [x] `pressed` olayı Input Engine'e ulaşıyor.
- [x] `released` olayı Input Engine'e ulaşıyor.
- [x] Aktif profil değerlendiriliyor.
- [x] Tuş eşleştirmesi bulunabiliyor.
- [x] Eylem zincirine aktarım gerçekleşiyor.
- [x] Test eylemi çalışıyor.
- [x] Proje derleniyor.
