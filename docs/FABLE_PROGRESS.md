# Çalışma İlerleme Kaydı

Son güncelleme: 2026-07-06 (oturum 2 — `fable5/autonomous-evolution`)

## Oturum 2 — Otonom ürün evrimi

Ayrıntı: `docs/FABLE_AUTONOMOUS_EVOLUTION.md`

- Yarım kalan sürekli vakit bildirimi tamamlandı, 7 koordinatör unit testi eklendi (66 test yeşil).
- Widget alarm-tabanlı yeniden yazıldı (donmuş geri sayım hatası giderildi; native Chronometer).
- Emoji glyph'ler 10 parçalık vector ikon setiyle değiştirildi.
- Build uyarıları sıfırlandı; repo hijyeni (crash logları git'ten çıkarıldı).

## Tamamlanan fazlar

- **Faz 0** — Baseline audit (`docs/BASELINE_AUDIT.md`), `.gitignore`, bin/obj git'ten çıkarıldı,
  branch `fable5/production-rebuild`, baseline Android Debug build BAŞARILI (0 hata / 20 uyarı).
- **Faz 1-3** — `src/PrayerApp.Core`, `src/PrayerApp.Infrastructure`, iki test projesi,
  solution (`PrayerApp.slnx`). Aladhan koordinat+method provider, SQLite prayer cache,
  Quran SQLite import, Dhikr repo, NotificationPlanner/Coordinator, NextPrayerCalculator,
  QiblaCalculator, VerseOfDaySelector. Testler: 34 unit + 25 integration YEŞİL.
  Commit: `feat: add Core/Infrastructure layers with offline prayer repository and tests`
- **Faz 4-6 (kod)** — MAUI head yeniden yazıldı: MauiProgram DI, App/AppShell, SettingsStore
  (legacy migration'lı), LocalNotificationScheduler (4 kanal), LocationService, ThemeManager,
  SystemEventsReceiver (TIME_SET/TIMEZONE), manifest temizliği (USE_EXACT_ALARM ve
  battery-opt izni kaldırıldı, konum izinleri eklendi, allowBackup=false), package ID
  `com.galleryusuf.ezanvakti` (Directory.Build.props). Yeni ekranlar: Onboarding, Calendar,
  Quran, Surah, Dhikr; Main/Settings/Qibla yeniden bağlandı. dotnet_bot.png silindi.
  Eski servisler (PrayerTimeService, VerseService, NotificationSchedulerService, ...) kaldırıldı.

## Faz 7-8 tamamlandı

- Android Debug build: ✅ 0 hata (temiz obj sonrası).
- Testler: 34 unit + 25 integration ✅ (`docs/TEST_REPORT.md`).
- Release AAB: ✅ `com.galleryusuf.ezanvakti-Signed.aab` üretildi (`PublishTrimmed=false` ile;
  ILLink bu ortamda sandbox engeli nedeniyle çalışmadı — CI'da trimmed publish var).
- CI: `.github/workflows/ci.yml` (test + Android debug/release + artifact).
- Dokümantasyon seti tamamlandı (docs/ altındaki tüm dosyalar + README + CHANGELOG).
- Commit'ler: baseline snapshot, gitignore, core katmanları, app rebuild, docs/release.

## Yeni oturumda devam edilecekse

1. `git log --oneline` ve bu dosyayı oku.
2. Kalan işler için `docs/ROADMAP.md` "Teknik borç" bölümüne bak.
3. Cihaz üzerinde `docs/TEST_REPORT.md` manuel senaryolarını ve
   `docs/NOTIFICATION_TESTING.md` ADB testlerini uygula.

## Bilinen açık konular

- Meal lisans teyidi (CONTENT_SOURCES.md).
- Diyanet referans tablosu manuel doldurulacak (PRAYER_TIME_VALIDATION.md).
- ~~Emoji glyph'leri~~ → oturum 2'de vector ikonlarla değiştirildi.
- ~~Widget eski WidgetDataService yolu~~ → oturum 2'de alarm-tabanlı yeniden yazıldı.
