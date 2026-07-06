# İçerik Kaynakları

## Kur'an metni ve meali — `Resources/Raw/quran_tr.json`

- **Kaynak proje:** [risan/quran-json](https://github.com/risan/quran-json) formatındaki
  dosya (depoda önceden mevcuttu). `text` alanları Arapça (Usmani) metni, `translation`
  alanları Türkçe meali içerir; her ikisi de SQLite'a import edilir.
- **Meal:** Diyanet İşleri Başkanlığı meali olarak etiketlenmiş Türkçe çeviri.
- **Lisans durumu:** quran-json projesi CC0/MIT dağıtımlıdır; ancak içerdiği Diyanet meali
  için Diyanet'in yeniden yayım politikası açık şekilde doğrulanamamıştır.
  **Yayın öncesi yapılacak:** meal kaynağının lisansı teyit edilmeli; gerekirse açık lisanslı
  başka bir Türkçe meal ile değiştirilmelidir. Uygulama "Hakkında" bölümünde kaynak belirtilir.
- Kullanım: ilk açılışta SQLite'a import edilir; sure okuma (Arapça + meal), ayet arama,
  favoriler, günün ayeti, Kur'an'dan dualar ve bildirim içerikleri bu veriden üretilir.

## Mushaf sayfa/cüz meta verisi — `QuranMetadata.cs`

- **Kaynak:** [Tanzil projesi](https://tanzil.net/docs/) `quran-data.xml` dosyasından üretilen
  statik tablo (604 Medine mushafı sayfası ve 30 cüzün başlangıç sure:ayet değerleri).
- Tanzil meta verisi serbest kullanımlıdır (atıf korunmuştur). Hatim takibi, cüz listesi ve
  "sayfayı aç" özellikleri bu tabloyu kullanır; ağa çıkılmaz.

## Kur'an'dan Dualar — `Data/QuranicDuas.cs`

- Yaygın bilinen Kur'an dualarının **yalnızca sure:ayet referans listesi**; metinler uygulamanın
  kendi Kur'an veritabanından okunur. Harici içerik veya ek lisans yoktur.

## Esmaül Hüsna — `Data/EsmaulHusnaData.cs`

- 99 isim, Arapça yazılış ve Türkçe anlamlarıyla uygulama içinde statik olarak yer alır
  (kamuya mal olmuş dini içerik; özgün derleme).

## Dini günler — `ReligiousDaysCalculator`

- Kandil/bayram tarihleri hicri takvimde sabittir; Gregoryen karşılıkları .NET
  `UmAlQuraCalendar` ile hesaplanır. Diyanet takvimiyle ±1 gün fark olabilir; bu not
  kullanıcıya ekranda gösterilir. Harici veri kaynağı yoktur.

## Namaz vakitleri — Aladhan API

- https://aladhan.com/prayer-times-api — ücretsiz, API key gerektirmez.
- Kullanım koşulları gereği makul istek sıklığı uygulanır: uygulama ayda ~2 istek/konum yapar
  (35 günlük takvim cache'lenir).

## Sesler

- Uygulamaya telif durumu doğrulanmamış hiçbir ezan sesi eklenmemiştir.
- Bildirimler varsayılan sistem sesini kullanır.
- Lisansı açık bir ezan kaydı eklenecekse bu dosyada kaynağı ve lisansı belgelenmelidir.

## Fontlar

- OpenSans (Regular/Semibold) — Apache License 2.0.
- **Amiri Quran 1.003** (`Resources/Fonts/AmiriQuran.ttf`) — Kur'an metni için;
  [aliftype/amiri](https://github.com/aliftype/amiri), SIL Open Font License 1.1
  (lisans metni: `Resources/Fonts/AmiriQuran-OFL.txt`).

## İkonlar

- Uygulama içi tab/aksiyon ikonları: proje için üretilmiş özgün SVG'ler (`Resources/Images`).
- App icon ve splash: depodaki mevcut özgün tasarım (`Resources/AppIcon`, `Resources/Splash`).
