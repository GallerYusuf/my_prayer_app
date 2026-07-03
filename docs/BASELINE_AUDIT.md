# Baseline Denetimi (Faz 0)

Tarih: 2026-07-03
Branch: `fable5/production-rebuild` (main üzerindeki WIP snapshot commit'i ile başlatıldı)

## Ortam

- .NET SDK 10.0.301, MSBuild 18.6.4
- MAUI workload'ları: `maui-windows`, `android` (36.1.43), `ios`, `maccatalyst` (VS 18.8 üzerinden MSI)
- Hedef framework'ler: `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`

## Mevcut yapı (tek proje)

- `PrayerApp.csproj` — tek MAUI projesi; katman ayrımı yok, test projesi yok, CI yok.
- Paketler: CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Http 10.0.9, Plugin.LocalNotification 14.1.1, Microsoft.Extensions.Logging.Debug 10.0.0.
- Ekranlar: `MainPage`, `SettingsPage`, `QiblaPage`, `CityPickerPage` (modal). Onboarding ekranı yok; ilk açılışta Ayarlar'a yönlendiriliyor.
- Servisler: `PrayerTimeService` (Aladhan `timingsByAddress`), `VerseService` (quran_tr.json tamamı belleğe), `SettingsService` (Preferences + JSON blob), `NotificationSchedulerService` (Plugin.LocalNotification + 10 sn'lik foreground timer), `QiblaService` (bearing matematiği doğru görünüyor), `WidgetDataService` (Android widget), `PrayerTrackingService`, `RamadanService`.
- `Data/TurkishCities.cs`: 81 il + ilçe listesi, arama/normalizasyon yardımcıları — korumaya değer.

## Baseline build sonuçları

- `dotnet restore PrayerApp.csproj`: BAŞARILI (~226 sn, soğuk cache).
- `dotnet build -f net10.0-android -c Debug`: sonuç bu dosyanın "Build günlüğü" bölümünde güncellenir.
- Test yok: depoda hiç test projesi bulunmuyor.

## Tespit edilen kritik sorunlar

### Depo hijyeni
1. **`.gitignore` yoktu**; `bin/` ve `obj/` (APK'lar dahil) git'e commit edilmişti. Düzeltildi: `.gitignore` eklendi, build çıktıları izlemeden çıkarıldı.
2. Placeholder package ID: `com.companyname.prayerapp`.
3. `Resources/Images/dotnet_bot.png` hâlâ depoda.

### Veri ve ağ
4. Namaz vakitleri her gün ilk açılışta internetten çekiliyor; **yalnızca bugünün verisi** Preferences içine cache'leniyor. 30 günlük offline takvim yok.
5. Konum, `timingsByAddress` ile **serbest metin şehir adı** üzerinden sorgulanıyor; calculation method hiç belirtilmiyor (Aladhan varsayılanına kalıyor), timezone yönetimi API'ye bırakılmış.
6. `quran_tr.json` (2.5 MB) her başlatmada komple parse edilip belleğe alınıyor.
7. Ayarlar tek bir JSON blob olarak Preferences'ta; SQLite yok, migration kavramı yok.

### Bildirimler
8. **10 saniyelik foreground dispatcher timer** "gecikmiş bildirimleri" uygulama açıkken elle gösteriyor — güvenilirlik çözümü değil.
9. Yalnızca bugünün vakitleri planlanıyor (rolling schedule yok); gece yarısından sonra ertesi gün planı ancak uygulama açılırsa oluşuyor.
10. Manifest'te **hem `SCHEDULE_EXACT_ALARM` hem `USE_EXACT_ALARM`** var (Play politikası riski) ve `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` (Play'de hassas izin) mevcut.
11. `TIME_SET` / `TIMEZONE_CHANGED` için receiver yok; cihaz saati veya dilim değişince planlar bozuk kalıyor.
12. Notification ID'ler indeks tabanlı (100+i) — tarih bilgisi taşımıyor, idempotency zayıf.

### Kod kalitesi
13. `DateTime.Now` her yerde; `IClock` soyutlaması ve test edilebilirlik yok.
14. Ham `ex.Message` birçok yerde doğrudan UI'a basılıyor (`ErrorMessage = ex.Message`, `"Şehir doğrulanamadı: {ex.Message}"`).
15. `App.RefreshNotificationsIfNeededAsync` içinde sessizce yutulan `catch { }` bloğu.
16. Emoji glyph'leri (🌙 ☀️ vb.) ana ikon sistemi olarak kullanılıyor.
17. UI Türkçe metinleri kodda hardcoded; `.resx` localization altyapısı yok.
18. Tema zorla Dark: `UserAppTheme = AppTheme.Dark` — light/system desteği fiilen kapalı.
19. `MainViewModel` ~460 satır; takip, Ramazan, widget, ayet, vakit sorumluluklarını tek sınıfta topluyor.
20. Test, CI, release ve mağaza dokümantasyonu yok.

## Korunmaya değer parçalar

- `TurkishCities` il/ilçe verisi ve normalize arama.
- `QiblaService` bearing/distance matematiği (unit teste alınacak).
- Kart tabanlı koyu yeşil/lacivert/altın tasarım dili ve mevcut SVG tab ikonları.
- Widget entegrasyonu (`PrayerAppWidget`).
- Aladhan cache fallback fikri (SQLite'a taşınacak).
- Shuffle-bag ayet seçimi (gün-sabit ayet mantığıyla birleştirilecek).

## Build günlüğü

| Adım | Sonuç |
|---|---|
| restore | OK (~226 sn, soğuk cache) |
| Android Debug (baseline) | BAŞARILI — 0 hata, 20 uyarı (CA1416 API level, CS8602 null, CS0618 MainPage deprecation), süre 17:56 (soğuk) |
| Test (baseline) | Test projesi yok |
| Android Release (baseline) | Faz 8'de çalıştırılacak |

Baseline uyarı özeti:
- `CA1416`: API 21 min iken API 23/26 API'leri guard'sız çağrılıyor (battery optimization, notification settings).
- `CS8602/CS8765`: Widget ve servislerde null güvenliği eksikleri.
- `CS0618`: `Application.MainPage` kullanımı deprecated.
- `CS4014`: await edilmeyen async çağrı (`RequestExactAlarmsPermission`).
