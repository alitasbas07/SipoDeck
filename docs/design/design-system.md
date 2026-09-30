# SipoDeck — Tasarım Sistemi ve Ekran Spesifikasyonu

Bu doküman `docs/design/ui-design-brief.md`'yi uygulanabilir bir WPF tasarım sistemine çevirir. Brief bağlayıcıdır; bu doküman onun ölçü, kaynak ve davranış düzeyindeki karşılığıdır. Kodlama yapan herkes buradaki anahtar adlarını ve ölçüleri **birebir** kullanır; yeni renk, yeni font boyutu, yeni süre uydurulmaz. Eksik bir ayrıntı gerekiyorsa önce bu dokümana eklenir.

Sürüm: 1.0 (Task 015) · Hedef: .NET 10 WPF, paketsiz özel stil, sistem fontları.

---

## 0. Dosya haritası ve bağlama

| Dosya | İçerik |
|---|---|
| `src/SipoDeck/Themes/Theme.xaml` | **Tek giriş noktası.** Aşağıdakilerin hepsini sırayla birleştirir. |
| `Themes/Colors.xaml` | Renkler (`*Color`) ve fırçalar (`*Brush`), siyah→şeffaf maskeler |
| `Themes/Typography.xaml` | Font aileleri, boyut/satır ölçeği, `Text*` TextBlock stilleri |
| `Themes/Metrics.xaml` | Boşluk, köşe yarıçapı, bileşen ve pencere ölçüleri |
| `Themes/Motion.xaml` | Süreler, easing'ler, KeySpline'lar, ortak Storyboard'lar |
| `Themes/Icons.xaml` | 24×24 ince çizgi ikon Geometry'leri + `Icon16/20/24` sunum stilleri |
| `Themes/Controls.xaml` | Buton, nav, başlık düğmeleri, pencere stili, CheckBox, ScrollBar, ToolTip, ayırıcı, kart, boş durum, sayfa geçişi |
| `Themes/StatusIndicators.xaml` | 4 durum göstergesi (`StatusIndicatorStyle`, `StatusLabelStyle`) |
| `Themes/DeviceVisual.xaml` | 12 tuşlu cihaz çizimi (`DeviceVisualStyle`, `DeviceKeyStyle`) |

`App.xaml` (kodlama ajanı ekler):

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/SipoDeck;component/Themes/Theme.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

- Her alt sözlük kendi bağımlılıklarını da birleştirir (tek başına yüklenebilir, tasarımcıda/testte güvenli). Kaynaklar `StaticResource` ile kullanılır; `DynamicResource` gerekmez (tema değişimi yok).
- Örtük (anahtarsız) stiller yalnızca **ScrollBar** ve **ToolTip** içindir. Button, TextBlock vb. için örtük stil YOKTUR; her kullanımda `Style="{StaticResource ...}"` yazılır.
- Tüm kaynaklar gerçek bir WPF çalıştırmasında yüklendi, durumlar (hover/pressed/disabled/selected, enum DataTrigger, dönen halka, sayfa geçişi) ekran görüntüsüyle doğrulandı; `dotnet build src/SipoDeck/SipoDeck.csproj` hatasız.

---

## 1. İlkeler

1. **Monokrom.** Yalnızca siyah, beyaz ve gri. Renkli vurgu, renkli gradient, RGB ışık, neon yok. Gradient yalnızca siyah → şeffaf siyah maske (`Mask*Brush`).
2. **Seçili = beyaz zemin + siyah içerik.** Bu kural nav, başlıktaki Kapat hover'ı, seçili deck tuşu ve işaretli CheckBox'ta aynıdır.
3. **Sessiz hiyerarşi.** Başlık beyaz, ikincil bilgi `TextSecondary`, etiketler `TextMuted`. Bir ekranda en fazla bir `PrimaryButtonStyle`.
4. **İnce çizgi, bol boşluk.** Ayırıcılar 1 px `BorderSubtle`; kontrol kenarları 1 px `Border`. Kart sayısı minimum.
5. **Hareket az ve anlamlı.** 120–260 ms, ease-out, bounce yok. Sürekli hareket yalnızca "işlem sürüyor" halkası. Yalnızca `Opacity` ve `RenderTransform` canlandırılır.
6. **Uydurma veri yok.** Veri yoksa alan gizlenir veya boş durum gösterilir. IP, port, COM port asla gösterilmez.

---

## 2. Renk tokenları (`Colors.xaml`)

Her renk için iki anahtar vardır: `<Ad>Color` (Color) ve `<Ad>Brush` (SolidColorBrush). Kodda **Brush** kullanılır; Storyboard `ColorAnimation.To` gibi yerlerde **Color**.

| Ad | Hex | Kullanım |
|---|---|---|
| `BgBase` | `#050505` | Pencere, kabuk, sayfa zemini |
| `SurfaceSecondary` | `#0D0D0D` | Kart, boş durum ikon kutusu |
| `SurfaceElevated` | `#141414` | ToolTip, ileride açılır yüzeyler |
| `Border` | `#272727` | Kontrol kenarlığı (Secondary buton, ToolTip) |
| `TextPrimary` | `#FFFFFF` | Ana metin, ikonlar |
| `TextSecondary` | `#8C8C8C` | İkincil metin, açıklamalar |
| `TextMuted` | `#5F5F5F` | Etiket (overline), soluk bilgi |
| `SelectionBg` | `#FFFFFF` | Seçili alan zemini, Primary buton |
| `SelectionFg` | `#050505` | Seçili alan üzerindeki metin/ikon |
| `SurfaceHover` | `#1A1A1A` | (ara ton) hover yüzeyi |
| `SurfacePressed` | `#202020` | (ara ton) basılı yüzey |
| `BorderSubtle` | `#1A1A1A` | Ayırıcı çizgiler, pencere kenarı, kart kenarı |
| `BorderStrong` | `#3D3D3D` | Hover'da kenar, CheckBox kutusu |
| `TextDisabled` | `#3D3D3D` | Devre dışı metin (gerekirse) |
| `PrimaryHover` / `PrimaryPressed` | `#E8E8E8` / `#D2D2D2` | Beyaz dolgu ara tonları (renk animasyonu gerekirse) |
| `ScrollThumb` / `ScrollThumbHover` | `#2E2E2E` / `#505050` | Kaydırma çubuğu |
| `GaugeTrack` | `#262626` | Dairesel göstergenin boş bölümü, meşgul halkanın izi |
| `Black` | `#000000` | Gölge rengi |
| `BgBaseTransparent` → `TransparentBrush` | `#00050505` | Şeffaf |
| `FocusRingBrush` | beyaz, %90 | Odak halkası |
| `DeviceBody` `DeviceBodyEdge` `DeviceKeyFace` `DeviceKeyFaceHover` `DeviceKeySide` `DeviceKeyEdge` `DeviceRimLight` `DeviceKeyHighlight` `StageLight` | `#0E0E0E` `#242424` `#171717` `#1E1E1E` `#080808` `#2B2B2B` `#1FFFFFFF` `#12FFFFFF` `#161616` | Yalnızca cihaz çizimi |

Maskeler (izin verilen tek gradient türü):

| Anahtar | Yön | Kullanım |
|---|---|---|
| `MaskFadeFromTopBrush` | üst %90 koyu → alt şeffaf | Video üstü (başlık düğmeleri okunurluğu), yükseklik 160 |
| `MaskFadeFromBottomBrush` | alt tam koyu → üst şeffaf | Video altı "çok hafif siyah geçiş" + metin zemini, yükseklik 440 |
| `MaskVignetteBrush` | merkez şeffaf → kenar koyu (radyal) | Açılış yer tutucusu kenar karartma |
| `MaskDeviceShadeBrush` | üst şeffaf → alt %55 siyah | Cihaz gövdesinin alt yarısı (derinlik) |

**Hover/pressed tekniği:** Renk animasyonu yerine katman opaklığı. Dolgulu/kenarlıklı düğmelerde `Foreground` renginde bir örtü katmanı 0 → 0.08 (hover) → 0.14 (pressed). Hayalet düğmede/nav'da içerik opaklığı 0.55 (≈ `TextSecondary`) → 1. Böylece yalnızca gri skala ara tonlar oluşur.

---

## 3. Tipografi (`Typography.xaml`)

Font aileleri (dosya eklenmez, sistem fontları):

| Anahtar | Değer |
|---|---|
| `FontSans` | `Inter, Segoe UI Variable Text, Segoe UI` |
| `FontDisplay` | `Inter, Segoe UI Variable Display, Segoe UI` |
| `FontMono` | `JetBrains Mono, Cascadia Mono, Consolas` |
| `FontIcons` | `Segoe Fluent Icons, Segoe MDL2 Assets` (yalnızca başlık düğmeleri) |

Ölçek (DIP; satır yüksekliği `LineStackingStrategy=BlockLineHeight` ile sabit):

