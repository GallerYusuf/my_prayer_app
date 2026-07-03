# Üçüncü Taraf Servisler

Uygulama local-first çalışır; tek dış servis vardır.

## 1. Aladhan Prayer Times API

| Başlık | Detay |
|---|---|
| **Amaç** | Seçilen konum için aylık namaz vakti takvimi almak |
| **Endpoint** | `https://api.aladhan.com/v1/calendar/{yıl}/{ay}` (HTTPS) |
| **Gönderilen veri** | Enlem/boylam (6 hane), hesaplama yöntemi numarası, yıl/ay. Kimlik, cihaz kimliği, IP dışında hiçbir kişisel veri gönderilmez. Konum, kullanıcının seçtiği şehir merkezi veya cihaz konumu olabilir. |
| **Sıklık** | Konum/method başına ~ayda 2 istek (35 günlük cache) + kullanıcı elle yenilerse |
| **Gizlilik etkisi** | Koordinat, vakit hesabı için zorunlu asgari veridir; sunucuda hesap/oturum yoktur. API'ye kayıt gerektirmez, API key yoktur. |
| **Fallback davranışı** | Ağ hatası/timeout/5xx → 1 kez retry → SQLite cache'inden son geçerli takvim → cache de yoksa kullanıcıya Türkçe hata + "Tekrar Dene" |

## 2. Platform servisleri (dış sunucu değil)

- **Geolocation / Geocoding (cihaz):** Konum yalnızca cihazda işlenir; reverse geocoding
  işletim sisteminin servisiyle yapılır ve başarısız olursa koordinatla devam edilir.
- **Google Play Services:** MAUI/Android çalışma zamanının standart bileşenleri dışında ek SDK
  (analytics, reklam, crash raporlama) **yoktur**.
