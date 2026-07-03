# Test Raporu

Tarih: 2026-07-03 · Branch: `fable5/production-rebuild` · Ortam: Windows 11, .NET SDK 10.0.301

## Otomatik testler

| Proje | Sonuç | Adet | Süre |
|---|---|---|---|
| PrayerApp.UnitTests | ✅ BAŞARILI | 34/34 | ~0.4 sn |
| PrayerApp.IntegrationTests | ✅ BAŞARILI | 25/25 | ~2 sn |

Komutlar:
```
dotnet test tests/PrayerApp.UnitTests/PrayerApp.UnitTests.csproj
dotnet test tests/PrayerApp.IntegrationTests/PrayerApp.IntegrationTests.csproj
```

### Unit test kapsamı (34)

- **NextPrayerCalculator (8):** sabah→öğle, yatsı sonrası→yarın imsak, gece yarısı sonrası→bugün
  imsak, tam vakit anı→sonraki, veri yok→null, yalnız geçmiş günler→null, farklı UTC offset'te
  duvar saati doğruluğu, geçen vakitlerin işaretlenmesi.
- **PrayerAdjustments/UserSettings (5):** dakika kaydırma, sıfır düzeltmede aynı instance,
  ±60 clamp, bozuk kalıcı veriden onarım (Sanitized), UserSettings aralık onarımı.
- **QiblaCalculator (7):** İstanbul ≈151.6°, Ankara ≈160.2°, Van ≈191.1° referans açıları,
  Kabe'de mesafe 0, İstanbul-Mekke ~2400 km, Türkiye geneli 140-200° aralığı.
- **NotificationPlanner/Ids (9):** yalnız gelecekteki bildirimler, idempotency (aynı girdi →
  aynı plan), ID benzersizliği, vakit başına aç/kapat, ön hatırlatma ofseti, manuel düzeltmenin
  plana yansıması, Cuma bildirimi (aynı gün ve sonraki hafta), ID determinizmi + kategori çözümü.
- **VerseOfDaySelector (5):** aynı gün sabit indeks, günler arası çeşitlilik, slot farkı,
  sınır kontrolü, sıfır ayet durumu.

### Integration test kapsamı (25)

- **AladhanProvider (6, FakeHttpHandler ile — gerçek ağ yok):** başarılı ay birleştirme +
  method=13/koordinat parametre doğrulaması, HTTP 500 retry→hata, bozuk JSON→InvalidData,
  eksik/bozuk gün atlama, ağ hatası→Network, API code≠200→InvalidData.
- **PrayerDayRepository (5, temp SQLite):** kayıt/okuma round-trip, aynı gün tekrar yazma
  duplicate üretmez, konum/method izolasyonu, eski kayıt temizliği, gelecek gün sayımı.
- **PrayerScheduleService (8, FakeClock ile):** boş cache→fetch+cache, sıcak cache→ağsız,
  offline+cache→fallback, offline+cache yok→tipli hata, konumsuz→hata, konum değişimi→yeni fetch,
  düzeltmelerin uygulanması, incelen cache→proaktif yenileme.
- **QuranRepository (6, temp SQLite):** JSON import, sıralı ayet listesi, büyük/küçük harf
  duyarsız arama, kısa sorgu reddi, global indeks eşlemesi, favori ekle/çıkar.

## Build doğrulaması

| Build | Sonuç |
|---|---|
| Android Debug (`net10.0-android`) | ✅ 0 hata |
| Android Release AAB (`dotnet publish -c Release -p:AndroidPackageFormats=aab -p:PublishTrimmed=false`) | ✅ `bin/Release/net10.0-android/publish/com.galleryusuf.ezanvakti-Signed.aab` üretildi |

**Trimming notu:** Bu geliştirme ortamında ILLink görevi, MSBuild alt sürecinin sandbox
tarafından engellenmesi ("Cannot acquire required number of nodes / erişim engellendi")
nedeniyle çalıştırılamadı; Release AAB `PublishTrimmed=false` ile üretildi. CI workflow'u
(`.github/workflows/ci.yml`) varsayılan (trimmed) publish'i çalıştırır — trimming testi orada
veya normal geliştirici makinesinde `dotnet publish -c Release` ile yapılmalıdır.
`Plugin.LocalNotification` için `TrimmerRootAssembly` korunmuştur.

## Manuel smoke test senaryoları (cihazda yayın öncesi)

| # | Senaryo | Beklenen |
|---|---|---|
| 1 | İlk açılış | Onboarding gelir; izinler ancak ilgili adımda istenir |
| 2 | Onboarding'te konum reddi | Hata mesajı + şehir listesinden seçim çalışır |
| 3 | Şehir seç + Başla | Ana sayfa vakitleri gösterir, geri sayım akar |
| 4 | Uçak modu + uygulamayı kapat/aç | Cache'ten açılır, "Kayıtlı veri" çipi görünür, geri sayım çalışır |
| 5 | Uçak modunda Kur'an/zikir/kıble | Tamamı çalışır |
| 6 | Takvim ekranında 30 gün ileri | Vakitler cache'ten gelir; ötesinde anlaşılır mesaj |
| 7 | Ayarlar→şehir değiştir→Kaydet | Yeni vakitler doğrulanmadan eski bildirimler silinmez; başarıda plan yenilenir |
| 8 | Tema değişimi (Açık/Koyu/Sistem) | Anında uygulanır, yeniden başlatmada korunur |
| 9 | Bildirim testi (Ayarlar) | Anında bildirim gelir; tanılama sayıları doğru |
| 10 | Gece 23:59→00:01 geçişi | Ana sayfa yarının imsakına geçer, tarih/ayet güncellenir |
| 11 | Bildirim izni reddi | Çökme yok; tanılamada "Kapalı" görünür |
| 12 | Pusulasız emülatör | Kıble ekranı açıklamalı fallback gösterir |
| 13 | Ayet bildirimine dokun | İlgili sure/ayet açılır |

ADB tabanlı bildirim senaryoları: [NOTIFICATION_TESTING.md](NOTIFICATION_TESTING.md)