| Stil anahtarı | Aile | Boyut / satır | Ağırlık | Renk | Nerede |
|---|---|---|---|---|---|
| `TextDisplay` | Display | 40 / 48 | SemiBold | Primary | Yalnızca açılış başlığı |
| `TextTitle1` | Display | 28 / 36 | SemiBold | Primary | Sayfa başlığı ("Hoş geldin", "Profiller") |
| `TextTitle2` | Display | 20 / 28 | SemiBold | Primary | Cihaz adı, bölüm başlığı |
| `TextTitle3` | Sans | 16 / 24 | SemiBold | Primary | Boş durum başlığı, kart başlığı |
| `TextBodyLarge` | Sans | 16 / 26 | Normal | Secondary | Açılış açıklaması (sarar) |
| `TextBody` | Sans | 14 / 22 | Normal | Primary | Genel metin |
| `TextBodyStrong` | Sans | 14 / 22 | SemiBold | Primary | Vurgulu satır |
| `TextBodySecondary` | Sans | 14 / 22 | Normal | Secondary | Alt başlık, açıklama |
| `TextBodySmall` | Sans | 13 / 20 | Normal | Primary | Nav alt bilgisi değerleri |
| `TextBodySmallStrong` | Sans | 13 / 20 | SemiBold | Primary | Nav altında cihaz adı |
| `TextCaption` | Sans | 12 / 16 | Normal | Secondary | Detay satırı ("Çalışma profili · Tuş 3") |
| `TextCaptionMuted` | Sans | 12 / 16 | Normal | Muted | Nav altı etiketleri ("Aktif profil") |
| `TextOverline` | Sans | 11 / 16 | SemiBold | Muted | BÜYÜK HARF etiket ("AKTİF PROFİL") |
| `TextMono` | Mono | 13 / 20 | Normal | Primary | Sürüm, yüzde, sıcaklık, cihaz kimliği |
| `TextMonoSmall` | Mono | 11 / 16 | Normal | Muted | Küçük teknik değer |
| `TextWordmark` / `TextWordmarkLarge` | Display | 14/20 · 18/24 | SemiBold | Primary | "SipoDeck" logosu (kabuk / açılış) |

Sayısal kaynaklar: `FontSizeDisplay`(40) `FontSizeTitle1`(28) `FontSizeTitle2`(20) `FontSizeTitle3`(16) `FontSizeBodyLarge`(16) `FontSizeBody`(14) `FontSizeBodySmall`(13) `FontSizeCaption`(12) `FontSizeOverline`(11) `FontSizeMono`(13) `FontSizeMonoSmall`(11) `FontSizeIconGlyph`(10) ve karşılık gelen `LineHeight*`.

Kurallar:
- `TextBase` varsayılanı: `NoWrap` + `CharacterEllipsis`. Paragraflar için `TextWrapping="Wrap" TextTrimming="None"` yerel olarak verilir (`TextDisplay`, `TextBodyLarge` zaten sarar).
- WPF'te harf aralığı (tracking) yoktur. Overline etiketleri kaynakta doğrudan BÜYÜK HARF yazılır; Türkçe İ/I'ya dikkat: `AKTİF PROFİL`, `SON İŞLEM`, `CİHAZ`, `DONANIM YAZILIMI`, `SİSTEM DURUMU`. `ToUpper()` kullanılacaksa `CultureInfo("tr-TR")` ile.
- Devasa başlık yok: 40 üst sınırdır.

---

## 4. Boşluk ölçeği (`Metrics.xaml`) — 4/8 tabanlı

| Anahtar | px | Tipik kullanım |
|---|---|---|
| `Space1` | 4 | İkon–metin ince boşluğu, nav öğeleri arası |
| `Space2` | 8 | Gösterge–metin, başlık–alt başlık |
| `Space3` | 12 | Buton–buton, ikon–metin (nav) |
| `Space4` | 16 | Blok içi |
| `Space5` | 20 | Bilgi satırları arası |
| `Space6` | 24 | Nav yatay iç boşluk, kart iç boşluk |
| `Space8` | 32 | Başlık ile içerik arası |
| `Space10` | 40 | Açılış butonları üst boşluğu |
| `Space12` | 48 | — |
| `Space14` | 56 | Sayfa yatay/üst boşluğu |
| `Space16` | 64 | Ana sayfa cihaz–bilgi sütunu arası, açılış kenar boşluğu |
| `Space20` | 80 | — |

Hazır `Thickness`: `PagePadding`=56,56,56,40 · `CardPadding`=24 · `NavListMargin`=12,8,12,0 · `NavFooterPadding`=24,20,16,24 · `ButtonPadding`=20,0 · `ButtonPaddingSmall`=14,0.

---

## 5. Köşe yarıçapları ve bileşen ölçüleri

| Anahtar | Değer | Nerede |
|---|---|---|
| `RadiusXs` | 3 | CheckBox kutusu |
| `RadiusSm` | 6 | ToolTip |
| `RadiusMd` | 8 | Buton, nav öğesi, ikon butonu |
| `RadiusLg` | 12 | Kart, hata bandı |
| `RadiusXl` | 16 | Boş durum ikon kutusu |
| `RadiusFocusRing` | 11 | Odak halkası (8 + 3) |
| `RadiusDeviceKey` | 12 | Cihaz tuşu |
| `RadiusDevice` | 28 | Cihaz gövdesi |
| `RadiusSystemPanel` | 0,0,24,24 | İleride: sistem paneli |
| `RadiusToast` | 12 | İleride: işlem bildirimi |

Ölçüler: `ControlHeight` 40 · `ControlHeightSmall` 32 · `NavItemHeight` 40 · `IconButtonSize` 32 · `TitleBarHeight` 40 · `CaptionButtonWidth` 46 · `NavColumnWidth` 208 (GridLength) · `HomeInfoColumnWidth` 320 (GridLength) · `WindowWidth/Height` 1440/900 · `WindowMinWidth/Height` 1280/720 · `StatusDotSize` 8 · `StatusBoxSize` 12 · `DeviceKeySize` 72 · `DeviceKeyGap` 16.

Aşırı yuvarlak "mobil" görünüm yasak: buton 8, kart 12'yi geçmez (cihaz gövdesi fiziksel nesne olduğu için istisna).

---

## 6. Hareket (`Motion.xaml`)

### 6.1 Süreler (`Duration`)

| Anahtar | Değer | Nerede |
|---|---|---|
| `DurationInstant` | 0 | — |
| `DurationPress` | 100 ms | Basma (scale 0.97) |
| `DurationFast` | 120 ms | Hover, CheckBox, seçimden çıkış |
| `DurationBase` | 180 ms | Seçim (nav aktif), bırakma, durum belirme |
| `DurationMedium` | 220 ms | Sayfa geçişi, bildirim girişi |
| `DurationSlow` | 260 ms | Kabuk girişi, cihaz sönme, splash çıkışı |
| `DurationSpin` | 900 ms/tur | Meşgul halka (doğrusal, sonsuz) |
| `DurationCinematicText` | 600 ms | Yalnızca açılış metinleri |
| `DurationCinematicReveal` | 1400 ms | Yalnızca açılış videosu/yer tutucu belirmesi |
| `DurationPanelOpen` / `DurationPanelClose` / `DurationGaugeFill` | 240 / 180 / 260 ms | İleride: sistem paneli |
| `DurationToastIn` / `DurationToastOut` | 220 / 180 ms | İleride: işlem bildirimi |

Sayısal: `MotionPageOffset` 8 · `MotionSplashTextOffset` 12 · `MotionToastOffset` −8 · `MotionPressScale` 0.97 · `MotionNavPressScale` 0.98 · `SystemPanelAutoCloseSeconds` 7 · `ToastLifetimeSeconds` 3.5 · `ToastErrorLifetimeSeconds` 6.

### 6.2 Easing (WPF karşılıkları)

| Anahtar | WPF | Yaklaştığı eğri | Kullanım |
|---|---|---|---|
| `EaseOut` | `CubicEase EaseOut` | (0.33,1,0.68,1) | Standart giriş/çıkış, durum |
| `EaseOutStrong` | `QuinticEase EaseOut` | ≈ (0.23,1,0.32,1) | Sayfa geçişi, bildirim, bırakma |
| `EaseHover` | `QuadraticEase EaseOut` | CSS `ease` | Hover ve opaklık |
| `EaseInOut` | `QuarticEase EaseInOut` | ≈ (0.77,0,0.175,1) | Ekranda yer değiştirme |
| `EaseDrawer` | `ExponentialEase EaseOut, Exponent=6` | ≈ (0.32,0.72,0,1) | İleride: üstten inen panel |
| `EaseCinematic` | `SineEase EaseOut` | yumuşak | Açılış belirmesi |

Kesin eğri gerekirse `SplineDoubleKeyFrame KeySpline="{StaticResource KeySplineEaseOut|KeySplineEaseInOut|KeySplineDrawer}"`.

Yasak: `BounceEase`, `ElasticEase`, `BackEase`, `EaseIn` modu, 300 ms üstü arayüz animasyonu (açılış hariç), `Width/Height/Margin` animasyonu.

### 6.3 Kurallar (design engineering ilkelerinin WPF uyarlaması)

