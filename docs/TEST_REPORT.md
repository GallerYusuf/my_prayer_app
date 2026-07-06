# Test Raporu

Tarih: 2026-07-06 · Branch: `fable5/autonomous-evolution` · Ortam: Windows 11, .NET SDK 10.0.301

## Otomatik testler

| Proje | Sonuç | Adet | Süre |
|---|---|---|---|
| PrayerApp.UnitTests | ✅ BAŞARILI | 62/62 | ~0.2 sn |
| PrayerApp.IntegrationTests | ✅ BAŞARILI | 37/37 | ~2 sn |

Komutlar:
```
dotnet test tests/PrayerApp.UnitTests/PrayerApp.UnitTests.csproj
dotnet test tests/PrayerApp.IntegrationTests/PrayerApp.IntegrationTests.csproj
```

### Unit test kapsamı (62)

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
- **NotificationCoordinator (7):** plan kurulumu + sürekli bildirim tazelemesi, fresh planda
  yalnız tazeleme, stale planda yeniden kurulum, tüm kategoriler kapalıyken iptal + sürekli
  bildirimin yaşaması, fetch hatasında eski planın korunması (atomiklik), eski ID'lerin
  değiştirilmesi, onboarding öncesi hiçbir şey yapılmaması.
- **KerahatCalculator (9):** üç pencerenin sınırları, pencere içi/dışı anlar, uç değerler.
- **ZakatCalculator (6):** nisap altı/üstü, %2,5 oranı, borç düşümü, negatif girdiler, clamp.
- **ReligiousDaysCalculator (6):** 12 aylık sıralı liste, çekirdek günlerin varlığı,
  Regaib'in perşembeye düşmesi, hicri round-trip, ay adları, aralık dışı null.

### Integration test kapsamı (37)

- **AladhanProvider (6, FakeHttpHandler ile — gerçek ağ yok):** başarılı ay birleştirme +
  method=13/koordinat parametre doğrulaması, HTTP 500 retry→hata, bozuk JSON→InvalidData,
  eksik/bozuk gün atlama, ağ hatası→Network, API code≠200→InvalidData.
- **PrayerDayRepository (5, temp SQLite):** kayıt/okuma round-trip, aynı gün tekrar yazma
  duplicate üretmez, konum/method izolasyonu, eski kayıt temizliği, gelecek gün sayımı.
- **PrayerScheduleService (8, FakeClock ile):** boş cache→fetch+cache, sıcak cache→ağsız,
  offline+cache→fallback, offline+cache yok→tipli hata, konumsuz→hata, konum değişimi→yeni fetch,
  düzeltmelerin uygulanması, incelen cache→proaktif yenileme.
- **QuranRepository (11, temp SQLite):** JSON import, sıralı ayet listesi, büyük/küçük harf
  duyarsız arama, kısa sorgu reddi, global indeks eşlemesi, favori ekle/çıkar, Arapça metin
  importu, sayfa/cüz ataması, sayfa başlangıcı sorgusu, cüz listesi, import v2 yeniden
  yüklemesinde favorilerin korunması.
- **KhatmRepository (7, temp SQLite):** plan yok→null, plan başlatma, eski planın değişimi,
  günlük ilerleme kaydı, clamp + 604'te tamamlanma, günlük hedef hesabı, plan silme.

## Build doğrulaması

| Build | Sonuç |
|---|---|
| Android Debug (`net10.0-android`) | ✅ 0 hata, **0 uyarı** (2026-07-06) |
| Android Release AAB (`dotnet publish -c Release -p:AndroidPackageFormats=aab -p:PublishTrimmed=false -p:RunAOTCompilation=false`) | ✅ `bin/Release/net10.0-android/publish/com.galleryusuf.ezanvakti-Signed.aab` üretildi (2026-07-06, 64 MB untrimmed) |

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
| 14 | Sürekli bildirim | Açılışta görünür; vakit girince başlık değişir; ayardan kapatınca kaybolur; reboot sonrası geri gelir |
| 15 | Widget ekle + uygulamayı kapat | Geri sayım akmaya devam eder (API 24+); vakit girince sıradaki vakit değişir; gün dönümünde yeni günün vakitleri gelir |
| 16 | Widget + uçak modu | Cache'ten çizilir, internet gerekmez |
| 17 | Güncelleme sonrası ilk Kur'an açılışı | Arapça metin görünür (v2 import ~1-2 sn); eski favoriler durur |
| 18 | Sure ekranı | Arapça aç/kapat ve A−/A+ çalışır, tercihler kalıcı; sayfa/cüz etiketi doğru |
| 19 | Hatim akışı | Plan başlat → sayfa kaydet → "Okumaya Git" doğru sayfayı açar; 604'te tebrik mesajı |
| 20 | Kaldığın yerden devam | Sure içinde kaydır, çık; Kur'an ekranındaki kart son ayete döndürür |
| 21 | Dini Günler | Liste sıralı, bir sonraki gün vurgulu; çevirici bugünün hicri tarihini verir |
| 22 | Kaza / Zekât | Sayaçlar kalıcı; zekât nisap altı/üstü mesajları doğru |
| 23 | Tesbihat | 33'e ulaşınca sonraki zikre otomatik geçer, üçüncüden sonra tamamlanır |
| 24 | Kerahat chip'i | Doğuş+45 dk, öğle öncesi 45 dk, akşam öncesi 45 dk aralıklarında görünür |

ADB tabanlı bildirim senaryoları: [NOTIFICATION_TESTING.md](NOTIFICATION_TESTING.md)
