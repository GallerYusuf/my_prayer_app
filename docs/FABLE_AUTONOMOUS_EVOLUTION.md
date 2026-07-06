# Fable 5 — Otonom Ürün Evrimi Kaydı

Tarih: 2026-07-06 · Branch: `fable5/autonomous-evolution` (taban: `fable5/production-rebuild`)

---

# Oturum 3 — Rakip analizi ve içerik genişlemesi (2026-07-06 öğleden sonra)

Kullanıcı, rakip bir "Pro" namaz uygulamasının ekran görüntülerini paylaştı ve uygun
özelliklerin eklenmesini, özellikle Kur'an bölümünün üst seviyeye taşınmasını istedi
(kapsam onayı: 3 fazın tamamı; Amiri font paketlensin; hatim sayfa bazlı).

## Kritik keşif

Paketli `quran_tr.json` dosyası zaten her ayetin **Arapça metnini** içeriyordu; eski import
bunu atıyordu. Arapça görünüm sıfır ek veri maliyetiyle eklendi.

## Faz 1 — Kur'an yükseltmesi

- **Arapça metin:** Import v2 Arapça + mushaf sayfası + cüz kolonlarını doldurur; mevcut
  kurulumlar sürüm işaretiyle otomatik yeniden import eder, favoriler korunur (testli).
  Sure ekranında Arapça (AmiriQuran fontu, RTL) + meal; Arapça aç/kapat ve A−/A+ yazı boyutu.
- **Sayfa/cüz meta verisi:** Tanzil `quran-data.xml` → `QuranMetadata.cs` (604 sayfa, 30 cüz).
- **Kaldığın yerden devam:** ayet bazlı konum (scroll takibi), Kur'an ekranında devam kartı.
- **Cüz listesi:** 30 cüz, başlangıç suresi/ayeti ve sayfa aralığıyla; dokununca açılır.
- **Hatim takibi (sayfa bazlı):** 30/60/90 gün veya serbest plan; sayfa işaretleme, ilerleme
  yüzdesi, günlük okunan/hedef sayfa, plan geçmişi (SQLite `khatm_plans` + `khatm_log`).
- **Kur'an'dan Dualar:** 26 küratif dua (sadece referans listesi; metin kendi DB'mizden).

## Faz 2 — İçerik modülleri

- **Kerahat vakitleri:** `KerahatCalculator` (Core, saf) + ana ekranda kerahat chip'i.
- **Dini Günler:** `ReligiousDaysCalculator` (UmAlQura tabanlı, ±1 gün notu ekranda),
  12 aylık liste + Miladi→Hicri çevirici.
- **Esmaül Hüsna:** 99 isim (Arapça + anlam), arama destekli ekran.

## Faz 3 — Araçlar

- **Kaza takibi:** 5 vakit + vitir + oruç sayaçları (±1/±10), cihazda saklanır.
- **Zekatmatik:** offline hesaplayıcı (nisap 80,18 g altın, %2,5); fiyatlar elle girilir
  (fiyat servisi yok — gizlilik ilkesi), girilen fiyatlar hatırlanır.
- **Tesbihat:** Zikirmatik'te rehberli 33×3 akış (otomatik adım geçişi, titreşim).
- Ana ekran hızlı erişim grid'i 8 modüle genişletildi; 3 yeni ikon (dua, sayaç, para).

## Bilinçli alınmayanlar (rakipten)

Canlı yayın, Kur'an radyosu, multimedya, Dua Kardeşliği (sosyal), Yakın Camiler/Mescit
(Places API + konum), Haftanın Hutbesi (sunucu içeriği), tebrik kartı, reklam kaldırma,
camide otomatik sessize alma (DND izni), Kur'an Ezberle/ses (yüzlerce MB asset veya
streaming). Gerekçe: gizlilik-önce, offline-first, sunucusuz ürün kimliği.

## Veri güvenliği

- DB şema v2 migration'ı: `CreateTableAsync` mevcut tabloya kolon ekler; favoriler, zikir
  geçmişi ve vakit cache'i korunur. Kur'an içeriği import-sürümü anahtarıyla yeniden yüklenir.
- Yeni veriler (hatim, kaza, zekât fiyatları) yalnızca cihazda.

---

## 1. İncelenen mevcut durum

- Git geçmişi, `docs/` seti (FABLE_PROGRESS, BASELINE_AUDIT, ROADMAP, DECISIONS,
  NOTIFICATION_ARCHITECTURE, TEST_REPORT), tüm ekranlar, ViewModel'ler, servisler,
  Core/Infrastructure katmanları, test projeleri, manifest ve CI incelendi.
