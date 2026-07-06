# Ezan Vakti

Namaz vakitleri, kıble pusulası, Kur'an meali ve zikirmatik içeren; **offline-first**,
reklamsız ve hesap gerektirmeyen .NET MAUI uygulaması. Ana dil Türkçe'dir; `.resx` tabanlı
localization altyapısıyla yeni diller eklenebilir.

## Özellikler

- **Namaz vakitleri** — Aladhan API, Diyanet (method=13) hesabı, koordinat tabanlı sorgu,
  35 günlük SQLite cache, vakit başına manuel dakika düzeltmesi, hicri tarih, Ramazan modu
- **Güvenilir bildirimler** — AlarmManager tabanlı exact alarm, uygulama kapalıyken çalışır,
  reboot/saat/timezone değişiminde yeniden planlama, 4 ayrı bildirim kanalı, rolling 3 günlük plan
- **Sürekli vakit bildirimi** — günün vakitleri ve sıradaki vakit bildirim çubuğunda sabit
  durur (sessiz, kapatılabilir); her vakit girişinde ve gece yarısında kendini günceller
- **Kıble pusulası** — gerçek sensör verisi, Kabe mesafesi, sensörsüz cihazlar için açıklamalı fallback
- **Kur'an meali** — 114 sure SQLite'ta, arama, favoriler, paylaşma, günün ayeti (gün boyu sabit)
- **Zikirmatik** — hazır/özel zikirler, hedef, titreşim, günlük geçmiş
- **Offline-first** — internetsiz açılış, cache göstergesi, sakin çevrimdışı durumu
- **Tema** — Light / Dark / System
- **Android ana ekran widget'ı** — günün 6 vakti, sıradaki vakit ve uygulama kapalıyken de
  işleyen native geri sayım (Chronometer); vakit girişlerinde alarm zinciriyle kendini yeniler
- Namaz takibi ve streak

## Mimari

Katmanlı yapı: `PrayerApp` (MAUI UI) → `src/PrayerApp.Core` (domain + saf mantık) →
`src/PrayerApp.Infrastructure` (SQLite, HTTP provider). Ayrıntı: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

## Gereksinimler

- .NET SDK 10.0.3xx
- MAUI Android workload: `dotnet workload install maui-android`
- Android SDK (API 35+) + JDK 17
- Windows'ta VS 2026 veya CLI

## Kurulum ve çalıştırma

```powershell
git clone <repo>
cd my_prayer_app
dotnet restore PrayerApp.csproj

# Bağlı cihaz/emülatöre kur ve çalıştır:
dotnet build PrayerApp.csproj -f net10.0-android -c Debug -t:Run
```

## Testler

```powershell
dotnet test tests/PrayerApp.UnitTests/PrayerApp.UnitTests.csproj
dotnet test tests/PrayerApp.IntegrationTests/PrayerApp.IntegrationTests.csproj
```

Testler gerçek ağa çıkmaz (FakeHttpHandler) ve `IClock` ile deterministic çalışır.
Sonuç raporu: [docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## Release build

```powershell
dotnet publish PrayerApp.csproj -f net10.0-android -c Release -p:AndroidPackageFormats=aab
```

İmzalama ve mağaza süreci: [docs/RELEASE_GUIDE.md](docs/RELEASE_GUIDE.md)

## Veri kaynakları

| Veri | Kaynak |
|---|---|
| Namaz vakitleri | [Aladhan API](https://aladhan.com/prayer-times-api) (HTTPS, key'siz) |
| Kur'an meali | `Resources/Raw/quran_tr.json` (quran-json formatı, Diyanet meali) |

Ayrıntı ve lisans notları: [docs/CONTENT_SOURCES.md](docs/CONTENT_SOURCES.md),
[docs/THIRD_PARTY_SERVICES.md](docs/THIRD_PARTY_SERVICES.md)

## Bilinen platform sınırlamaları

- iOS derlemesi macOS gerektirir; bildirim soyutlaması hazır ancak iOS üzerinde test edilmedi.
- Bazı üreticiler (Xiaomi/MIUI vb.) agresif pil yönetimiyle alarmları geciktirebilir; uygulama
  içi "Bildirim Tanılama" bölümü kullanıcıyı doğru ayara yönlendirir.
- Windows head'i yalnızca geliştirme kolaylığı içindir; mağaza hedefi değildir.

## Dokümantasyon

Tüm mimari, karar, test, bildirim, gizlilik ve yayın dokümanları [docs/](docs/) klasöründedir.
