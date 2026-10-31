<div align="center">

<img src="Resources/AppIcon/appicon.png" width="88" height="88" alt="Ezan Vakti uygulama simgesi" />

# Ezan Vakti

**Vakitleri takip et. Kaldığın yerden oku. İbadetini kendi ritminde sürdür.**

Türkçe, reklamsız ve hesap gerektirmeyen; günlük ibadet akışını tek yerde buluşturan native .NET MAUI uygulaması.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![MAUI](https://img.shields.io/badge/UI-Native%20MAUI-00796B)
![Android](https://img.shields.io/badge/Android-API%2021%2B-3DDC84?logo=android&logoColor=white)
![Dil](https://img.shields.io/badge/Dil-Türkçe-8C6D1F)

[Özellikler](#özellikler) · [Kurulum](#kurulum-ve-telefonda-çalıştırma) · [Testler](#testler-ve-doğrulama) · [Tasarım](#tasarım-ve-canvas-alternatifleri) · [Dokümantasyon](#dokümantasyon)

</div>

## Günlük kullanım için beş ana sekme

| Sekme | Ne yapabilirsiniz? |
|---|---|
| **Bugün** | Şehrinizi seçin, sıradaki vakti ve geri sayımı görün, beş farz namazı işaretleyin; Kur'an, kıble ve zikre hızlıca geçin. |
| **Kur'an** | Sure, cüz, konu veya favorilerden okumaya başlayın; kaldığınız ayete dönün, mealde arayın ve hatim ilerlemenizi yönetin. |
| **İbadet** | Zikirmatik, namaz ve kaza takibi ile hatim planına aynı merkezden ulaşın. |
| **Takvim** | Seçtiğiniz günün vakitlerini, yaklaşan dini günleri ve Miladi–Hicri tarih dönüşümünü inceleyin. |
| **Keşfet** | Kıble pusulası, Kur'an'dan dualar, Esmaül Hüsna, zekât hesaplayıcı ve ayarlara ulaşın. |

## Özellikler

### Vakitler ve hatırlatmalar

- **Konuma göre namaz vakitleri:** Aladhan API üzerinden Diyanet hesaplama yöntemi (`method=13`), şehir/ilçe seçimi veya isteğe bağlı cihaz konumu.
- **35 günlük yerel önbellek:** Kaydedilmiş vakitlere internetsiz erişim; çevrimdışı ve veri bulunamadı durumları için açıklayıcı geri bildirim.
- **Kişiselleştirme:** Vakit başına dakika düzeltmesi, hesaplama yöntemi seçimi, hicri tarih, Ramazan ve kerahat bilgileri.
- **Android bildirimleri:** Vakit öncesi hatırlatmalar, ayet ve cuma bildirimleri, sürekli vakit bildirimi; yeniden başlatma ve saat değişiminden sonra yeniden planlama.
- **Ana ekran widget'ı:** Günün altı vakti, sıradaki vakit ve native geri sayım.

### Kur'an ve okuma

- **114 sure, 30 cüz:** Amiri Quran ile Arapça metin ve Türkçe meal; Mushaf/Nüzul sıralaması ve konu fihristi.
- **Okumaya devam:** Son okunan ayet, favoriler, kopyalama ve paylaşma; önceki/sonraki sureye geçiş.
- **Okuma görünümü:** Arapça ve meal için bağımsız yazı boyutu, görünürlük tercihleri ve odak modu.
- **Sesli tilavet:** İnternet üzerinden sure dinleme; yükleme, duraklatma ve hata durumları.
- **Hatim planı:** 604 sayfa üzerinden 30/60/90 gün veya serbest hedef, günlük ilerleme ve okuma ekranından sayfa güncelleme.

### İbadet araçları

- **Zikirmatik:** Hazır/özel zikir, büyük sayaç, hedef, titreşim ve günlük geçmiş; bir azaltma ve sıfırlamayı geri alma.
- **Rehberli tesbihat:** Adım adım namaz tesbihatı.
- **Namaz ve kaza takibi:** Günlük işaretleme, son yedi günün özeti, kaza sayaçları ve son işlemi geri alma.
- **Dini içerik:** Arapça metin ve Türkçe anlamlarıyla dualar ve Esmaül Hüsna; arama ve favoriler.
- **Kıble ve zekât:** Sensör destekli pusula, Kâbe mesafesi ve çevrimdışı zekât hesaplama.
- **Görünüm ve yedekleme:** Açık/koyu/sistem teması, ayarlar üzerinden yerel JSON yedeği dışa aktarma ve geri yükleme.

## Çevrimdışı kullanım ve gizlilik

Hesap oluşturmanız gerekmez. Uygulama verileri cihazda SQLite ve yerel tercihler içinde saklanır; yedeği kullanıcı kendi seçtiği konuma aktarabilir.

| İşlem | İnternet ihtiyacı |
|---|---|
| Kur'an metni, meal, arama, favoriler ve hatim | Metinler uygulamayla gelir ve ilk açılışta yerel veritabanına aktarılır. |
| Zikir, namaz/kaza takibi, dualar, Esma ve hesaplayıcılar | Çevrimdışı kullanılabilir. |
| Namaz vakitleri | Takvim indirmek/yenilemek için gerekir; kayıtlı günler çevrimdışı açılır. |
| Sesli tilavet | Ses akışı için gerekir; çevrimdışı ses indirme özelliği yoktur. |

Vakit sorgularında seçilen konumun koordinatları Aladhan'a iletilir; sesli tilavet QuranicAudio'dan alınır. Dini günlerin Miladi karşılıkları Ümmü'l-Kura takvimiyle hesaplanır; yerel resmi takvimle farklılık gösterebilir. Kaynak ve yayın notları için [İçerik Kaynakları](docs/CONTENT_SOURCES.md) belgesine bakın.

## Kurulum ve telefonda çalıştırma

### Gereksinimler

- .NET 10 SDK ve MAUI Android workload.
- Android SDK **Platform 36**, Android Build Tools ve Platform Tools (`adb`).
- JDK; bu çalışma ağacı **JDK 21** ile derlendi.
- Fiziksel Android cihazda geliştirici seçenekleri ve USB hata ayıklama ya da bir Android emülatörü.

Android minimum sürümü API 21'dir; bazı platform özellikleri daha yeni Android sürümü veya ek izin gerektirir. Doğrulanan geliştirme ortamı: Windows, .NET SDK **10.0.401**, Android SDK **36**, JDK **21**.

```powershell
git clone https://github.com/GallerYusuf/my_prayer_app.git
cd my_prayer_app

dotnet workload install maui-android
dotnet build PrayerApp.csproj -f net10.0-android -c Debug

# Bağlı cihaz veya emülatöre yükle ve aç:
dotnet build PrayerApp.csproj -f net10.0-android -c Debug -t:Run
```

Windows'ta mevcut dağıtım yardımcısını da kullanabilirsiniz:

```powershell
adb devices
.\deploy.ps1
```

`deploy.cmd` aynı PowerShell yardımcısını açar. Yardımcı, yaygın SDK/JDK konumlarını kontrol eder; Türkçe karakter içeren Windows yolları için `C:\Android` altında ASCII junction kullanabilir. Farklı bir SDK/JDK kurulumunda derlemeye `AndroidSdkDirectory` ve `JavaSdkDirectory` parametrelerini verin.

**Sık karşılaşılan durumlar:**

- Cihaz `unauthorized` görünüyorsa telefondaki USB hata ayıklama onayını verin.
- `XA5207` hatasında kullanılan SDK dizininde `platforms/android-36/android.jar` bulunduğunu kontrol edin.
- Bildirimler için sistem bildirim ve tam zamanlı alarm izinlerini kontrol edin. Uygulama içindeki **Ayarlar → Gelişmiş Bildirim Tanılama** bölümü yardımcı olur; üreticinin pil yönetimi de teslimi etkileyebilir.

### Release paketi

```powershell
dotnet publish PrayerApp.csproj -f net10.0-android -c Release -p:AndroidPackageFormats=aab
```

Mağaza dağıtımı için ayrıca imzalama ve yayın kontrolleri gerekir: [Release Guide](docs/RELEASE_GUIDE.md).

## Testler ve doğrulama

```powershell
dotnet test tests/PrayerApp.UnitTests/PrayerApp.UnitTests.csproj
dotnet test tests/PrayerApp.IntegrationTests/PrayerApp.IntegrationTests.csproj
```

**31 Ekim 2026 doğrulaması:** 65 birim testi ve 38 entegrasyon testi geçti; toplam **103/103**. Android Debug derlemesi **0 hata / 0 uyarı** ile tamamlandı. APK fiziksel Android 12 telefona yüklenip başlatıldı.

Testler Core/Infrastructure hesaplarını, veri erişimini, yedeklemeyi ve servis davranışlarını kapsar; tüm MAUI ekranlarının veya gerçek cihaz bildirimlerinin uçtan uca kabul testi değildir. Ayrıntılar: [Test Raporu](docs/TEST_REPORT.md), [Bildirim Testleri](docs/NOTIFICATION_TESTING.md).

## Mimari

```text
PrayerApp (native MAUI / XAML / ViewModels)
    ├── PrayerApp.Core           → domain, sözleşmeler, hesaplamalar
    └── PrayerApp.Infrastructure → SQLite, HTTP ve servis uygulamaları
```

| Konum | Sorumluluk |
|---|---|
| `Views/`, `ViewModels/` | Ekranlar, bağlamalar, komutlar ve etkileşim durumları |
| `Resources/Styles/` | Paylaşılan renkler, tema bağlamaları ve XAML stilleri |
| `Services/`, `Platforms/Android/` | Uygulama/platform servisleri, bildirimler, widget ve ses |
| `src/PrayerApp.Core/` | Platformdan bağımsız kurallar ve hesaplamalar |
| `src/PrayerApp.Infrastructure/` | Veritabanı ve dış servis erişimi |
| `tests/` | Birim ve entegrasyon testleri |

Detaylı katmanlar ve tasarım kararları: [Mimari](docs/ARCHITECTURE.md), [Kararlar](docs/DECISIONS.md).

## Tasarım ve Canvas alternatifleri

Mevcut native arayüzün tasarım bağlamı [DESIGN.md](DESIGN.md) içinde tutulur. Kur'an, İbadet ve Takvim için Kombai'de hazırlanan görsel keşifler de [Canvas dosyasında](.kombai/canvas/ezan-vakti-redesign.canvas) yer alır.

| Görsel dil | Yaklaşım |
|---|---|
| **Levha** | Kâğıt hissi, tipografi ve ince ayırıcı çizgiler |
| **Pano** | Belirgin zümrüt alanlar ve güçlü durum vurguları |
| **Fanous** | Yuvarlak yüzeyler, ton farkları ve halkalı zikir sayacı |

Canvas, bu stilleri satır odaklı **A**, eylem odaklı **B** ve sabit görev alanlı **C** düzenlerinde karşılaştırır. Panolar açık temadadır; koyu tema karşılıkları tasarım notlarında tanımlanmıştır.

**Bu alternatifler tasarım aşamasındadır; uygulamanın çalışan XAML ekranlarına aktarılmamıştır.** Seçilecek tasarım native MAUI bileşenleriyle uygulanacak; Canvas HTML/CSS çıktısı doğrudan uygulama arayüzü olarak kullanılmayacaktır. Küçük ekran, erişilebilirlik ve koyu tema için cihaz doğrulaması bu aşamanın parçasıdır.

## Platform ve yayın durumu

- **Android:** Güncel Debug paketi derlendi ve fiziksel cihazda başlatıldı. Bildirim, widget ve sensör davranışları cihaz/üretici koşullarına göre ayrıca doğrulanmalıdır.
- **iOS / Mac Catalyst:** Proje hedefleri bulunur; derleme için macOS gerekir. Bu değişiklikler bu platformlarda doğrulanmadı.
- **Windows:** Geliştirme amacıyla bulunan hedeftir; bu sürüm için mağaza kabulü doğrulanmadı.
- **İçerik:** Özellikle mealin yeniden dağıtım koşulları yayın öncesinde teyit edilmelidir; ayrıntılar içerik kaynakları belgesindedir.

## Dokümantasyon

| Konu | Belge |
|---|---|
| Tasarım sistemi | [DESIGN.md](DESIGN.md) |
| Mimari ve kararlar | [Architecture](docs/ARCHITECTURE.md) · [Decisions](docs/DECISIONS.md) |
| Bildirim altyapısı | [Notification Architecture](docs/NOTIFICATION_ARCHITECTURE.md) |
| Vakit doğrulaması | [Prayer Time Validation](docs/PRAYER_TIME_VALIDATION.md) |
| İçerik ve dış servisler | [Content Sources](docs/CONTENT_SOURCES.md) · [Third-party Services](docs/THIRD_PARTY_SERVICES.md) |
| Gizlilik | [Türkçe gizlilik politikası](docs/PRIVACY_POLICY_TR.md) |
| Yayın | [Release Guide](docs/RELEASE_GUIDE.md) · [Mağaza metni](docs/PLAY_STORE_LISTING_TR.md) |
| Gelişim | [Roadmap](docs/ROADMAP.md) · [Changelog](CHANGELOG.md) |

Hata bildirirken uygulama/Android sürümünü ve tekrar adımlarını [GitHub Issues](https://github.com/GallerYusuf/my_prayer_app/issues) üzerinden paylaşabilirsiniz.
