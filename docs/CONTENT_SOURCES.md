# İçerik Kaynakları

## Kur'an meali — `Resources/Raw/quran_tr.json`

- **Kaynak proje:** [risan/quran-json](https://github.com/risan/quran-json) formatındaki
  Türkçe meal dosyası (depoda önceden mevcuttu).
- **Meal:** Diyanet İşleri Başkanlığı meali olarak etiketlenmiş Türkçe çeviri
  (dosyadaki `translation` alanları).
- **Lisans durumu:** quran-json projesi CC0/MIT dağıtımlıdır; ancak içerdiği Diyanet meali
  için Diyanet'in yeniden yayım politikası açık şekilde doğrulanamamıştır.
  **Yayın öncesi yapılacak:** meal kaynağının lisansı teyit edilmeli; gerekirse açık lisanslı
  başka bir Türkçe meal ile değiştirilmelidir. Uygulama "Hakkında" bölümünde kaynak belirtilir.
- Kullanım: ilk açılışta SQLite'a import edilir; ayet arama, favoriler, günün ayeti ve
  bildirim içerikleri bu veriden üretilir.

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

## İkonlar

- Uygulama içi tab/aksiyon ikonları: proje için üretilmiş özgün SVG'ler (`Resources/Images`).
- App icon ve splash: depodaki mevcut özgün tasarım (`Resources/AppIcon`, `Resources/Splash`).