- **Canlandırmalı mı?** Günde yüzlerce kez görülen şey (klavye kısayolu, tuşa basınca çalışan eylem) canlandırılmaz. Hover: çok kısa (120 ms). Sayfa geçişi, bildirim, panel: standart. Açılış: sinematik olabilir.
- **Easing seçimi:** giren/çıkan → ease-out; ekranda kayan → ease-in-out; hover → `EaseHover`; sürekli (halka) → doğrusal.
- **Kesilebilirlik:** Durum geçişleri `VisualStateManager` + `VisualTransition` ile yapılır; geçiş yarıda kesilirse o anki değerden yeni hedefe gider (CSS transition gibi). Hızlı tetiklenebilen şeylerde `From` verilmez, yalnızca `To` verilir.
- **Asimetri:** Basma 100 ms, bırakma 180 ms `EaseOutStrong`. Seçime giriş 180 ms, çıkış 120 ms.
- **Kaynağa duyarlı (origin-aware):** Ölçeklenen öğe tetikleyicisinden büyür (`RenderTransformOrigin`). Üst panel üst kenardan, bildirim sağ üstten gelir. Merkez ölçek yalnızca modal/açılış içindir.
- **Hiçbir şey sıfırdan doğmaz:** Giriş ölçeği en az 0.96, opaklık 0'dan; `ScaleX=0` yok.
- **Stagger:** Açılış metinlerinde 120 ms; listelerde 30–50 ms. Stagger etkileşimi asla engellemez (opaklık 0 olan buton tıklanabilir).
- **Azaltılmış hareket:** `SystemParameters.ClientAreaAnimation == false` ise açılış sinematiği atlanır (son kare doğrudan gösterilir), sayfa geçişinde kayma kaldırılıp yalnızca opaklık bırakılabilir (isteğe bağlı .cs, §13).

### 6.4 Ortak Storyboard'lar

- `FadeInUpStoryboard` — Opacity 0→1 + `(UIElement.RenderTransform).(TranslateTransform.Y)` 8→0, 220 ms `EaseOutStrong`. Hedef öğe `RenderTransform`'unu **yerel** tanımlamalıdır: `<X.RenderTransform><TranslateTransform /></X.RenderTransform>`.
- `FadeInStoryboard` — Opacity 0→1, 180 ms. `FadeOutStoryboard` — Opacity →0, 180 ms.

```xml
<Grid.Triggers>
    <EventTrigger RoutedEvent="FrameworkElement.Loaded">
        <BeginStoryboard Storyboard="{StaticResource FadeInUpStoryboard}" />
    </EventTrigger>
</Grid.Triggers>
```

---

## 7. İkonlar (`Icons.xaml`)

24×24 ızgara, 1.5 px çizgi, yuvarlak uç/birleşim. Geometry renksizdir; renk sarmalayan öğenin `Foreground`'undan miras alınır (pencere stili `Foreground=TextPrimary` verir). **Tek kullanım biçimi:**

```xml
<ContentControl Style="{StaticResource Icon20}" Content="{StaticResource IconHome}" />
```

| Stil | Boyut | Not |
|---|---|---|
| `Icon24` | 24 | Boş durum, büyük ikon |
| `Icon20` | 20 | Nav, bildirim, hata bandı |
| `Icon16` | 16 | İkon butonları, satır içi |
| `IconLogo16` / `IconLogo20` | 16 / 20 | Dolgulu 4×3 tuş ızgarası marka işareti (`Content` gerekmez) |

`Icon*` stilleri `Tag`'ı iç çizgi kalınlığı için kullanır (1.5 × 24 / boyut); ikon ContentControl'lerinde `Tag` verilmez. Renk değiştirmek için `Foreground` verilir (ör. boş durumda `TextSecondaryBrush`).

Geometry anahtarları:

| Grup | Anahtarlar |
|---|---|
| Navigasyon | `IconHome` (Ana Sayfa), `IconDeck` (Deck), `IconProfiles` (Profiller), `IconActions` (Eylemler), `IconDevices` (Cihazlar), `IconNotifications` (Bildirimler), `IconSettings` (Ayarlar) |
| Kontrol | `IconRefresh` (Yenile), `IconClose` (Kapat), `IconChevronRight` |
| Geri bildirim | `IconCheck` (Tik), `IconCross` (Çarpı), `IconExclamation` (Ünlem), `IconCheckCircle`, `IconCrossCircle`, `IconAlertCircle`, `IconClock` |
| Sistem/bağlantı | `IconTemperature` (Sıcaklık), `IconWifi`, `IconUsb` (kablo/fiş), `IconSystem` (işlemci) |
| Eylem türleri | `IconMicrophone`, `IconMicrophoneOff`, `IconMusic`, `IconMeeting` (kamera), `IconBrowser` (küre), `IconCode` (`</>`) |
| Marka | `IconLogo` (dolgulu) |

Emoji ve renkli ikon yok. Font glif yalnızca pencere başlık düğmelerinde.

---

## 8. Kontroller (`Controls.xaml`) — katalog

### 8.1 Butonlar

| Stil | Görünüm | Normal → Hover → Pressed → Disabled |
|---|---|---|
| `PrimaryButtonStyle` | Beyaz dolgu, siyah SemiBold 14, yükseklik 40, yatay 20, köşe 8, MinWidth 96 | örtü siyah 0 → 0.08 → 0.14 + scale 0.97 → tüm buton %32 opak |
| `SecondaryButtonStyle` | Şeffaf, 1 px `Border`, beyaz metin | örtü beyaz 0 → 0.08 → 0.14, kenar `BorderStrong`'a, scale 0.97 → %32 |
| `GhostButtonStyle` | Zeminsiz, Normal ağırlık, yatay 14 | içerik 0.55 → 1, örtü beyaz 0.06 → 0.10 + scale → içerik 0.25 |
| `IconButtonStyle` | 32×32, ikon `Tag` ile: `Tag="{StaticResource IconRefresh}"` | ikon 0.55 → 1, örtü 0.06 → 0.10, scale 0.94 → 0.25 |
| `CaptionButtonStyle` | 46×40, Segoe Fluent glif 10 | glif 0.7 → 1, örtü beyaz 0.08 → 0.12 (animasyonsuz basma) |
| `CaptionCloseButtonStyle` | 46×40 | hover: **beyaz zemin + siyah glif** (kırmızı YOK) |

- Küçük buton: `Height="32" Padding="{StaticResource ButtonPaddingSmall}" FontSize="{StaticResource FontSizeBodySmall}"` yerel olarak.
- İkon + metin: `Content` içine `StackPanel` (ikon `Icon16` + 8 px + TextBlock); renk miras alınır.
- Her ikon butonuna `ToolTip` zorunlu (Türkçe fiil: "Yenile", "Kapat", "Ayarlar").
- Varsayılan eylem `IsDefault="True"` (Enter). Ekranda tek Primary.

### 8.2 Navigasyon

`NavListBoxStyle` + `NavListBoxItemStyle` (ItemContainerStyle otomatik). Öğe: yükseklik 40, alt boşluk 4, iç yatay 12, köşe 8; ikon 20 + 12 px + metin 14.

| Hâl | Görünüm | Geçiş |
|---|---|---|
| Normal | ikon+metin beyaz %55 | — |
| Hover | zemin beyaz %5, içerik %100 | 120 ms `EaseHover` |
| Seçili | **beyaz zemin, siyah ikon, siyah SemiBold metin** | giriş 180 ms `EaseOut`, çıkış 120 ms |
| Klavye odağı | 1 px beyaz dış halka (3 px dışarıda, 11 yarıçap) | anında |
| Disabled | %35 | — |

Neden ListBox: ok tuşlarıyla gezinme, tek seçim ve `SelectedIndex`/`SelectedItem` bağlama hazır gelir; menü statiktir, öğeler XAML'da yazılır, ikon `Tag` ile verilir (.cs/Converter gerekmez).

### 8.3 Diğerleri