- Başlangıç doğrulaması: 34 unit + 25 integration test YEŞİL; son Android Debug build
  başarılı (11 uyarı). Cihaz bağlı değil (adb yok) — cihaz testleri manuel listeye ayrıldı.
- Çalışma alanında commit edilmemiş, yarım bir "sürekli vakit bildirimi" işi bulundu
  (5 değişik dosya + 3 yeni dosya). Kod incelendi, tamamlanabilir ve mimariye uygun görüldü.

## 2. Bulunan fırsatlar ve puanlama

Değerlendirme ölçütleri: kullanıcı etkisi, kullanım sıklığı, güvenilirlik katkısı,
teknik risk, maliyet, mimariyle uyum, mağaza yayınına katkı.

| Fırsat | Etki | Maliyet | Karar |
|---|---|---|---|
| Yarım kalan sürekli bildirim işinin tamamlanması + koordinatör testleri | Yüksek (namaz uygulamalarının temel beklentisi) | Düşük (iş %90 hazırdı) | ✅ Yapıldı |
| Widget'ın uygulama kapalıyken donması (yanlış geri sayım göstermesi) | Çok yüksek (güven kırıcı, her gün görülür) | Orta | ✅ Yapıldı |
| Emoji glyph'lerin vector ikonlarla değişimi | Orta-yüksek (mağaza kalitesi, cihazlar arası tutarlılık) | Orta | ✅ Yapıldı |
| Build warning'lerinin sıfırlanması | Orta (bakım kalitesi) | Düşük | ✅ Yapıldı |
| Repo hijyeni (JVM crash logları, run_command git'te) | Düşük-orta | Çok düşük | ✅ Yapıldı |
| Aylık takvim grid görünümü | Orta | Orta | ❌ v1.1'e bırakıldı (gün gezinme mevcut ve çalışıyor) |
| MainViewModel'in alt-VM'lere bölünmesi | Düşük (kullanıcıya görünmez) | Orta-yüksek risk | ❌ Yapılmadı (çalışan davranışı bozma riski > kazanç) |
| İngilizce localization | Orta | Yüksek | ❌ v1.1 (ana pazar TR; .resx altyapısı hazır) |
| Ezan sesi | Yüksek | Bloklu | ❌ Lisanslı asset yok (CONTENT_SOURCES) |
| Namaz istatistik ekranı | Orta | Orta | ❌ v1.2 (takip + streak zaten var) |

## 3. Yapılan geliştirmeler

### A. Sürekli vakit bildirimi (Android) — tamamlandı ve test edildi
- Ongoing (kaydırılamaz), sessiz, Low-importance bildirim; günün 6 vakti + sıradaki vakit.
- Kendini yeniden kuran tek exact alarm (vakit anı / gece yarısı); foreground service yok.
- Ayarlar > Bildirimler'den açılıp kapanabilir (varsayılan açık).
- `NotificationCoordinator`'a 7 yeni unit test eklendi (fresh/stale yol, atomiklik,
  kategori kapalıyken sürekli bildirimin yaşaması, fetch hatasında eski planın korunması).

