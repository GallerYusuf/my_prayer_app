# Gizlilik Politikası — Ezan Vakti

Son güncelleme: 2026-07-03

## Özet

Ezan Vakti, verilerinizi **cihazınızda** tutar. Hesap yoktur, reklam yoktur, analitik yoktur,
kişisel verileriniz hiçbir sunucuda saklanmaz.

## Toplanan ve işlenen veriler

### Konum
- **Ne için:** Namaz vakitlerinin ve kıble yönünün hesaplanması.
- **Nasıl:** Konum izni verirseniz cihazınızın konumu; vermezseniz seçtiğiniz şehir merkezi.
- **Nereye gider:** Yalnızca vakit hesaplaması için koordinat (enlem/boylam) Aladhan namaz
  vakti servisine (api.aladhan.com, HTTPS) iletilir. Bu istekte kimliğinizi tanımlayan hiçbir
  bilgi bulunmaz. Konum geçmişi tutulmaz, hiçbir sunucuya kaydedilmez.

### Cihazda saklanan veriler
- Uygulama ayarları (tema, bildirim tercihleri, vakit düzeltmeleri)
- İndirilen namaz vakti takvimi (çevrimdışı kullanım için)
- Favori ayetler, zikir sayacı geçmişi, namaz takip işaretleri

Bu veriler yalnızca cihazınızdadır; uygulamayı sildiğinizde tamamen silinir.
Android yedeklemesi kapalıdır (`allowBackup=false`) — verileriniz buluta kopyalanmaz.

## Üçüncü taraflar

Tek dış servis Aladhan Prayer Times API'dir (bkz. uygulama içi Hakkında bölümü).
Reklam SDK'sı, analitik SDK'sı veya sosyal medya SDK'sı **kullanılmaz**.

## İzinler

| İzin | Amaç |
|---|---|
| İnternet | Vakit takvimini indirmek |
| Bildirim | Vakit ve ayet hatırlatmaları |
| Tam zamanlı alarm | Bildirimlerin dakikasında teslimi |
| Konum (isteğe bağlı) | Otomatik vakit/kıble hesabı — reddedilirse şehir seçimiyle çalışır |

## Çocuklar

Uygulama kişisel veri toplamadığından her yaş için uygundur.

## Değişiklikler

Politika değişirse uygulama güncelleme notlarında belirtilir.

## İletişim

Sorularınız için: (yayın öncesi geliştirici e-postası eklenecek)
