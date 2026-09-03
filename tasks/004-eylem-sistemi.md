# Task 004 — Eylem Sistemi

## Amaç

Profilde tanımlanan tuş veya tuş kombinasyonlarının bir veya birden fazla eylem çalıştırabilmesini sağlamak.

## Eylem Zinciri

Bir tuşa birden fazla eylem sıralı olarak atanabilmelidir.

Örnek:

```text id="2qk0az"
Tuş 1
 ├── URL aç
 ├── 2 saniye bekle
 ├── Bildirim göster
 └── Program çalıştır
```

Eylemler tanımlandıkları sırayla çalıştırılmalıdır.

## Temel Eylemler

Çekirdek sistemde aşağıdaki eylemler desteklenmelidir:

- Program çalıştırma
- URL açma
- Bekleme / gecikme
- Bildirim gösterme
- Ses kontrolü
- Medya kontrolü

Eylem sistemi gelecekte yeni eylemler eklenmesine uygun olmalıdır.

## Eylem Arayüzü

Her eylem ortak bir arayüz üzerinden çalışmalıdır.

Örnek:

```text id="g7z0bn"
IAction
   ↓
RunProgramAction
OpenUrlAction
WaitAction
NotificationAction
MediaAction
...
```

## Eylem Zinciri Çalıştırma

Bir tuşa birden fazla eylem atanmışsa:

1. İlk eylem çalıştırılır.
2. Tamamlanması beklenir.
3. Sıradaki eylem çalıştırılır.
4. Zincir tamamlanana kadar devam edilir.

## Hata Yönetimi

Bir eylem başarısız olduğunda sistemin tamamen çökmesi engellenmelidir.

Eylemin başarısız olması durumunda zincirin:

- Durdurulması
- Devam etmesi

gibi davranışlar ileride yapılandırılabilir hale getirilebilir.

Bu task kapsamında karmaşık hata yönetimi oluşturulmasına gerek yoktur.

## Uzun Basma ve Eylemler

Input Engine tarafından belirlenen tuş durumuna göre farklı eylem zincirleri çalıştırılabilmelidir.

Örneğin:

```text id="y4f8mq"
Normal basma
    ↓
Eylem zinciri A

Uzun basma
    ↓
Eylem zinciri B
```

## Kurallar

- Eylemler `IAction` üzerinden genişletilebilir olmalıdır.
- Eylemler sıralı çalışmalıdır.
- Eylem sistemi Input Engine'e doğrudan bağımlı olmamalıdır.
- Eylemler UI koduna bağımlı olmamalıdır.
- Çekirdek eylemler ile ileride oluşturulacak eklenti eylemleri birbirinden ayrılabilmelidir.
- Gereksiz bir workflow/automation motoru oluşturulmayacaktır.
- Eylem sistemi mümkün olduğunca basit tutulacaktır.

## Kabul Kriterleri

- [ ] `IAction` oluşturuldu.
- [ ] Eylem zinciri modeli oluşturuldu.
- [ ] Eylemler sıralı çalıştırılabiliyor.
- [ ] Program çalıştırma eylemi oluşturuldu.
- [ ] URL açma eylemi oluşturuldu.
- [ ] Bekleme eylemi oluşturuldu.
- [ ] Bildirim eylemi için temel yapı oluşturuldu.
- [ ] Sistem/medya eylemleri için temel yapı oluşturuldu.
- [ ] Eylemler UI'dan bağımsız çalışıyor.
- [ ] Proje derleniyor.
