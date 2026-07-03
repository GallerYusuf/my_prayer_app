# Çalışma İlerleme Kaydı

Son güncelleme: 2026-07-03 (oturum 1)

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

## Devam eden

- **Android Debug build doğrulaması** — CS hataları giderildi (CS0104, CS0246); son build
  `obj` temizliği sonrası çalışıyor. XAML hataları çıkarsa düzeltilecek.

## Sıradaki kesin adımlar

1. Android Debug build'i yeşile çevir (XAML binding hataları muhtemel).
2. Testleri tekrar çalıştır, `docs/TEST_REPORT.md` doldur.
3. Commit: UI + bildirim fazları.
4. Release build + AAB (`dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormats=aab`),
   trimming sorunlarını çöz (Plugin.LocalNotification TrimmerRootAssembly zaten var).
5. Final rapor.

## Bilinen açık konular

- Meal lisans teyidi (CONTENT_SOURCES.md).
- Diyanet referans tablosu manuel doldurulacak (PRAYER_TIME_VALIDATION.md).
- Emoji glyph'leri vakit kartlarında dekoratif olarak duruyor (ROADMAP teknik borç).
- Widget hâlâ eski WidgetDataService yolunu kullanıyor (çalışır durumda).
