# Changelog

## [1.0.0] — 2026-07-03 (hazırlık)

### Eklendi
- Katmanlı mimari: `PrayerApp.Core` (domain/saf mantık) ve `PrayerApp.Infrastructure` (SQLite, HTTP)
- SQLite tabanlı 35 günlük offline namaz vakti cache'i (konum + hesaplama yöntemi başına)
- Koordinat tabanlı Aladhan sorgusu, açık `method=13` (Diyanet), retry + doğrulama
- Onboarding akışı (konum, tema, bildirim izni — bağlamıyla)
- Otomatik konum (GPS) desteği + 81 il / ilçe manuel seçim
- Vakit Takvimi ekranı (gün gezinme, hicri tarih, paylaşma)
- Kur'an ekranı: sure listesi, ayet arama, favoriler, paylaşma, son okunan kayıt
- Zikirmatik: hazır/özel zikir, hedef, titreşim, günlük geçmiş (SQLite)
- Manuel vakit düzeltmeleri (her vakit ±60 dk)
- Hesaplama yöntemi seçimi (12 yöntem)
- Light/Dark/System tema desteği
- Bildirim kanalları (vakit, ön hatırlatma, ayet, diğer) ve rolling 3 günlük plan
- `TIME_SET`/`TIMEZONE_CHANGED`/exact-alarm izni değişimi receiver'ı
- Unit + integration test projeleri (59 test), GitHub Actions CI
- Kapsamlı docs seti (mimari, kararlar, bildirim, gizlilik, mağaza, release)

### Değişti
- Package ID: `com.companyname.prayerapp` → `com.galleryusuf.ezanvakti` (merkezi Directory.Build.props)
- Kur'an verisi belleğe tam yükleme → ilk çalıştırmada SQLite import + indeksli sorgular
- Günün ayeti: rastgele → tarihe bağlı deterministic (gün boyu sabit, offline)
- Ayarlar: tek JSON blob → versiyonlu `UserSettings` + legacy migration
- `USE_EXACT_ALARM` ve `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` izinleri kaldırıldı
- `android:allowBackup` → `false` (bilinçli gizlilik kararı)

### Kaldırıldı
- 10 saniyelik foreground bildirim timer'ı (gerçek AlarmManager planı ile değiştirildi)
- `dotnet_bot.png` placeholder'ı
- Git'te izlenen `bin/`, `obj/` çıktıları (.gitignore eklendi)