### B. Widget güvenilirliği — alarm tabanlı yeniden yazım
- **Eski davranış:** Widget yalnızca uygulama açıkken (55 sn'de bir) güncelleniyordu;
  uygulama kapanınca geri sayım donuyor ve saatlerce yanlış değer gösteriyordu.
- **Yeni davranış:** Widget, sürekli bildirimle aynı alarm zincirinden beslenir ve
  SQLite cache'ten çizilir. Geri sayım API 24+'ta native `Chronometer` (count-down) ile
  sistem tarafından işletilir — uygulama süreci ölü olsa bile saniyesi saniyesine akar.
  API 21-23'te geri sayım gizlenir, vakit saati gösterilir.
- Widget'a günün 6 vaktinin tamamı eklendi; `WidgetDataService`/`IWidgetDataService`
  ve MainViewModel'deki widget güncelleme yolu tamamen kaldırıldı.

### C. Emoji glyph'lerin vector ikon setiyle değişimi
- 10 yeni stroke tabanlı SVG (`icon_fajr/sunrise/dhuhr/asr/maghrib/isha/crescent/pin/flame/search`).
- Vakit kartları, detay listesi, namaz takibi, bilgi chip'leri, kıble merkezi ve şehir
  seçici arama/boş durumu artık ikon kullanıyor; cihazlar arası tutarlı görünüm.
- `PrayerIcons` eşlemesi tek noktada; `PrayerTime.Glyph`/`PrayerTrackItem.Glyph` → `Icon`.

### D. Kalite temizliği
- `DisplayAlert` → `DisplayAlertAsync` (CS0618), widget null-güvenliği (CS8765/CS8602),
  `PendingIntentFlags.Immutable` API 23 guard'ı (CA1416).
- Git'ten çıkarıldı: `hs_err_pid*.log`, `replay_pid*.log`, `run_command` (+ .gitignore).

## 4. Teknik kararlar

- **Tek alarm zinciri, iki yüzey:** Sürekli bildirim ve widget aynı anlarda (vakit anı,
  gece yarısı) değiştiği için tek `PersistentNotificationService.RefreshAsync` yolu ikisini
  birden tazeler; ayrı zamanlayıcı/servis eklenmedi.
- **Foreground service bilinçli kullanılmadı:** Play politikası + pil etkisi; ongoing
  notification + exact alarm aynı sonucu daha ucuza veriyor.
- **Chronometer count-down (API 24+):** Uygulamasız canlı geri sayımın tek dürüst yolu;
  eski API'lerde geri sayım gösterilmez (yanlış değer göstermekten iyidir).
- **Varsayılan açık sürekli bildirim:** Türkiye'deki muadil uygulamaların temel beklentisi;
  sessiz/Low kanal olduğu için rahatsızlık vermez, tek dokunuşla kapanır.

## 5. Test ve build durumu

| Doğrulama | Sonuç |
|---|---|
| Unit testler (41; 7 yeni koordinatör testi dahil) | ✅ BAŞARILI |
| Integration testler (25) | ✅ BAŞARILI |
| Android Debug build | ✅ 0 hata, **0 uyarı** (önceki durum: 11 uyarı) |
| Android Release AAB (`PublishTrimmed=false -p:RunAOTCompilation=false`) | ✅ `com.galleryusuf.ezanvakti-Signed.aab` üretildi |

Not: Bu ortamda ILLink sandbox engeli nedeniyle trimmed publish çalıştırılamıyor
(önceki oturumla aynı bilinen kısıt); trimmed Release CI'da doğrulanır. Ayrıca
`PublishTrimmed=false` iken `RunAOTCompilation=false` gerektiği tespit edilip
RELEASE_GUIDE'a not düşüldü.

## 6. Gerçek cihazda doğrulanması gerekenler

1. Sürekli bildirim: açılışta görünmesi, vakit girince başlığın değişmesi, gece yarısı
   yeni güne geçmesi, ayardan kapatınca kaybolması, reboot sonrası geri gelmesi.
2. Widget: ekleme anında dolması, geri sayımın uygulama kapalıyken akması, vakit girince
   sıradaki vaktin değişmesi (alarm +5 sn), gün dönümü, reboot sonrası doğruluk.
3. İkonların light/dark temada ve düşük yoğunluklu ekranlarda görünümü.
4. Exact alarm izni kapalıyken zincirin inexact alarma düşerek çalışmaya devam etmesi.
5. Pil tasarrufu (Doze) altında bildirim ve widget güncellemeleri (MIUI vb. üreticiler).

## 7. Kalan geliştirme alanları

- Aylık takvim grid'i, İngilizce localization, istatistik ekranı (ROADMAP v1.1/v1.2).
- Meal lisans teyidi ve Diyanet referans tablosu (yayın öncesi manuel işler).
- MainViewModel'in alt-VM'lere bölünmesi (davranış değişikliği olmadan, ayrı bir bakım turu).

## Oturum devam bilgisi (bağlam sınırı protokolü)

- Branch: `fable5/autonomous-evolution`
- Commit'ler: hijyen → sürekli bildirim (+testler) → widget → ikonlar → uyarı düzeltmesi → docs
- Çalıştırılan komutlar ve sonuçları:
  - `dotnet test tests/PrayerApp.UnitTests` → 41/41 ✅
  - `dotnet test tests/PrayerApp.IntegrationTests` → 25/25 ✅
  - `dotnet build PrayerApp.csproj -f net10.0-android -c Debug` → 0 hata / 0 uyarı ✅
  - `dotnet publish ... -c Release -p:AndroidPackageFormats=aab -p:PublishTrimmed=false -p:RunAOTCompilation=false` → AAB üretildi ✅
- Durum: TAMAMLANDI. Kalan işler yalnızca gerçek cihaz doğrulamaları (bölüm 6) ve
  ROADMAP'teki yayın öncesi manuel maddeler.
