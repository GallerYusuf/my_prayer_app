# Karar Kaydı

## D1 — Vakit kaynağı: Aladhan calendar API + method=13 (Diyanet)
Aladhan, `method=13` ile Diyanet İşleri Başkanlığı parametrelerini uygular ve koordinat
tabanlı sorguyu destekler. Doğrulanmamış "resmî Diyanet API" veya scraping kullanılmadı.
Şehir adı yerine koordinat kullanılıyor; il merkezleri için sabit koordinat tablosu
(`Data/CityCoordinates.cs`) eklendi.

## D2 — SQLite: sqlite-net-pcl + bundle_green 2.1.11
EF Core mobil için ağır; sqlite-net hafif ve olgun. **Bilinen konu:** transitive
`SQLitePCLRaw.lib.e_sqlite3` paketi CVE-2025-6965 (SQLite < 3.50.2 aggregate memory corruption)
uyarısı veriyor ve sqlite-net'in SQLitePCLRaw 3.x destekli stabil sürümü henüz yok
(1.11 beta iOS sorunlu). Uygulama yalnızca kendi ürettiği parametreli SQL'i çalıştırdığı ve
dışarıdan SQL girdisi almadığı için risk kabul edildi ve `NuGetAuditSuppress` ile
belgelenerek bastırıldı. sqlite-net 1.11 stabil çıktığında yükseltilecek.

## D3 — Exact alarm izin modeli: yalnızca SCHEDULE_EXACT_ALARM
`USE_EXACT_ALARM` manifesto'dan kaldırıldı. Google Play, `USE_EXACT_ALARM`'ı yalnızca çekirdek
işlevi alarm/takvim olan uygulamalara tanır ve iade edilemez; namaz vakti uygulamaları için
`SCHEDULE_EXACT_ALARM` (kullanıcı onaylı, Android 12+) hem politika açısından güvenli hem de
yeterlidir. İzin reddedilirse Plugin.LocalNotification inexact alarma düşer; ayarlarda
kullanıcıyı izin ekranına götüren buton vardır.

## D4 — REQUEST_IGNORE_BATTERY_OPTIMIZATIONS kaldırıldı
Play politikasında hassas izin; AlarmClock modundaki exact alarm'lar Doze'da da çalıştığı için
teknik olarak gerekli değil. Bildirim tanılama ekranından genel ayarlara yönlendirme yapılmıyor;
izinsiz `ActionIgnoreBatteryOptimizationSettings` intent'i de kaldırıldı (gereksiz).

## D5 — allowBackup=false
Konum (koordinat) ve ayarlar cihaz-yerel tutuluyor; bulut yedeklemesi eski/yanlış konumun yeni
cihaza taşınmasına yol açabilir ve veriler saniyeler içinde yeniden üretilebilir. Bu yüzden
`android:allowBackup="false"` seçildi.

## D6 — Bildirim planlayıcı: Plugin.LocalNotification üzerine ince koordinatör
Plugin, AlarmManager tabanlı native scheduling, kalıcılık ve BOOT_COMPLETED/MY_PACKAGE_REPLACED
restore receiver'larını hazır veriyor. Sıfırdan AlarmManager yazmak yerine plugin korunup üstüne
deterministic ID + rolling plan + atomik değişim kuralları (`NotificationCoordinator`,
`NotificationPlanner`) eklendi. Eski 10 sn'lik foreground timer "teslimat" mekanizması kaldırıldı.

## D7 — Rolling schedule: 3 gün
Android'de bekleyen alarm sayısını düşük tutmak (sistem limitleri ve pil) ile uygulama günlerce
açılmasa bile bildirimlerin sürmesi arasında denge: bugün + 3 gün planlanır; uygulama açılışı,
resume, sistem olayları ve ayar değişimlerinde plan kaydırılır.

## D8 — Ayarlar: Preferences + immutable record, veri: SQLite
Küçük ayarlar için Preferences yeterli (görev tanımına uygun). Vakit cache'i, Kur'an,
favoriler ve zikir geçmişi SQLite'ta. Eski `app_settings` JSON'u ilk çalıştırmada yeni şemaya
migrate edilir (şehir → koordinat eşlemesi plaka koduyla yapılır).

## D9 — Kur'an verisi: ilk çalıştırmada SQLite import
Hazır üretilmiş DB dosyası dağıtmak yerine mevcut `quran_tr.json` korunarak ilk açılışta import
edildi; böylece içerik kaynağı tek (JSON) kalıyor ve APK boyutu artmıyor. Import ~1-2 sn,
arka plan thread'inde ve yalnızca bir kez.

## D10 — Tema
Zorla Dark kaldırıldı; System/Light/Dark kullanıcı tercihi (`ThemeManager`). Varsayılan System.

## D11 — Widget, namaz takibi ve Ramazan modu korundu
Mevcutta çalışan özellikler (Android widget, namaz takip streak'i, Ramazan kartı) kullanıcı
değeri taşıdığı için yeni mimariye taşındı, silinmedi.
