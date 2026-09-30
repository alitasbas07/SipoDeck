# SipoDeck — Arayüz Tasarım Brief'i

Bu doküman SipoDeck Windows masaüstü uygulamasının ürün arayüzü için tasarım yönünü tanımlar. Tüm arayüz görevleri (015–019 ve sonrası) bu brief'e uymalıdır.

## Ürün

SipoDeck; kullanıcının fiziksel ESP32 tabanlı kontrol cihazını bilgisayarına bağladığı, cihaz tuşlarına işlemler atadığı, profiller oluşturduğu, sistem bilgilerini görüntülediği ve hızlı işlemler çalıştırdığı bir masaüstü uygulamasıdır.

Arayüz Runtime ile yalnızca Runtime sözleşmesi ve yönetim API'si (Task 014) üzerinden konuşur. Ekranlarda gösterilen değerler gerçek Runtime verisinden gelir; bir veri kaynağı henüz yoksa ilgili alan gösterilmez veya boş durum (empty state) kullanılır, uydurma veri gösterilmez.

## Temel Tasarım Yönü

Arayüz tamamen siyah, beyaz ve gri tonlarından oluşur.

| Amaç | Renk |
|---|---|
| Ana arka plan | `#050505` |
| İkincil yüzey | `#0D0D0D` |
| Yükseltilmiş yüzey | `#141414` |
| İnce sınırlar | `#272727` |
| Ana metin | `#FFFFFF` |
| İkincil metin | `#8C8C8C` |
| Soluk metin | `#5F5F5F` |
| Seçili alan | `#FFFFFF` |
| Seçili alan üzerindeki metin | `#050505` |

Mavi, mor, yeşil, kırmızı, neon renk, renkli gradient ve RGB ışık kullanılmaz.

Tasarım çok sade, karanlık, premium ve teknik görünür. Gaming arayüzü, klasik SaaS dashboard'u veya kalabalık yönetim paneli gibi görünmez.

Karakter: monokrom, minimal, sessiz ve kontrollü, premium masaüstü yazılımı, bol boşluk, ince çizgiler, net tipografi, az sayıda kart, gereksiz dekorasyon yok, gereksiz grafik ve istatistik kartı yok.

Tipografi: Inter, Geist veya benzer sade bir sans-serif (sistemde Segoe UI Variable geri dönüşü kabul edilir). Sistem değerleri, yüzdeler ve cihaz kimliklerinde isteğe bağlı olarak JetBrains Mono (geri dönüş: Cascadia Mono / Consolas).

Pencere: ana hedef 1440×900, minimum 1280×720. Özel pencere başlığı kullanılır; Windows küçültme, büyütme ve kapatma kontrolleri tasarıma uygun şekilde sağ üstte gösterilir.

## Açılış Ekranı

Uygulama açıldığında SipoDeck cihazını güçlü şekilde tanıtan sinematik ve çok sade bir açılış ekranı gösterilir.

Bu ekranda gerçek SipoDeck cihazına ait hazırlanmış bir video kullanılır. Video dosyası henüz yoksa gerçekçi bir yer tutucu gösterilir; video kaynağı tek bir yerden kolayca değiştirilebilir olmalıdır.

Video özellikleri:

- Ekranın büyük bölümünü kaplayan, siyah arka planlı cihaz videosu.
- Cihaz karanlığın içinden yavaşça görünür hale gelir.
- Otomatik oynar, sessizdir ve döngüseldir.
- Üzerinde ağır metin veya renkli efekt yoktur.
- Alt tarafta çok hafif siyah geçiş vardır.
- Cihazın gerçek formu tasarımın ana görsel kimliğidir.

Metin:

- Logo: "SipoDeck"
- Başlık: "Kontrol, parmaklarının ucunda."
- Açıklama: "Cihazını bağla, tuşlarını düzenle ve çalışma alanını kontrol et."
- Ana buton: "Başla"
- İkincil buton: "Cihaz bağla"

İlk kurulum tamamlandıysa açılış videosu kısa süre gösterilir ve kullanıcı ana ekrana geçebilir. "Bir daha gösterme" seçeneği bulunur, görsel olarak geri planda tutulur.

## Ana Uygulama Yapısı

Sol tarafta sade ve dar bir navigasyon alanı bulunur.

Menü: Ana Sayfa, Deck, Profiller, Eylemler, Cihazlar, Bildirimler, Ayarlar.

Navigasyonda yalnızca ince beyaz ikonlar ve kısa metinler kullanılır. Aktif menü öğesi renkli vurgu yerine beyaz zemin ve siyah ikon/metin ile gösterilir.

Sol menünün altında: cihaz bağlantı durumu, aktif profil, küçük ayarlar kısayolu.

## Ana Sayfa

Ana sayfa klasik dashboard gibi görünmez. Büyük sayı kartları veya gereksiz grafikler kullanılmaz. Ana odak fiziksel SipoDeck cihazıdır.

Yerleşim:

- Üst bölümde sade karşılama metni.
- Merkezde cihazın görüntüsü veya üç boyutlu sunumu.
- Cihazın yanında: bağlantı durumu, aktif profil, son çalıştırılan işlem.
- "Deck'i düzenle" ve "Sistem bilgileri" butonları.

Gösterilen bilgiler: cihaz adı (ör. "SipoDeck One"), durum ("Bağlı"), bağlantı türü ("Wi-Fi" / "USB"), aktif profil adı, son işlem (ör. "Mikrofon kapatıldı"). IP, port ve COM port bilgisi arayüzde gösterilmez.

