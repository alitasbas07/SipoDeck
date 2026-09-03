# Task 009 — Veri ve Profil Saklama

## Amaç

SipoDeck kullanıcı ayarlarının, profillerin ve profil içerisindeki tuş yapılandırmalarının kalıcı olarak saklanmasını sağlamak.

## Profil Yapısı

Her profil:

- Benzersiz bir kimliğe sahip olmalıdır.
- Kullanıcı tarafından isimlendirilebilmelidir.
- Aktif veya pasif durumda olabilir.
- Tuş atamalarına sahip olabilir.

Kullanıcı istediği kadar profil oluşturabilmelidir.

## Aktif Profil

Aynı anda yalnızca bir profil aktif olabilir.

Pasif profiller saklanmaya devam eder ancak tuş olaylarını işlemez.

Örnek:

```text
Profil 1 → Aktif
Profil 2 → Pasif
Profil 3 → Pasif
Profil 4 → Pasif
```

Kullanıcı istediği zaman başka bir profili aktif hale getirebilmelidir.

## Tuş Yapılandırması

Profil içerisindeki her fiziksel tuş için farklı yapılandırmalar saklanabilmelidir.

Örneğin:

```text
Profil 1
 ├── Button 1 → URL aç
 ├── Button 2 → Program çalıştır
 └── Button 3 → Bildirim göster
```

Aynı tuş başka profilde farklı bir göreve sahip olabilir.

## FN Yapılandırması

FN tuşu profil sisteminden bağımsız olarak saklanmalıdır.

Kullanıcı FN olarak kullanacağı fiziksel tuşu değiştirebilmelidir.

Profil değiştirme kombinasyonları da yapılandırılabilir olmalıdır.

## Veri Formatı

Verilerin saklanacağı format ve klasör yapısı uygulamanın ihtiyaçlarına uygun şekilde belirlenmelidir.

Veri yapısı gelecekte yeni alanlar eklenebilecek şekilde tasarlanmalıdır.

Eski yapıların yeni sürümlerde mümkün olduğunca korunabilmesi hedeflenmelidir.

## Varsayılan Profil

Uygulama ilk çalıştırıldığında kullanıcıya kullanılabilir bir varsayılan profil sunulmalıdır.

Varsayılan profil kullanıcı tarafından değiştirilebilir veya silinebilir.

## Veri Güvenliği

Kullanıcıya ait hassas bilgiler gereksiz şekilde düz metin olarak saklanmamalıdır.

Log dosyalarına profil veya kullanıcı ayarlarının tamamı yazılmamalıdır.

## Kurallar

- Profil sayısı sınırlandırılmamalıdır.
- Aktif ve pasif profil durumu desteklenmelidir.
- Profil verileri uygulama ayarlarından ayrı tutulmalıdır.
- Profil kimlikleri benzersiz olmalıdır.
- Veri formatı ileride genişletilebilir olmalıdır.
- Gereksiz veritabanı kullanılmamalıdır.
- Profil değiştirme sırasında uygulama yeniden başlatılmak zorunda kalmamalıdır.

## Kabul Kriterleri

- [ ] Profil modeli oluşturuldu.
- [ ] Profil oluşturma ve saklama destekleniyor.
- [ ] Profil isimlendirme destekleniyor.
- [ ] Aktif/pasif profil durumu destekleniyor.
- [ ] Profil içerisindeki tuş yapılandırmaları saklanıyor.
- [ ] FN tuşu yapılandırması saklanıyor.
- [ ] Profil değiştirme kombinasyonları saklanıyor.
- [ ] Veriler uygulama yeniden başlatıldığında korunuyor.
- [ ] Varsayılan profil oluşturuluyor.
- [ ] Proje derleniyor.
