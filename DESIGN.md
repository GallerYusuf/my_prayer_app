# Ezan Vakti — Tasarım Sistemi ve Bilgi Mimarisi (`DESIGN.md`)

Bu doküman, **Ezan Vakti** (.NET 10 MAUI) uygulamasının yenilenen ürün mimarisini, ekran hiyerarşisini, açık/koyu tema yüzey kurallarını ve etkileşim ilkelerini tanımlar.

---

## 1. Ürün İlkeleri ve Bilgi Mimarisi

Uygulama, özellik listesi yerine kullanıcının günlük ibadet akışı etrafında **5 ana sekme** ve her yerden erişilebilen ortak **Ayarlar** girişiyle düzenlenmiştir:

| Sekme | Rota | Temel Kullanıcı Sorusu / Görevi | İçerik ve Akış |
|---|---|---|---|
| **Bugün** | `MainPage` | *"Sıradaki vakit ne zaman, bugün hangi vakitleri kıldım?"* | Doğrudan şehir seçici açan üst başlık, birleşik geri sayım kartı (Ramazan'da iftar/sahur öncelikli), 6 vaktin ve 5 farz namaz için "Kıldım" işaretinin tek bakışta görüldüğü liste, Kur'an/Kıble/Zikir hızlı devam şeridi ve sakin Günün Ayeti kartı. |
| **Kur'an** | `QuranPage` | *"Kur'an okumaya kaldığım yerden devam edeyim."* | Kaldığın yer + Hatim ilerleme özeti üst barı; Sureler (Mushaf/Nüzul), Cüzler, Konular (Fihrist) ve Favoriler sekmeleri; Sure içinde bağımsız Arapça/Meal yazı boyutu, Odak (dikkat dağıtmayan okuma) modu, çevrimdışı/internet durumu netleştirilmiş sesli tilavet ve tek dokunuşla hatim sayfası güncelleme. |
| **İbadet** | `WorshipPage` | *"Zikir çekeyim, namaz ve kaza takibimi yapayım, hatmimi yöneteyim."* | **Zikirmatik** (tek el uyumlu büyük sayaç, `-1` ve sıfırlama geri alma, 33×3 namaz tesbihatı), **Namaz & Kaza Takibi** (baskı kurmayan günlük ilerleme, kaza sayacı ve son işlemi geri alma), **Hatim Planı** (30/60/90 gün veya serbest hedef). |
| **Takvim** | `CalendarPage` | *"Seçtiğim günün vakitleri ne, yaklaşan kandil ve bayramlar hangileri?"* | Vakit Takvimi ve Dini Günler aynı merkezde: Hızlı tarih seçici (`DatePicker`), seçili gündeki dini gün rozeti, yaklaşan kandil/bayram listesi (dokununca o günün vakitlerine geçiş) ve Miladi → Hicri tarih çevirici. |
| **Keşfet** | `ExplorePage` | *"Kıbleyi bulayım, dua veya Esmaül Hüsna okuyayım, zekât hesaplayayım."* | Kıble Pusulası (kalibrasyon ve sensör durumuyla), Kur'an'dan Dualar (arama ve favorilerle), Esmaül Hüsna (arama, favoriler ve zikirmatikte başlatma ile), Zekât Hesaplayıcı ve Ayarlar/Yedekleme erişimi. |

---

## 2. Görsel Kimlik ve Renk Sistemi (`Resources/Styles/Colors.xaml`)

Aşırı parlama, ağır gölgeler ve rastgele çok renkli gradyanlar yerine; dinginlik, yüksek okunabilirlik ve ince kenarlıklarla ayrılan katmanlı yüzeyler kullanılmıştır.

### Açık Tema (Sıcak Kağıt & Zümrüt)
- **Uygulama Arka Planı (`AppBackground`)**: `#F5F7F5` (Gözü yormayan sıcak taş/kağıt tonu)
- **Birincil Kart Yüzeyi (`CardSurface`)**: `#FFFFFF`
- **İkincil / Gömülü Yüzey (`CardSurfaceAlt`)**: `#EBF0EC`
- **Kenarlık / Ayırıcı (`HairLine`)**: `#D8E2DC`
- **Metin (`TextPrimary` / `TextSecondary`)**: `#111C18` / `#52635B`

### Koyu Tema (Derin Gece Yeşili & Yumuşak Altın)
- **Uygulama Arka Planı (`AppBackgroundDark` / `BackgroundDeep`)**: `#0B1411`
- **Birincil Kart Yüzeyi (`CardSurfaceDark` / `BackgroundMid`)**: `#13211C`
- **İkincil / Gömülü Yüzey (`CardSurfaceAltDark`)**: `#1A2C25`
- **Kenarlık / Ayırıcı (`HairLineDark`)**: `#243B32`
- **Metin (`TextPrimaryDark` / `TextSecondaryDark`)**: `#F1F5F3` / `#99ADA4`

### Vurgu ve Anlam Renkleri
- **Zümrüt (`AccentEmerald` / `AccentEmeraldDeep`)**: `#10B981` / `#059669` — Aktif vakit, tamamlanan ibadet, birincil ilerleme.
- **Altın (`AccentGold` / `AccentGoldMuted`)**: `#D4AF37` / `#9E7B1C` (Açık temada kontrast için koyulaştırılmış altın, koyu temada sıcak altın) — Bölüm etiketleri, saat vurguları, özel dini günler.
- **Durum Renkleri**: Uyarı/Kerahat `#D97706` (Kehribar), Hata `#EF4444`, Başarı `#10B981`.

---

## 3. Tipografi ve Boşluk Kuralları (`Resources/Styles/Styles.xaml`)

- **Yazı Tipleri**:
  - Arayüz ve Türkçe metinler: `OpenSansRegular` ve `OpenSansSemibold`
  - Kur'an ve Dua Arapça metinleri: `AmiriQuran` (`LineHeight="1.75"`, sağdan sola akış)
- **Ölçek**:
  - `DisplayHero` (`44pt Semibold`): Tekil geri sayım ve Zikirmatik ana sayacı. Sabit satır yüksekliği ve zıplamayan yerleşim.
  - `DisplayTitle` (`24pt Semibold`): Ekran üst başlıkları.
  - `SectionLabel` (`11pt Semibold`, `CharacterSpacing="1.5"`): Grup ve bölüm başlıkları.
  - `Body` (`14pt Regular`, `LineHeight="1.45"`): Meal, açıklamalar ve ayar satırları.
  - `Caption` (`12pt Regular`): Tarih, kaynak ve durum bilgileri.
- **Boşluk ve Köşe Yarıçapları**:
  - Sayfa kenar boşluğu: `16dp` yatay, `12dp–24dp` dikey.
  - Kart köşe yarıçapı: Ana kartlar `18dp`, satır/giriş bileşenleri `12dp`, bilgi rozetleri (`InfoChip`) `16dp`.
  - Dokunma hedefleri: Tüm etkileşimli buton ve satırlarda en az `44x44dp`.

---

## 4. Etkileşim ve Güvenilirlik İlkeleri

Kur'an sure okuma araçları dar telefonlarda iki satıra yerleşir: okuma konumu ve ses üstte, odak ve görünüm altta. Bu araçlarda ortak `ReaderToolChip` stili en az 44dp dokunma yüksekliği sağlar. Önceki/Sonraki Sure geçişi yeni surenin ilk ayetine kaydırır.

Kur'an, İbadet ve Takvim iç bölümlerinde aynı `SectionTabButton` stili kullanılır. Bölümler gerçek MAUI düğmeleridir; en az 44dp dokunma yüksekliği, basılma durumu ve açık/koyu temada görünür seçili durum taşır. Kur'an devam ve hatim işlemleri dar telefonlarda ayrı satırlara yerleşir; uzun hatim metni okuma düğmesini sıkıştırmaz. Bu ortak bileşenin kaynağı `Resources/Styles/Styles.xaml`, ekran uygulamaları ilgili `Views/*.xaml` dosyalarıdır.

1. **Tekrarlanan Bilgi Yok**: Bugün ekranında geri sayım ve vakit listesi yalnızca bir kez, en anlaşılır biçimde gösterilir.
2. **Durum Şeffaflığı**: Çevrimdışı modda veya önbellek verisi kullanılırken verinin hangi tarihe ait olduğu açıkça gösterilir; veri yokken yanıltıcı sayaç çalıştırılmaz.
3. **Geri Alınabilir Eylemler (Undo)**: Zikirmatikte `-1` ve sıfırlamayı geri alma; Kaza takibinde son yapılan değişikliği geri alma düğmesi bulunur.
4. **Anında Kayıt**: Ayarlar ekranındaki değişiklikler anında kaydedilir ve kullanıcıya görünür geri bildirim verilir.
