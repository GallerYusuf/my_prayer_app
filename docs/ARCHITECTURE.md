# Mimari

## Katmanlar

```
PrayerApp (MAUI head — kök dizin)
 ├─ Views/            XAML sayfaları (iş mantığı yok)
 ├─ ViewModels/       CommunityToolkit.Mvvm tabanlı VM'ler
 ├─ Services/         Platform servis implementasyonları (SettingsStore,
 │                    LocalNotificationScheduler, LocationService, ThemeManager, Widget)
 ├─ Platforms/        Android/iOS platform kodu (receiver'lar, manifest)
 └─ Data/             Türkiye il/ilçe listesi + il merkez koordinatları

src/PrayerApp.Core (net10.0 — platform bağımsız)
 ├─ Common/           Result<T>, AppError, IClock
 ├─ Domain/           PrayerDay, PrayerKind, GeoLocation, CalculationMethod,
 │                    PrayerAdjustments, UserSettings
 ├─ Contracts/        IPrayerTimeProvider, IPrayerDayRepository, IQuranRepository,
 │                    IDhikrRepository, ISettingsStore, INotificationScheduler, IKeyValueStore
 ├─ Notifications/    NotificationPlanner (saf plan üretici), NotificationIds
 └─ Services/         PrayerScheduleService, NotificationCoordinator,
                      NextPrayerCalculator, QiblaCalculator, VerseOfDaySelector

src/PrayerApp.Infrastructure (net10.0)
 ├─ Database/         AppDatabase (sqlite-net, PRAGMA user_version migration)
 ├─ Providers/        AladhanPrayerTimeProvider (koordinat + method paramlı calendar API)
 └─ Repositories/     PrayerDayRepository, QuranRepository, DhikrRepository

tests/PrayerApp.UnitTests          Core saf mantık testleri (xUnit)
tests/PrayerApp.IntegrationTests   SQLite (temp dosya) + FakeHttpHandler testleri
```

## Temel akışlar

### Vakit verisi (offline-first)
1. `PrayerScheduleService.GetScheduleAsync(from, count)` çağrılır.
2. SQLite'ta bugünden itibaren 7+ gün varsa doğrudan cache'ten döner (ağ çağrısı yok).
3. Cache inceyse Aladhan `calendar/{y}/{m}?latitude&longitude&method=13` ile ~35 gün çekilir,
   SQLite'a atomik yazılır, 7 günden eski kayıtlar temizlenir.
4. Ağ hatasında mevcut cache ile devam edilir (`IsFromCache=true`); cache yoksa tipli `AppError` döner.
5. Manuel dakika düzeltmeleri okuma anında uygulanır (ham veri cache'te bozulmaz).

### Bildirim planlama
`NotificationCoordinator.RescheduleAsync()`:
1. Ayarları okur; onboarding tamamlanmadıysa çıkar.
2. Yerel cache'ten bugün + 3 günü alır (internet gerektirmez).
3. `NotificationPlanner.BuildPlan` deterministic ID'li (tarih+vakit türevli) plan üretir.
4. Yeni plan üretilemezse mevcut plan korunur (atomik değişim).
5. Plan platform scheduler'ına verilir (Plugin.LocalNotification → AlarmManager, AlarmClock modu).
6. Planlanan ID listesi Preferences'a yazılır; sonraki planlamada eskiler iptal edilir.

Yeniden planlama tetikleyicileri: uygulama açılışı/resume (günde bir), ayar kaydı (force),
`TIME_SET`/`TIMEZONE_CHANGED`/exact-alarm izin değişimi (`SystemEventsReceiver`),
reboot + uygulama güncellemesi (Plugin.LocalNotification'ın kendi restore receiver'ı).

### Kur'an verisi
İlk erişimde `quran_tr.json` (2.5 MB) bir kez SQLite'a import edilir (arka plan thread).
Sonraki tüm erişimler indeksli SQL sorgusudur; ~6.2K ayet artık bellekte tutulmaz.
Günün ayeti `VerseOfDaySelector` ile tarihten deterministic üretilir (gün boyu sabit, offline çalışır).

## İlkeler

- ViewModel'lerde doğrudan `DateTime.Now` yok; `IClock` enjekte edilir.
- Kullanıcıya yalnızca `AppError.Message` (Türkçe) gösterilir; teknik istisnalar loglanır.
- Platform API çağrıları `Services/` veya `Platforms/` altında; Core hiçbir MAUI tipine bağımlı değildir.
- Ayarlar immutable `UserSettings` record'u; her okumada `Sanitized()` ile bozuk veri onarılır.
- Localization: `Resources/Localization/AppStrings.resx` (varsayılan Türkçe); yeni diller
  `AppStrings.{culture}.resx` dosyalarıyla eklenir.
