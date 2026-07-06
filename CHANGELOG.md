# Changelog

## [1.1.0-dev] — 2026-07-06 (fable5/autonomous-evolution)

### Eklendi
- Kur'an: Arapça metin (Amiri Quran fontu, RTL) + meal birlikte; Arapça aç/kapat ve yazı
  boyutu kontrolü; 30 cüz listesi; ayet bazlı "kaldığın yerden devam"; sayfa bazlı hatim
  takibi (30/60/90 gün veya serbest, günlük hedef, ilerleme)
- Kur'an'dan Dualar ekranı (26 küratif dua, kendi veritabanından, paylaşılabilir)
- Esmaül Hüsna ekranı (99 isim, Arapça yazılış, anlam, arama)
- Dini Günler ekranı (kandil/bayram takvimi, 12 ay) + Miladi→Hicri tarih çevirici
- Kaza takibi (5 vakit + vitir + oruç sayaçları) ve Zekât hesaplayıcı (offline, nisap 80,18 g)
- Ana ekranda kerahat vakti göstergesi; Zikirmatik'te rehberli namaz tesbihatı (33×3)
- Ana ekran hızlı erişim alanı 8 modüle genişletildi
- Sürekli vakit bildirimi (Android): günün vakitleri + sıradaki vakit bildirim çubuğunda
  sabit durur; sessiz kanal, ayarlardan kapatılabilir, reboot/güncelleme sonrası geri gelir
- Widget'a günün 6 vaktinin tamamı ve uygulama kapalıyken de işleyen native geri sayım
  (Chronometer count-down, API 24+)
- Vakit türleri için özel vector ikon seti (10 SVG); tüm ekranlarda emoji glyph'lerin yerini aldı
- `NotificationCoordinator` için 7 yeni unit test (toplam 66 test)

### Değişti
- Widget artık uygulama açıkken yazılan önbelleğe değil, doğrudan SQLite vakit cache'ine ve
  vakit girişlerinde tetiklenen alarm zincirine bağlı — uygulama kapalıyken donmuş/yanlış
  geri sayım gösterme hatası giderildi
- `DisplayAlert` → `DisplayAlertAsync`; widget null-güvenliği ve API-level guard düzeltmeleri
  (derleme uyarıları sıfırlandı)

### Kaldırıldı
- `WidgetDataService` / `IWidgetDataService` ve MainViewModel'deki 55 sn'lik widget güncelleme yolu
- Git'te izlenen JVM crash logları ve `run_command` artığı

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