| Stil / anahtar | Özet |
|---|---|
| `QuietCheckBoxStyle` | 14×14 kutu (köşe 3, 1 px `BorderStrong`), etiket 12 px `TextSecondary` × 0.68 opak (≈ Muted). Hover: etiket %100, kutu kenarı `TextSecondary`. İşaretli: beyaz dolu kutu + siyah tik (120 ms). Odak: `FocusVisualCompact`. |
| `ThinScrollBarStyle` (örtük) | 10 px dokunma alanı, 4 px görünen başparmak (#2E2E2E, hover/sürükleme #505050), ok yok, iz şeffaf. |
| `SipoToolTipStyle` (örtük) | `SurfaceElevated` zemin, 1 px `Border`, köşe 6, iç 10,6, 12 px, MaxWidth 280, öğenin 6 px altında, gölge yok. |
| `DividerHorizontalStyle` / `DividerVerticalStyle` | `Rectangle`, 1 px, `BorderSubtle`. |
| `SurfaceCardStyle` | `Border`: `SurfaceSecondary`, 1 px `BorderSubtle`, köşe 12, iç 24. Nadiren (hata bandı). |
| `EmptyStateStyle` | `HeaderedContentControl`: `Tag`=ikon, `Header`=başlık, `Content`=tek satır açıklama (§10.e). |
| `PageHostStyle` | İçerik alanı `ContentControl`'ü; içerik değişince 220 ms opaklık + 8 px yukarı kayma (§10.g). |
| `SipoWindowStyle` | Özel pencere krom'u (§10.a). |
| `FocusVisual` / `FocusVisualCompact` / `FocusVisualInset` | Odak görselleri: 1 px beyaz halka; −3/11, −2/5, +1/2 (margin/yarıçap). Varsayılan kesikli dikdörtgen hiçbir yerde kullanılmaz. |

Odak halkası yalnızca klavye ile gezinmede görünür (WPF `FocusVisualStyle` davranışı); fare tıklaması halka çizmez.

---

## 9. Durum göstergeleri (`StatusIndicators.xaml`)

Tek kullanım biçimi — durum değeri `Tag`'e bağlanır (string veya enum; enum adıyla eşleşir):

```xml
<!-- Yalnızca nokta -->
<Control Style="{StaticResource StatusIndicatorStyle}" Tag="{Binding ConnectionState}" />

<!-- Nokta + metin (8 px aralık) -->
<ContentControl Style="{StaticResource StatusLabelStyle}"
                Tag="{Binding ConnectionState}"
                Content="{Binding ConnectionStateText}" />
```

| Görsel | Tag değerleri | Ölçü |
|---|---|---|
| Dolu beyaz nokta | `Connected`, `Running` | Ø 8 |
| İçi boş beyaz nokta (varsayılan, null dahil) | `Waiting`, `Connecting` | Ø 8, çizgi 1.25 |
| Çapraz çizgili nokta (⊘, gri) | `Disconnected`, `Error`, `Stopped`, `Faulted` | Ø 10, çizgi 1.25, `TextSecondary` |
| Dönen ince beyaz halka | `Busy`, `Starting`, `Stopping` | Ø 12, iz `GaugeTrack` + ¼ yay beyaz 1.5, 900 ms/tur doğrusal |

Kutu 12×12 sabittir; satır hizası kaymaz. Halka **yalnızca** bu durumlarda döner; durum değişince Storyboard kaldırılır.

`SipoDeck.Core.Transport.ConnectionState` ve `SipoDeck.Runtime.RuntimeState` doğrudan bağlanabilir. Türkçe metin eşlemesi (ViewModel üretir):

| Enum | Metin |
|---|---|
| `ConnectionState.Connected` | Bağlı |
| `ConnectionState.Connecting` | Bağlanıyor |
| `ConnectionState.Disconnected` | Bağlantı yok |
| `ConnectionState.Error` | Bağlantı hatası |
| `RuntimeState.Running` | Çalışıyor |
| `RuntimeState.Starting` | Başlatılıyor |
| `RuntimeState.Stopping` | Durduruluyor |
| `RuntimeState.Stopped` | Durduruldu |
| `RuntimeState.Faulted` | Hata |
| `ConnectionType.WiFi` / `Serial` | Wi-Fi / USB |

---

## 10. Ekran spesifikasyonları

Genel yerleşim: pencere 1440×900 hedef, 1280×720 minimum. Tüm ekranlar `BgBase` zemin. Ölçüler DIP'tir.

### 10.a Özel pencere krom'u

`MainWindow`: `Style="{StaticResource SipoWindowStyle}" Title="SipoDeck"`. Stil şunları verir:

| Ayar | Değer | Not |
|---|---|---|
| `WindowChrome.CaptionHeight` | 40 | Pencerenin üst 40 px'i sürükleme alanı; çift tık büyütür, sağ tık sistem menüsü |
| `ResizeBorderThickness` | 6 | Kenardan boyutlandırma |
| `GlassFrameThickness` | 0,0,0,1 | DWM gölgesi ve Win11 yuvarlak köşeleri korunur. Windows'ta altta 1 px parlak çizgi görülürse `0` yapılır (gölge kaybolur) |
| `UseAeroCaptionButtons` | False | Kendi düğmelerimiz |
| `CornerRadius` / `NonClientFrameEdges` | 0 / None | |
| Pencere kenarı | 1 px `BorderSubtle` | Büyütülünce 0 |
| Büyütme taşması | `Trigger WindowState=Maximized` → `Padding = SystemParameters.WindowResizeBorderThickness` | Büyütülmüş WindowChrome penceresi ekrandan ~7–8 px taşar; bu iç boşluk düzeltir |
| Başlık düğmeleri | Sağ üst, 46×40 ×3: Küçült `E921`, Büyüt `E922` / Önceki boyut `E923` (maximize'da yer değiştirir), Kapat `E8BB` | `IsHitTestVisibleInChrome=True`. Pasif pencerede %60 opak |
| Foreground / Font | `TextPrimary` / `FontSans` 14 | Tüm miras bu kökten |

Şablon, içeriği **tam pencere** olarak yerleştirir ve başlık düğmelerini üstüne bindirir (ayrı başlık satırı yoktur). Bu nedenle:
- İçeriğin üst 40 px'i sürükleme alanıdır. Buraya yalnızca etkileşimsiz öğe konur (logo). Etkileşimli öğe gerekiyorsa `WindowChrome.IsHitTestVisibleInChrome="True"` verilir.
- Sağ üst 138×40 alan (3 düğme) içerikle çakışmamalıdır.
- Açılış videosu başlık altına tam taşar (sinematik), üst maske düğmeleri okunur kılar.

**.cs gereksinimi (MainWindow.xaml.cs ctor):**

```csharp
CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
```

Kapat → `Close()` → mevcut `Closing` davranışı (tepsiye gizleme) aynen çalışır; değiştirilmez. Bağlama yapılmazsa düğmeler devre dışı (soluk) görünür — bu kasıtlı bir uyarı işaretidir.

### 10.b Açılış ekranı (Splash / Welcome)

Açılış, `MainWindow` içinde kabuğun **üstüne bindirilmiş** bir katmandır (ayrı pencere değil). Böylece geçiş tek pencerede yumuşak olur.

Katmanlar (arkadan öne, hepsi aynı Grid hücresinde, `ClipToBounds=True`):

| # | Öğe | Ölçü / kaynak |
|---|---|---|
| 1 | `VideoLayer` (Grid, `Opacity=0`, `RenderTransformOrigin=0.5,0.4`, yerel `ScaleTransform`) | Tam pencere |
| 1a | `MediaElement` — `Source` tek yerden, `IsMuted=True`, `LoadedBehavior=Manual`, `Stretch=UniformToFill`, `UnloadedBehavior=Close` | Video yoksa gizli |
| 1b | Yer tutucu: `Viewbox MaxWidth=640 Stretch=Uniform Margin=64,72,64,300 VerticalAlignment=Center` içinde `ListBox Style=DeviceVisualStyle ItemTemplate=DeviceKeyBlankTemplate IsHitTestVisible=False Focusable=False` + 1 px `LightSweep` dikdörtgeni; üstüne `Rectangle Fill=MaskVignetteBrush` | Video varsa gizli |
| 2 | Üst maske `Rectangle Height=160 VerticalAlignment=Top Fill=MaskFadeFromTopBrush` | |
| 3 | Alt maske `Rectangle Height=440 VerticalAlignment=Bottom Fill=MaskFadeFromBottomBrush` | "Alt tarafta çok hafif siyah geçiş" |
| 4 | Logo: `IconLogo20` + 12 px + "SipoDeck" `TextWordmarkLarge`; sol üst `Margin=40,30,0,0`, yükseklik 24 | |
| 5 | Metin bloğu: `StackPanel HorizontalAlignment=Left VerticalAlignment=Bottom Margin=64,0,64,64 MaxWidth=560` | |
| 5a | Başlık "Kontrol, parmaklarının ucunda." `TextDisplay` | |
| 5b | Açıklama "Cihazını bağla, tuşlarını düzenle ve çalışma alanını kontrol et." `TextBodyLarge`, üst 16, `MaxWidth=440` | |
| 5c | Butonlar (üst 40): "Başla" `PrimaryButtonStyle MinWidth=120 IsDefault=True` · 12 px · "Cihaz bağla" `SecondaryButtonStyle` | |
| 6 | "Bir daha gösterme" `QuietCheckBoxStyle`, sağ alt `Margin=0,0,64,76` (buton satırıyla aynı hizada) | Görsel olarak geri planda |

Video kaynağı — **tek yer:** `Assets/Video/intro.mp4`. Önerilen: `SplashViewModel` (veya tek bir `AppAssets` sabiti) `IntroVideoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Video", "intro.mp4")`, `HasIntroVideo = File.Exists(IntroVideoPath)`. csproj'a (kodlama ajanı) `<Content Include="Assets\Video\**" CopyToOutputDirectory="PreserveNewest" />`. `MediaElement` pack URI'yi oynatamaz; mutlak dosya yolu kullanılır. Döngü: `MediaEnded` → `Position = TimeSpan.Zero; Play();`. `MediaFailed` → yer tutucuya düş. Video önerisi: H.264 MP4, siyah zemin, 1920×1080, sessiz, 6–10 sn döngü.

Yer tutucu (video yokken): XAML ile çizilmiş cihaz silüeti (tuşlar boş), karanlıktan yavaşça belirir; gövdenin üst kenarında tek seferlik ince beyaz ışık süpürmesi (monokrom), sahne ışığı bulanık gri elips ve kenar vinyeti. Döngü animasyonu **yoktur**; belirme bittikten sonra sahne durağandır.

Zaman çizelgesi (sinematik istisna; `Grid.Triggers` → `FrameworkElement.Loaded`):

| t (ms) | Öğe | Animasyon |
|---|---|---|
| 0 | `VideoLayer` | Opacity 0→1, 1400 ms `EaseCinematic`; Scale 1.04→1.00, 1800 ms `EaseOutStrong` |
| 300 | `Logo` | Opacity 0→1, 600 ms `EaseOut` |
| 600 | `Headline` | Opacity 0→1 + Y 12→0, 600 ms `EaseOutStrong` |
| 720 | `Description` | aynı |
| 840 | `Actions` | aynı |
| 1400 | `DontShowAgain` | Opacity 0→1, 400 ms `EaseOut` |
| 1400 | `LightSweep` (yalnızca yer tutucu) | X −120→120, 1600 ms `EaseInOut`; Opacity 0→0.55→0 |

Öğeler yerelde `Opacity="0"` başlar (ilk karede parlama olmaz); Y animasyonu alan öğeler yerel `<TranslateTransform/>` taşır. Butonlar opaklık 0 iken de tıklanabilir (etkileşim bekletilmez). Doğrulanmış XAML iskeleti:

```xml
<Grid x:Name="SplashRoot" Background="{StaticResource BgBaseBrush}" ClipToBounds="True">
    <Grid.Triggers>
        <EventTrigger RoutedEvent="FrameworkElement.Loaded">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetName="VideoLayer" Storyboard.TargetProperty="Opacity"
                                     From="0" To="1" Duration="{StaticResource DurationCinematicReveal}"
                                     EasingFunction="{StaticResource EaseCinematic}" />
                    <DoubleAnimation Storyboard.TargetName="VideoLayer"
                                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                     From="1.04" To="1" Duration="0:0:1.8" EasingFunction="{StaticResource EaseOutStrong}" />
                    <DoubleAnimation Storyboard.TargetName="VideoLayer"
                                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                                     From="1.04" To="1" Duration="0:0:1.8" EasingFunction="{StaticResource EaseOutStrong}" />
                    <DoubleAnimation Storyboard.TargetName="Logo" Storyboard.TargetProperty="Opacity"
                                     From="0" To="1" BeginTime="0:0:0.3" Duration="0:0:0.6" EasingFunction="{StaticResource EaseOut}" />
                    <DoubleAnimation Storyboard.TargetName="Headline" Storyboard.TargetProperty="Opacity"
                                     From="0" To="1" BeginTime="0:0:0.6" Duration="{StaticResource DurationCinematicText}" EasingFunction="{StaticResource EaseOutStrong}" />
                    <DoubleAnimation Storyboard.TargetName="Headline" Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.Y)"
                                     From="12" To="0" BeginTime="0:0:0.6" Duration="{StaticResource DurationCinematicText}" EasingFunction="{StaticResource EaseOutStrong}" />
                    <!-- Description: BeginTime 0:0:0.72, Actions: 0:0:0.84 — Headline ile aynı iki animasyon -->
                    <DoubleAnimation Storyboard.TargetName="DontShowAgain" Storyboard.TargetProperty="Opacity"
                                     From="0" To="1" BeginTime="0:0:1.4" Duration="0:0:0.4" EasingFunction="{StaticResource EaseOut}" />
                    <DoubleAnimation Storyboard.TargetName="LightSweep" Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.X)"
                                     From="-120" To="120" BeginTime="0:0:1.4" Duration="0:0:1.6" EasingFunction="{StaticResource EaseInOut}" />
                    <DoubleAnimationUsingKeyFrames Storyboard.TargetName="LightSweep" Storyboard.TargetProperty="Opacity" BeginTime="0:0:1.4">
                        <LinearDoubleKeyFrame KeyTime="0:0:0" Value="0" />
                        <LinearDoubleKeyFrame KeyTime="0:0:0.8" Value="0.55" />
                        <LinearDoubleKeyFrame KeyTime="0:0:1.6" Value="0" />
                    </DoubleAnimationUsingKeyFrames>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Grid.Triggers>

    <Grid x:Name="VideoLayer" Opacity="0" RenderTransformOrigin="0.5,0.4">
        <Grid.RenderTransform><ScaleTransform /></Grid.RenderTransform>
        <!-- MediaElement (HasIntroVideo) VEYA aşağıdaki yer tutucu -->
        <Grid x:Name="Placeholder">
            <Viewbox MaxWidth="640" Stretch="Uniform" Margin="64,72,64,300" VerticalAlignment="Center">
                <Grid>
                    <ListBox Style="{StaticResource DeviceVisualStyle}"
                             ItemTemplate="{StaticResource DeviceKeyBlankTemplate}"
                             IsHitTestVisible="False" Focusable="False" />
                    <Rectangle x:Name="LightSweep" Width="80" Height="1" Margin="0,1,0,0"
                               VerticalAlignment="Top" HorizontalAlignment="Center"
                               Fill="{StaticResource TextPrimaryBrush}" Opacity="0">
                        <Rectangle.RenderTransform><TranslateTransform /></Rectangle.RenderTransform>
                    </Rectangle>
                </Grid>
            </Viewbox>
            <Rectangle Fill="{StaticResource MaskVignetteBrush}" IsHitTestVisible="False" />
        </Grid>
    </Grid>

    <Rectangle Height="160" VerticalAlignment="Top" Fill="{StaticResource MaskFadeFromTopBrush}" IsHitTestVisible="False" />
    <Rectangle Height="440" VerticalAlignment="Bottom" Fill="{StaticResource MaskFadeFromBottomBrush}" IsHitTestVisible="False" />

    <StackPanel x:Name="Logo" Opacity="0" Orientation="Horizontal" Height="24"
                HorizontalAlignment="Left" VerticalAlignment="Top" Margin="40,30,0,0">
        <ContentControl Style="{StaticResource IconLogo20}" />
        <TextBlock Style="{StaticResource TextWordmarkLarge}" Text="SipoDeck" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>

    <StackPanel HorizontalAlignment="Left" VerticalAlignment="Bottom" Margin="64,0,64,64" MaxWidth="560">
        <TextBlock x:Name="Headline" Opacity="0" Style="{StaticResource TextDisplay}" Text="Kontrol, parmaklarının ucunda.">
            <TextBlock.RenderTransform><TranslateTransform /></TextBlock.RenderTransform>
        </TextBlock>
        <TextBlock x:Name="Description" Opacity="0" Style="{StaticResource TextBodyLarge}" Margin="0,16,0,0" MaxWidth="440"
                   HorizontalAlignment="Left" Text="Cihazını bağla, tuşlarını düzenle ve çalışma alanını kontrol et.">
            <TextBlock.RenderTransform><TranslateTransform /></TextBlock.RenderTransform>
        </TextBlock>
        <StackPanel x:Name="Actions" Opacity="0" Orientation="Horizontal" Margin="0,40,0,0">
            <StackPanel.RenderTransform><TranslateTransform /></StackPanel.RenderTransform>
            <Button Style="{StaticResource PrimaryButtonStyle}" Content="Başla" MinWidth="120" IsDefault="True" />
            <Button Style="{StaticResource SecondaryButtonStyle}" Content="Cihaz bağla" Margin="12,0,0,0" />
        </StackPanel>
    </StackPanel>

    <CheckBox x:Name="DontShowAgain" Opacity="0" Style="{StaticResource QuietCheckBoxStyle}" Content="Bir daha gösterme"
              HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="0,0,64,76" />
</Grid>
```

Davranış:
- **İlk açılış:** açılış tam gösterilir ve kullanıcı eylemine kadar bekler. "Başla" → Ana Sayfa. "Cihaz bağla" → Cihazlar sayfası (015'te yer tutucu). Enter = Başla.
- **İlk kurulum tamamlandıysa (kısa mod):** aynı ekran; 2400 ms sonra kendiliğinden kabuğa geçer. Herhangi bir tıklama, Enter veya Esc hemen geçirir. Fare butonların üzerindeyse otomatik geçiş iptal edilir.
- **"Bir daha gösterme" işaretliyse:** sonraki açılışta açılış atlanır, kabuk doğrudan §10.g giriş animasyonuyla görünür. (Bu tercihin kalıcı saklanması bir ayar alanı gerektirir — §14 risk.)
- 1280×720'de cihaz yer tutucusu `Viewbox` sayesinde küçülür, metinle çakışmaz (doğrulandı).

### 10.c Kabuk (Shell)

```
+--------------+-+---------------------------------------------+
| [#] SipoDeck | |                                  _  []  X   |  <- üst 40 px: sürükleme bandı
|              | |  Sayfa başlığı (y=56)                        |
| [Ana Sayfa]  | |                                              |
|  Deck        | |        İçerik alanı (PageHostStyle)          |
|  Profiller   | |                                              |
|  Eylemler    | |                                              |
|  Cihazlar    | |                                              |
|  Bildirimler | |                                              |
|  Ayarlar     | |                                              |
|              | |                                              |
| ------------ | |                                              |
| o SipoDeck One  (ayar)                                        |
|   Bağlı · Wi-Fi                                               |
| Aktif profil / Çalışma                                        |
| Çalışma motoru / Çalışıyor                                    |
+--------------+-+---------------------------------------------+
  208 px        1 px   *
```

Kök Grid sütunları: `208` (`NavColumnWidth`) · `1` (dikey ayırıcı) · `*`. Nav zemini `BgBase` (içerikle aynı; ayrım yalnızca 1 px `DividerVerticalStyle`).

Nav sütunu satırları: `64` (logo) · `Auto` (menü) · `*` · `Auto` (alt bilgi).

| Bölüm | Spesifikasyon |
|---|---|
| Logo | `StackPanel Horizontal Height=16 Margin=24,12,0,0 VerticalAlignment=Top`: `IconLogo16` + 10 px + "SipoDeck" `TextWordmark`. Dikey merkezi y=20 → başlık düğmesi glifleriyle aynı hizada. Sürükleme bandı içinde, etkileşimsiz. |
| Menü | `ListBox Style=NavListBoxStyle Margin=NavListMargin` (12,8,12,0) → öğe genişliği 184. Sıra ve ikonlar: Ana Sayfa `IconHome` · Deck `IconDeck` · Profiller `IconProfiles` · Eylemler `IconActions` · Cihazlar `IconDevices` · Bildirimler `IconNotifications` · Ayarlar `IconSettings`. |
| Alt bilgi | Üstte `DividerHorizontalStyle Margin=24,0`. Altında `Grid Margin=NavFooterPadding` (24,20,16,24), sütunlar `*`,`Auto`. |
| — Cihaz | `StatusLabelStyle Tag={ConnectionState}` içinde cihaz adı `TextBodySmallStrong`; altında `TextCaption Margin=20,2,0,0` "Bağlı · Wi-Fi" (bağlı değilken yalnızca durum metni, bağlantı türü gizli). Cihaz adı bilinmiyorsa "Cihaz yok". ToolTip: "Donanım yazılımı 1.2.0" (yalnızca biliniyorsa). |
| — Ayarlar kısayolu | Sağ sütun: `IconButtonStyle Tag=IconSettings ToolTip="Ayarlar" VerticalAlignment=Top Margin=0,-7,0,0` → Ayarlar sayfasını seçer. |
| — Aktif profil | `TextCaptionMuted` "Aktif profil" (üst 16) + `TextBodySmall` profil adı (üst 2). Profil yoksa "Profil yok" `TextSecondary`. |
| — Çalışma motoru | `TextCaptionMuted` "Çalışma motoru" (üst 12) + `TextBodySmall` durum metni (§9 tablo). `Starting/Stopping` iken metnin soluna `StatusIndicatorStyle` (dönen halka) 8 px aralıkla; `Faulted` iken metin "Hata" ve Ana Sayfa'da hata bandı (§10.d). |

015 görevindeki "durum alanı" gereksinimi (Runtime durumu, cihaz bağlantısı, cihaz adı + firmware, aktif profil) ayrı bir alt çubuk yerine bu alt bilgiye ve Ana Sayfa bilgi sütununa gömülür. Ayrı alt/üst durum satırı **yoktur** (sade görünüm). IP/port/COM hiçbir yerde gösterilmez.

İçerik sütunu: `ContentControl Style=PageHostStyle Content="{Binding CurrentPage, NotifyOnTargetUpdated=True}"`; ViewModel → View eşlemesi `DataTemplate DataType` ile. Her sayfanın kökü `Margin=PagePadding` (56,56,56,40) alır; sayfa başlığı `TextTitle1`.

Doğrulanmış kabuk XAML'i:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{StaticResource NavColumnWidth}" />
        <ColumnDefinition Width="1" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <Grid Grid.Column="0">
        <Grid.RowDefinitions>
            <RowDefinition Height="64" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <StackPanel Orientation="Horizontal" Margin="24,12,0,0" VerticalAlignment="Top" Height="16">
            <ContentControl Style="{StaticResource IconLogo16}" />
            <TextBlock Style="{StaticResource TextWordmark}" Text="SipoDeck" Margin="10,0,0,0" VerticalAlignment="Center" />
        </StackPanel>
        <ListBox Grid.Row="1" Style="{StaticResource NavListBoxStyle}" Margin="{StaticResource NavListMargin}"
                 SelectedIndex="{Binding SelectedNavIndex}">
            <ListBoxItem Tag="{StaticResource IconHome}" Content="Ana Sayfa" />
            <ListBoxItem Tag="{StaticResource IconDeck}" Content="Deck" />
            <ListBoxItem Tag="{StaticResource IconProfiles}" Content="Profiller" />
            <ListBoxItem Tag="{StaticResource IconActions}" Content="Eylemler" />
            <ListBoxItem Tag="{StaticResource IconDevices}" Content="Cihazlar" />
            <ListBoxItem Tag="{StaticResource IconNotifications}" Content="Bildirimler" />
            <ListBoxItem Tag="{StaticResource IconSettings}" Content="Ayarlar" />
        </ListBox>
        <StackPanel Grid.Row="3">
            <Rectangle Style="{StaticResource DividerHorizontalStyle}" Margin="24,0" />
            <Grid Margin="{StaticResource NavFooterPadding}">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <StackPanel>
                    <ContentControl Style="{StaticResource StatusLabelStyle}" Tag="{Binding ConnectionState}">
                        <TextBlock Style="{StaticResource TextBodySmallStrong}" Text="{Binding DeviceName}" />
                    </ContentControl>
                    <TextBlock Style="{StaticResource TextCaption}" Text="{Binding ConnectionSummary}" Margin="20,2,0,0" />
                    <TextBlock Style="{StaticResource TextCaptionMuted}" Text="Aktif profil" Margin="0,16,0,0" />
                    <TextBlock Style="{StaticResource TextBodySmall}" Text="{Binding ActiveProfileName}" Margin="0,2,0,0" />
                    <TextBlock Style="{StaticResource TextCaptionMuted}" Text="Çalışma motoru" Margin="0,12,0,0" />
                    <TextBlock Style="{StaticResource TextBodySmall}" Text="{Binding RuntimeStateText}" Margin="0,2,0,0" />
                </StackPanel>
                <Button Grid.Column="1" Style="{StaticResource IconButtonStyle}" Tag="{StaticResource IconSettings}"
                        ToolTip="Ayarlar" VerticalAlignment="Top" Margin="0,-7,0,0"
                        Command="{Binding OpenSettingsCommand}" />
            </Grid>
        </StackPanel>
    </Grid>

    <Rectangle Grid.Column="1" Style="{StaticResource DividerVerticalStyle}" />

    <ContentControl Grid.Column="2" Style="{StaticResource PageHostStyle}"
                    Content="{Binding CurrentPage, NotifyOnTargetUpdated=True}" />
</Grid>
```

(Bağlama adları öneridir; ViewModel tasarımı kodlama ajanına aittir.)

### 10.d Ana Sayfa

Cihaz merkezli; sayı kartı, grafik yok.

Sayfa kökü `Grid Margin=PagePadding`, satırlar `Auto` (başlık) · `Auto` (hata bandı, yalnızca gerekirse) · `*` (sahne).

| Bölge | Spesifikasyon |
|---|---|
| Başlık | `TextTitle1` "Hoş geldin"; altında `TextBodySecondary` üst 8 alt başlık (duruma göre, aşağıdaki tablo). |
| Hata bandı | Yalnızca `RuntimeState.Faulted` veya Runtime kritik hatası varsa. `Border Style=SurfaceCardStyle Padding=16 Margin=0,24,0,0 MaxWidth=720 HorizontalAlignment=Left`: `Icon20 IconAlertCircle` + 12 px + `TextBodyStrong` "Çalışma motoru başlatılamadı" / altında `TextCaption` Runtime'ın anlaşılır mesajı. Renk yok. |
| Sahne | `Grid Margin=0,32,0,0`, sütunlar `*` · `64` · `HomeInfoColumnWidth`(320). |
| Cihaz | Sütun 0: `Viewbox MaxWidth=560 MaxHeight=440 Stretch=Uniform Margin=0,0,0,40` içinde `ListBox Style=DeviceVisualStyle Tag={ConnectionState} IsHitTestVisible=False Focusable=False`. 1440×900'de ~1.36×, 1280×720'de ~1.3× ölçeklenir. |
| Bilgi sütunu | Sütun 2: `StackPanel VerticalAlignment=Center Margin=0,0,0,40` |

Bilgi sütunu sırası:

| Öğe | Stil | Boşluk |
|---|---|---|
| "CİHAZ" | `TextOverline` | — |
| Cihaz adı ("SipoDeck One") | `TextTitle2` | üst 6 |
| Durum satırı: `StatusLabelStyle` (Tag=ConnectionState, Content=durum metni) + `TextBodySecondary` " · Wi-Fi" (yalnızca bağlıyken) | — | üst 10 |
| Ayırıcı | `DividerHorizontalStyle` | 28 üst / 24 alt |
| "AKTİF PROFİL" / profil adı | `TextOverline` / `TextBody` | değer üst 6 |
| "SON İŞLEM" / `Icon16` (eylem türü ikonu) + 8 px + başlık ("Mikrofon kapatıldı") / altında `TextCaption Margin=24,2,0,0` ("Çalışma profili · Tuş 3") | `TextOverline` / `TextBody` / `TextCaption` | grup üst 20, değer üst 6 |
| "DONANIM YAZILIMI" / sürüm (yalnızca biliniyorsa) | `TextOverline` / `TextMono` + `TextSecondaryBrush` | grup üst 20 |
| Butonlar: "Deck'i düzenle" `PrimaryButtonStyle` · 12 px · "Sistem bilgileri" `SecondaryButtonStyle` | — | üst 36 |

Son işlem ikonu eşlemesi: mikrofon → `IconMicrophone/IconMicrophoneOff`, medya → `IconMusic`, toplantı → `IconMeeting`, tarayıcı/URL → `IconBrowser`, program/kod → `IconCode`, sistem → `IconSystem`, bilinmeyen → `IconActions`.

Durumlar:

| Durum | Alt başlık | Cihaz görseli | Durum satırı | Butonlar |
|---|---|---|---|---|
| Bağlı | "{Cihaz adı} hazır. Tuşların {profil} profiliyle eşleşiyor." | tam opak | ● Bağlı · Wi-Fi/USB | Deck'i düzenle (P) · Sistem bilgileri (S) |
| Bağlanıyor | "{Cihaz adı} bekleniyor…" | tam opak | ○ Bağlanıyor | aynı |
| Bağlantı yok (cihaz biliniyor) | "Cihazın bağlı değil. USB ile bağla veya aynı ağa bağlan." | 260 ms'de %40'a söner (`Tag=Disconnected`) | ⊘ Bağlantı yok (tür gizli) | **Cihaz bağla (P)** → Cihazlar · Deck'i düzenle (S) |
| Hiç cihaz yok | "Başlamak için SipoDeck cihazını bağla." | %40, tuş numaraları görünür | ⊘ Bağlantı yok; cihaz adı yerine "Cihaz bulunamadı" (`TextTitle2`) | Cihaz bağla (P) |
| Son işlem yok | — | — | "SON İŞLEM" altında `TextBodySecondary` "Henüz bir işlem çalıştırılmadı." | — |
| Firmware bilinmiyor | — | — | "DONANIM YAZILIMI" grubu tümden gizli | — |

015 kapsamı: "Deck'i düzenle" Deck yer tutucu sayfasına gider. "Sistem bilgileri" paneli ayrı task olduğundan `IsEnabled=False`, `ToolTip="Yakında"`, `ToolTipService.ShowOnDisabled=True`.

**Cihaz görseli (`DeviceVisual.xaml`)**

- **Yerleşim 4 sütun × 3 satır (yatay).** Gerekçe: (1) 16:10 pencerede yatay gövde cihazın yanına 320 px bilgi sütununu rahat sığdırır; 3×4 dikey gövde 720 px yükseklikte sahneye sığmak için fazla küçülürdü. (2) Makro klavyelerde yaygın, tek elle taranan 4'lü sıra. (3) Numara soldan sağa, yukarıdan aşağı 1–12 ("Tuş 3" = üst sıra 3.). Gerçek cihaz dikeyse yalnızca `UniformGrid Rows/Columns` (4/3) ve gövde `Width/Height` (312×412) değişir.
- Gövde 400×324, köşe 28, `DeviceBody` + 1 px `DeviceBodyEdge`; alt yarıda `MaskDeviceShadeBrush`; üst kenarda 1 px `DeviceRimLight` (yanlardan 72 px içeride) → "ışık kenarı yakalıyor" hissi. Altta 9 px mono "SIPODECK" kazıma (`TextMuted`, %70).
- Tuş 72×72, aralık 16 (her tuş `Margin=8`), köşe 12: 3 px aşağı kaydırılmış koyu yan yüz (`DeviceKeySide`) + yüz (`DeviceKeyFace`, 1 px `DeviceKeyEdge`) + üstte 1 px `DeviceKeyHighlight`.
- Sahne ışığı: gövdenin altında 560×200 düz gri (`StageLight`) elips + `BlurEffect Radius=90`, `Canvas` içinde (kırpılmaz), `BitmapCache` ile önbellekli. Gradient değildir.
- Hâller (`DeviceKeyStyle`, VSM): hover → yüz `DeviceKeyFaceHover` + `BorderStrong` kenar (120 ms); seçili → **beyaz yüz + siyah içerik** (180 ms). Klavye odağı: 1 px beyaz halka, 4 px dışarıda, yarıçap 16.
- İçerik: varsayılan `DeviceKeyIndexTemplate` (sol üst 10,8 mono 10 px numara, %55). Açılışta `DeviceKeyBlankTemplate`. Deck ekranı (017) kendi `ItemTemplate`'ini verir: ortada `Icon24` + altında 11 px kısa başlık; renk `TextElement.Foreground`'dan miras (seçilince otomatik siyah).
- Veri: `ItemsSource` verilmezse `DeviceKeyPlaceholders` (1..12). `Tag` = bağlantı durumu → `Disconnected/Error/Stopped/Faulted` iken tüm cihaz 260 ms'de %40 opaklığa iner, dönüşte 260 ms'de %100.
- İleride (017, fiziksel tuş basıldı geri bildirimi): ilgili tuş 100 ms'de scale 0.96 + yüz `SurfacePressed`, 180 ms `EaseOutStrong` ile geri. Bu task'ta uygulanmaz.

### 10.e Yer tutucu sayfalar (Deck, Profiller, Eylemler, Cihazlar, Bildirimler, Ayarlar)

Tek ortak tasarım:

```xml
<Grid Margin="{StaticResource PagePadding}">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <TextBlock Style="{StaticResource TextTitle1}" Text="Profiller" />
    <HeaderedContentControl Grid.Row="1" Margin="0,0,0,64"
                            Style="{StaticResource EmptyStateStyle}"
                            Tag="{StaticResource IconProfiles}"
                            Header="Henüz hazır değil"
                            Content="Profil oluşturma ve düzenleme yakında burada." />
</Grid>
```

Boş durum bileşeni: 56×56 kutu (köşe 16, `SurfaceSecondary`, 1 px `BorderSubtle`) içinde `Icon24` `TextSecondary` · 20 px · `TextTitle3` başlık · 6 px · `TextBodySecondary` tek satır, ortalı, MaxWidth 360. Alt `Margin=64` optik merkezi biraz yukarı alır.

| Sayfa | İkon | Başlık | Açıklama |
|---|---|---|---|
| Deck | `IconDeck` | Henüz hazır değil | Tuşlara eylem atama yakında burada. |
| Profiller | `IconProfiles` | Henüz hazır değil | Profil oluşturma ve düzenleme yakında burada. |
| Eylemler | `IconActions` | Henüz hazır değil | Eylem kitaplığı yakında burada. |
| Cihazlar | `IconDevices` | Henüz hazır değil | Cihaz eşleştirme ve yönetimi yakında burada. |
| Bildirimler | `IconNotifications` | Henüz hazır değil | İşlem bildirimlerinin geçmişi yakında burada. |
| Ayarlar | `IconSettings` | Henüz hazır değil | Uygulama ayarları yakında burada. |

### 10.f İleride: sistem bilgileri paneli ve işlem bildirimi (bu task'ta UYGULANMAZ)

Tokenlar hazır: `SystemPanelWidth` 520 · `SystemPanelHeight` 144 · `SystemPanelHandleWidth/Height` 48/4 · `SystemGaugeSize` 56 · `SystemGaugeStroke` 3 · `SystemGaugeStrokeCritical` 5 · `RadiusSystemPanel` 0,0,24,24 · `DurationPanelOpen/Close` · `DurationGaugeFill` · `EaseDrawer` / `KeySplineDrawer` · `SystemPanelAutoCloseSeconds` 7 · `ToastWidth` 360 · `ToastOffset` 16 · `RadiusToast` 12 · `DurationToastIn/Out` · `MotionToastOffset` −8 · `ToastLifetimeSeconds` 3.5 · `ToastErrorLifetimeSeconds` 6.

**Sistem bilgileri paneli**
- Ayrı, kenarlıksız, `Topmost`, görev çubuğunda görünmeyen pencere; ekranın (aktif monitör) üst ortasına yapışık; `AllowsTransparency=True` (gölge için yanlarda/altta 24 px şeffaf pay).
- Kapalı: yalnızca tutamaç — 48×4, köşe 2, `BorderStrong`; ekran üst kenarından 6 px aşağıda; 120×16 görünmez vuruş alanı. Tetiklenince tutamaç 120 ms'de `TextSecondary`'ye aydınlanır.
- Açık yüzey: 520×144, zemin `BgBase`, yalnızca sol/sağ/alt 1 px `BorderSubtle`, alt köşeler 24, üst kenar ekranla birleşik (köşe 0). Gölge: `DropShadowEffect Color=Black BlurRadius=32 ShadowDepth=8 Direction=270 Opacity=0.5`.
- İç düzen: `Padding=24,16,24,20`. Satır 1: `TextOverline` "SİSTEM DURUMU" solda; sağda `IconButtonStyle` Yenile (`IconRefresh`) + 4 px + Kapat (`IconClose`). Satır 2 (üst 12): 4 gösterge eşit aralıklı (`UniformGrid Columns=4`). Her gösterge: 56×56 halka (iz `GaugeTrack` 3 px, kullanım beyaz 3 px, yuvarlak uç, 12 yönünden saat yönüne), merkezde `TextBodySmallStrong` "C" / "M" / "D" / `Icon16 IconTemperature`; altında (üst 8) `TextMonoSmall` + `TextSecondary` "42 %" / "61 %" / "73 %" / "48 °". Etiket metni yok (merkez harfi yeterli), `ToolTip` ile "İşlemci", "Bellek", "Disk", "Sıcaklık".
- Kritik (ör. ≥ 90 % veya ≥ 85 °): halka kalınlığı 5, değerin yanında `Icon16 IconExclamation`; yanıp sönme YOK, renk YOK.
- Hareket: açılış `TranslateY −144 → 0`, 240 ms `EaseDrawer`; kapanış `0 → −144`, 180 ms `EaseOut`. Açıldıktan 60 ms sonra halkalar 0'dan değere 260 ms `EaseOutStrong`, 40 ms stagger, **tek sefer**. Yenile: değerler yeni değere 260 ms'de akar (sıfırlanmaz).
- Etkileşim: 7 sn sonra otomatik kapanır; fare üzerindeyken sayaç durur; yukarı sürükleme (24 px veya hızlı fırlatma) ve Esc kapatır; tekrar tetikleme aç/kapa.
- .cs gereksinimi (o task'ta): yüzdeden yay `Geometry` üreten küçük bir converter veya kontrol.

**İşlem bildirimi (toast)**
- Ayrı `Topmost` pencere, çalışma alanının sağ üstü, kenarlardan 16 px. Genişlik 360, `Padding=16`, köşe 12, zemin `BgBase`, 1 px `Border`.
- İçerik: `Icon20` (`IconCheckCircle` başarı / `IconCrossCircle` hata / `IconAlertCircle` uyarı) beyaz · 12 px · başlık `TextBodyStrong` ("Mikrofon kapatıldı") / altında `TextCaption` ("Çalışma profili · Tuş 3"). Hata örneği: "Discord açılamadı" / "Uygulama yolu bulunamadı." + sağ altta küçük `SecondaryButtonStyle` (Height 32) "Eylemi düzenle". Renkli şerit yok.
- Hareket: giriş Opacity 0→1 + Y −8→0, 220 ms `EaseOutStrong` (üst kenardan gelir); çıkış Opacity →0 + Y →−4, 180 ms `EaseOut`. Yığın: yeni bildirim üstte, diğerleri 8 px aralıkla 220 ms `EaseInOut` ile aşağı kayar. Süre: 3.5 sn (hata + eylem butonu: 6 sn); fare üzerindeyken durur.

### 10.g Ekran geçişleri

**Sayfa değişimi (nav):** `PageHostStyle` — içerik değişince yeni sayfa Opacity 0→1 + Y 8→0, 220 ms `EaseOutStrong`. Eski sayfa animasyonsuz kalkar (çıkış animasyonu yok → hızlı his, çift çizim yok). Gerekli: `Content="{Binding CurrentPage, NotifyOnTargetUpdated=True}"`.

**Açılış → Kabuk:**

| t (ms) | Öğe | Animasyon |
|---|---|---|
| 0 | Açılış kökü | Opacity 1→0, 260 ms `EaseOut`; `VideoLayer` scale 1.00→1.02 aynı sürede (ileri doğru his) |
| 80 | Kabuk kökü (açılışın altında, başta Opacity 0, yerel `TranslateTransform`) | Opacity 0→1 + Y 8→0, 260 ms `EaseOutStrong` |
| 340 | Açılış | Görsel ağaçtan kaldırılır (ViewModel `Splash = null`) → `MediaElement` `UnloadedBehavior=Close` ile video çözücüyü bırakır |

Uygulama (öneri, .cs minimal): `MainWindow` kök Grid'inde `ShellRoot` ve üstünde `ContentControl Content="{Binding Splash}"`. `ShellViewModel.IsSplashVisible` false olunca `DataTrigger EnterActions` ile yukarıdaki iki storyboard başlar; ViewModel 340 ms sonra `Splash = null` yapar.

**Açılış atlanırsa:** `ShellRoot` pencere yüklenince `FadeInUpStoryboard` süresiyle ama 260 ms (`DurationSlow`) ile belirir.

---

## 11. Ekran dışı küçük kurallar

- **Metin dili:** Türkçe, kısa, fiil odaklı butonlar ("Başla", "Cihaz bağla", "Deck'i düzenle", "Sistem bilgileri", "Eylemi düzenle", "Yenile", "Kapat"). Lorem ipsum yok. Sonunda nokta yalnızca cümle olan açıklamalarda.
- **Tepsi ipucu metni:** "SipoDeck — Bağlı (Wi-Fi)" / "SipoDeck — Bağlantı yok" (IP/port yok).
- **Klavye:** Tab sırası nav → içerik. Başlık düğmeleri tab dışında. Enter varsayılan buton. Esc açılışı geçer.
- **Yüksek DPI:** Tüm ölçüler DIP; `UseLayoutRounding=True` pencere stilinde.

---

## 12. Genel anahtar kataloğu (hızlı başvuru)

- **Renk/Fırça:** `BgBase` `SurfaceSecondary` `SurfaceElevated` `Border` `TextPrimary` `TextSecondary` `TextMuted` `SelectionBg` `SelectionFg` `SurfaceHover` `SurfacePressed` `BorderSubtle` `BorderStrong` `TextDisabled` `PrimaryHover` `PrimaryPressed` `ScrollThumb` `ScrollThumbHover` `GaugeTrack` `Black` + `Device*` `StageLight` (her biri `…Color`/`…Brush`), `FocusRingBrush`, `TransparentBrush`, `MaskFadeFromTopBrush`, `MaskFadeFromBottomBrush`, `MaskVignetteBrush`, `MaskDeviceShadeBrush`.
- **Tipografi:** `FontSans` `FontDisplay` `FontMono` `FontIcons`; `FontSize*`, `LineHeight*`; stiller `TextBase` `TextDisplay` `TextTitle1` `TextTitle2` `TextTitle3` `TextBodyLarge` `TextBody` `TextBodyStrong` `TextBodySecondary` `TextBodySmall` `TextBodySmallStrong` `TextCaption` `TextCaptionMuted` `TextOverline` `TextMono` `TextMonoSmall` `TextWordmark` `TextWordmarkLarge`.
- **Ölçü:** `Space1…Space20`, `PagePadding` `CardPadding` `NavListMargin` `NavFooterPadding` `ButtonPadding` `ButtonPaddingSmall`, `Radius*`, `Window*`, `TitleBarHeight` `CaptionButtonWidth` `NavColumnWidth` `HomeInfoColumnWidth` `ControlHeight` `ControlHeightSmall` `NavItemHeight` `IconButtonSize` `StatusDotSize` `StatusBoxSize` `DeviceKeySize` `DeviceKeyGap` `SystemPanel*` `SystemGauge*` `Toast*`.
- **Hareket:** `Duration*`, `EaseOut` `EaseOutStrong` `EaseHover` `EaseInOut` `EaseDrawer` `EaseCinematic`, `KeySplineEaseOut` `KeySplineEaseInOut` `KeySplineDrawer`, `Motion*`, `FadeInUpStoryboard` `FadeInStoryboard` `FadeOutStoryboard`.
- **İkon:** `Icon*` Geometry'leri (§7), `IconBase` `Icon16` `Icon20` `Icon24` `IconLogoBase` `IconLogo16` `IconLogo20`.
- **Kontrol:** `FocusVisual` `FocusVisualCompact` `FocusVisualInset`, `SolidButtonTemplate` `GhostButtonTemplate`, `ButtonBaseStyle` `PrimaryButtonStyle` `SecondaryButtonStyle` `GhostButtonStyle` `IconButtonStyle` `CaptionButtonStyle` `CaptionCloseButtonStyle` `SipoWindowStyle` `NavListBoxStyle` `NavListBoxItemStyle` `QuietCheckBoxStyle` `ThinScrollBarStyle` (+örtük) `SipoToolTipStyle` (+örtük) `DividerHorizontalStyle` `DividerVerticalStyle` `SurfaceCardStyle` `EmptyStateStyle` `PageHostStyle`.
- **Durum:** `StatusIndicatorStyle` `StatusLabelStyle`.
- **Cihaz:** `DeviceVisualStyle` `DeviceKeyStyle` `DeviceKeyIndexTemplate` `DeviceKeyBlankTemplate` `DeviceKeyPlaceholders`.

---

## 13. Kod (.cs) gereksinimleri

Tema dosyaları .cs gerektirmez. Ekranların çalışması için kodlama ajanının yazacağı minimum kod:

1. **MainWindow.xaml.cs** — 4 `SystemCommands` `CommandBinding`'i (§10.a). Mevcut kapanış/tepsi davranışı korunur.
2. **App.xaml** — `Theme.xaml` birleştirmesi (§0).
3. **Açılış videosu** — tek yol sabiti (`Assets/Video/intro.mp4`), `File.Exists` kontrolü, `MediaEnded` döngüsü, `MediaFailed` → yer tutucu; csproj `Content` öğesi (§10.b).
4. **Açılış geçişi** — `IsSplashVisible`/`Splash` ViewModel alanları ve 340 ms sonra kaldırma (§10.g); kısa mod için 2400 ms `DispatcherTimer`.
5. **Enum → Türkçe metin** eşlemesi ViewModel'de (§9). Göstergeler enum'u doğrudan `Tag`'den okur.
6. (İsteğe bağlı) Azaltılmış hareket: `SystemParameters.ClientAreaAnimation == false` iken açılış Storyboard'u başlatılmaz, öğeler `Opacity=1` yapılır.
7. (İleride) sistem paneli yay converter'ı, toast yöneticisi.

---

## 14. Windows'ta doğrulanacaklar ve riskler

- `GlassFrameThickness="0,0,0,1"`: Win11'de gölge + yuvarlak köşe beklenir; bazı sürücülerde altta 1 px açık çizgi görülürse `0` yapılmalı.
- Büyütülmüş pencerede taşma düzeltmesi (`WindowResizeBorderThickness`) farklı DPI'lı çoklu monitörde kontrol edilmeli.
- Win11 "Snap Layouts" menüsü özel Büyüt düğmesinde çıkmaz (WM_NCHITTEST/HTMAXBUTTON gerektirir) — bilinçli olarak kapsam dışı.
- `Segoe UI Variable` WPF'te adlandırılmış örnek olarak çözülemezse otomatik `Segoe UI`'ye düşer (görünüm çok yakın). Inter kuruluysa o kullanılır; font dosyası pakete eklenmedi.
- `BlurEffect` (sahne ışığı) düşük donanımda yavaşsa `Radius` 60'a indirilebilir; `BitmapCache` zaten açık.
- "Bir daha gösterme" ve "ilk kurulum tamamlandı" bilgisinin kalıcı saklanması mevcut ayar/veri biçimine yeni alan eklemeyi gerektirir → **veri formatı değişikliği; kullanıcı onayı gerekir.** Onay yoksa 015'te bu tercih yalnızca oturum içi tutulur.
- Cihazın gerçek formu (video/foto) geldiğinde `DeviceVisual` ölçüleri (gövde oranı, tuş boyutu, 4×3/3×4) ona göre güncellenmeli.
- Brief'teki Deck düzenleme ekranı (017) bu dokümanda yalnızca `DeviceKeyStyle` seçim hâli ile hazırlandı; yerleşim spesifikasyonu o task'ta eklenecek.