Durumlar renk kullanılmadan ifade edilir:

- Bağlı: dolu beyaz nokta
- Bekliyor: içi boş beyaz nokta
- Bağlantı yok: çapraz çizgili nokta
- İşlem sürüyor: dönen ince beyaz halka

## Deck Düzenleme Ekranı

Üç bölümlü sade çalışma alanı:

- Solda profil listesi.
- Ortada fiziksel SipoDeck tuşlarının canlı görünümü.
- Sağda seçilen tuşun özellikleri.

Ortada 12 tuşlu bir deck gösterilir. Tuşlarda sade monokrom ikonlar ve kısa başlıklar kullanılır.

Seçilen tuşun özellikleri: tuş adı, simge, basma eylemi, uzun basma eylemi, eylem zinciri, "Eylem ekle", "Test et", "Kaydet".

Eylem zinciri sade dikey satırlar olarak gösterilir (ör. Programı aç → 500 ms bekle → Klavye kısayolunu çalıştır → Bildirim göster). Aşırı büyük kartlar veya karmaşık bağlantı diyagramları kullanılmaz.

## Sistem Bilgileri Bildirim Çubuğu

Panel ekranın üst orta bölümünden açılır; normal bir pencere veya klasik Windows bildirimi gibi değil, ekranın üst kenarının doğal bir uzantısı gibi görünür.

Yapı:

- Ekranın üst orta bölümüne bağlıdır; kapalıyken yalnızca küçük, ince bir tutamaç görünür.
- Açıldığında üstten aşağı yaklaşık 110–150 px uzanır; genişlik yaklaşık 440–560 px.
- Arka plan saf siyaha yakındır; alt köşeler geniş ve yumuşak yuvarlatılmıştır; üst kısım ekran sınırıyla birleşir.
- Organik, kesintisiz yüzey hissi korunur; dışarıdan eklenmiş sıradan bir kart gibi görünmez.
- Çok hafif bir gölge kullanılabilir. Renkli durum göstergesi yoktur.

İçerik (yatay): CPU (merkezde "C", altında yüzde), RAM (merkezde "M"), DİSK (merkezde "D"), SICAKLIK (merkezde sıcaklık ikonu, altında "°"). Her biri ince dairesel beyaz göstergedir: boş bölüm koyu gri, kullanım oranı beyaz. Kritik değerlerde renk kullanılmaz; kritik durum daha kalın halka, ünlem ikonu veya yanıp sönmeyen beyaz işaretle belirtilir.

Küçük başlık: "Sistem Durumu". Sağda iki sade kontrol: Yenile, Kapat.

Etkileşim:

- "Sistem bilgileri" butonuna veya cihazdaki atanmış tuşa basıldığında tutamaç belirginleşir, panel yumuşak şekilde yukarıdan aşağı iner, göstergeler tek seferlik kısa bir dolum animasyonu yapar.
- 6–8 saniye sonra otomatik kapanabilir; üzerine gelince açık kalır; yukarı sürüklenerek ve Escape ile kapanır; tekrar tetiklenince açılıp kapanır.
- Animasyon süresi 180–260 ms. Bounce, neon parlama veya sürekli hareket yoktur.

## Normal İşlem Bildirimleri

Sistem bilgileri panelinden ayrı, küçük işlem bildirimleri:

- Örnek: "Mikrofon kapatıldı" / "Çalışma profili · Tuş 3".
- Ekranın üst sağ bölümünde gösterilir; siyah zeminli, ince gri sınırlıdır.
- Beyaz ikon ve başlık, gri açıklama.
- Başarı için tik, hata için çarpı, uyarı için ünlem (renkli şerit yok).
- 3–4 saniye sonra yumuşak şekilde kaybolur.
- Hata örneği: başlık "Discord açılamadı", açıklama "Uygulama yolu bulunamadı.", buton "Eylemi düzenle".

## Etkileşimler

Açılıştan ana uygulamaya geçiş; sol menüde ekran değiştirme; deck üzerinde tuş seçme; tuşa eylem atama; profil değiştirme; sistem bilgileri panelini üstten açma/kapatma; sistem değerlerini yenileme; işlem bildirimi gösterme; cihaz bağlantısının kesilmesi ve yeniden bağlanması; bir eylemi test etme.

## Tasarım Kısıtları

Kesinlikle kullanılmaz: renkli gradientler, mavi/mor vurgu, RGB ışıklar, yoğun glassmorphism, çok sayıda dashboard kartı, büyük renkli grafikler, gereksiz istatistikler, aşırı yuvarlak mobil uygulama görünümü, emoji ikonları, lorem ipsum, devasa başlıklar, sürekli çalışan dikkat dağıtıcı animasyonlar.

## Kalite Ölçütleri

Açılış ekranı, ana sayfa, deck düzenleme ekranı, üstten açılan sistem bilgileri paneli ve işlem bildirimleri aynı tasarım sisteminin parçaları gibi görünmelidir. Öncelik sırası: (1) video kullanılan açılış ekranı, (2) fiziksel cihazı merkeze alan ana sayfa, (3) üst orta bölümden açılan sistem bilgileri paneli. Gerçekçi Türkçe içerik kullanılır; sonuç yüksek kaliteli, sade, premium, monokrom bir Windows masaüstü uygulaması olmalıdır.
