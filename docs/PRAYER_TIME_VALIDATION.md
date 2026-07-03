# Namaz Vakti Doğrulaması

## Yöntem

Uygulama, Aladhan API'sinin `method=13` (Diyanet İşleri Başkanlığı, Türkiye) parametresini
koordinat tabanlı sorgularla kullanır. Bu method Diyanet'in açı parametrelerini
(İmsak 18°, Yatsı 17°) uygular.

Doğrulanmamış "resmî Diyanet API'si" veya izinsiz scraping kullanılmamıştır; bu nedenle
values Diyanet takvimiyle birebir değil, "mümkün olduğunca yakın" hedeflenmiştir.

## Bilinen fark kaynakları

1. **Temkin dakikaları:** Diyanet bazı vakitlere il bazlı temkin ekler (örn. güneş +5 dk gibi).
   Aladhan method=13 genel açı hesabı yapar; il bazlı temkinleri bilmez.
2. **Koordinat farkı:** İl merkezi koordinatı ile Diyanet'in referans noktası birebir aynı
   olmayabilir (~1-2 dk fark üretebilir).
3. **Yükseklik:** Rakım hesaba katılmaz.

Tipik sapma: çoğu vakit için 0-3 dk. Bu fark, uygulamadaki **manuel dakika düzeltmeleri**
(her vakit için ±60 dk) ile kullanıcı tarafından kapatılabilir.

## Referans karşılaştırma prosedürü

Yayın öncesi şu tablo cihaz üzerinde diyanet.gov.tr namaz vakitleri sayfasıyla doldurulmalıdır
(otomatik erişim/scraping yapılmadığı için manuel kontrol gerekir):

| Şehir | Tarih | Vakit | Diyanet | Uygulama | Fark |
|---|---|---|---|---|---|
| İstanbul | (bugün) | İmsak | __ | __ | __ |
| İstanbul | (bugün) | Akşam | __ | __ | __ |
| Ankara | (bugün) | Öğle | __ | __ | __ |
| Ankara | (+15 gün) | Yatsı | __ | __ | __ |
| Van | (bugün) | İmsak | __ | __ | __ |
| Van | (+15 gün) | İkindi | __ | __ | __ |

Kabul kriteri: |fark| ≤ 5 dk. Sistematik sapma görülürse (örn. güneş hep +5 dk) bu sapma
varsayılan düzeltme önerisi olarak `PRAYER_TIME_VALIDATION.md`'ye işlenmeli ve gerekirse
uygulamada il bazlı varsayılan offset düşünülmelidir.

## Otomatik doğrulanan kısımlar (unit/integration testleri)

- API yanıtının parse edilmesi, eksik/bozuk gün atlama
- Gece yarısı sonrası ilk vakit (bugünün imsakı) hesabı
- Gün değişimi ve yarına devreden sonraki vakit
- Timezone offset'inden bağımsız duvar saati hesabı
- Manuel dakika düzeltmelerinin uygulanması
- Cache izolasyonu (konum/method başına) ve tazelik kontrolü
