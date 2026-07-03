# Bildirim Mimarisi

## Bileşenler

| Bileşen | Katman | Görev |
|---|---|---|
| `NotificationPlanner` | Core (saf) | Cache'lenmiş günler + ayarlardan deterministic plan üretir |
| `NotificationIds` | Core (saf) | Tarih + vakit türünden benzersiz, tekrarlanabilir ID üretir |
| `NotificationCoordinator` | Core | Planı kurar, eski planı atomik değiştirir, günde bir yeniler |
| `INotificationScheduler` | Core contract | Platform soyutlaması |
| `LocalNotificationScheduler` | MAUI head | Plugin.LocalNotification implementasyonu |
| `SystemEventsReceiver` | Android | TIME_SET / TIMEZONE_CHANGED / exact-alarm izni değişimi |
| Plugin restore receiver | Plugin | BOOT_COMPLETED + MY_PACKAGE_REPLACED sonrası kayıtlı alarmları geri yükler |

## Notification ID şeması

```
Prayer      : 100_000_000 + DayNumber*10 + PrayerKind   (günde 6)
PreReminder : 200_000_000 + DayNumber*10 + PrayerKind
Verse       : 300_000_000 + DayNumber*10 + slot (0-4)
Friday      : 400_000_000 + DayNumber
```

Aynı gün + aynı vakit her zaman aynı ID'yi üretir → yeniden planlama idempotent'tir;
`NotificationIds.GetCategory(id)` ile kategori geri çözülür.

## Kanallar (Android)

| Kanal | Önem | İçerik |
|---|---|---|
| `channel_prayer_time` | High | Vakit girdi bildirimi |
| `channel_pre_reminder` | High | Vakitten X dk önce |
| `channel_verse` | Default | Günün ayeti |
| `channel_other` | Default | Cuma ve diğer hatırlatmalar |

Kullanıcı Android ayarlarından her kanalın sesini/önemini bağımsız yönetebilir.
Uygulama içinden ayrıca: vakit başına aç/kapat (Güneş ayrı), kaç dk önce, ses, titreşim,
ayet bildirimi saati.

## Zamanlama modeli

- `AndroidScheduleMode.AlarmClock` + `RtcWakeup`: Doze modunda dahi teslim edilir.
  Exact alarm izni yoksa plugin inexact moda düşer (bildirim yine gelir, dakikasal sapabilir).
- Payload bildirimin içinde taşınır (ayet metni dahil) → teslim anında internet gerekmez.
- Rolling window: bugün + 3 gün. Kaynak veri SQLite cache'i (35 gün) olduğundan planlama
  tamamen offline yapılabilir.

## Yeniden planlama tetikleyicileri

| Olay | Mekanizma |
|---|---|
| Uygulama açılışı | `MainViewModel` → `RescheduleIfStaleAsync` (günde bir) |
| Resume | `App.Window.Resumed` → `RescheduleIfStaleAsync` |
| Ayar değişikliği | `SettingsViewModel.Save` → `RescheduleAsync(force: true)` |
| Cihaz saati değişti | `SystemEventsReceiver` (TIME_SET) |
| Timezone değişti | `SystemEventsReceiver` (TIMEZONE_CHANGED) |
| Exact alarm izni değişti | `SystemEventsReceiver` (SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED) |
| Reboot | Plugin.LocalNotification restore receiver (BOOT_COMPLETED) |
| Uygulama güncellendi | Plugin restore receiver (MY_PACKAGE_REPLACED) |

## Atomiklik kuralı

Konum/ayar değişiminde yeni plan **başarıyla üretilmeden** eski bildirimler iptal edilmez
(`NotificationCoordinator.RescheduleAsync`: schedule fetch başarısızsa mevcut plan korunur ve
hata döndürülür). Böylece kullanıcı hiçbir zaman "bildirimsiz" durumda kalmaz.

## Ses

Telifsiz doğrulanmış ezan sesi bulunmadığından varsayılan sistem bildirim sesi kullanılır.
Kullanıcı ses/titreşimi ayrı ayrı kapatabilir. Lisansı doğrulanmış bir ezan asset'i temin
edilirse kanal sesi olarak eklenebilir (bkz. `docs/CONTENT_SOURCES.md`).

## iOS

`INotificationScheduler` soyutlaması sayesinde iOS implementasyonu aynı `PlannedNotification`
listesini alır. Plugin.LocalNotification iOS'ta UNUserNotificationCenter kullanır; iOS'un 64
bekleyen bildirim limiti, 3 günlük rolling window (~30 bildirim) ile aşılmaz. iOS testleri bu
görevde yapılmadı (macOS gerektirir) — bkz. bilinen sınırlamalar.
