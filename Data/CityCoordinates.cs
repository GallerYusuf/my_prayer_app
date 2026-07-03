namespace PrayerApp.Data;

/// <summary>
/// Province-center coordinates for Turkey's 81 provinces, keyed by plate code.
/// Used for coordinate-based prayer time queries when the user selects a city manually.
/// Values are city-center approximations (±0.01° ≈ 1 km, well within prayer time tolerance).
/// </summary>
public static class CityCoordinates
{
    private static readonly Dictionary<int, (double Lat, double Lon)> ByPlate = new()
    {
        [1] = (37.0000, 35.3213),   // Adana
        [2] = (37.7648, 38.2786),   // Adıyaman
        [3] = (38.7507, 30.5567),   // Afyonkarahisar
        [4] = (39.7191, 43.0503),   // Ağrı
        [5] = (40.6499, 35.8353),   // Amasya
        [6] = (39.9334, 32.8597),   // Ankara
        [7] = (36.8969, 30.7133),   // Antalya
        [8] = (41.1828, 41.8183),   // Artvin
        [9] = (37.8560, 27.8416),   // Aydın
        [10] = (39.6484, 27.8826),  // Balıkesir
        [11] = (40.1451, 29.9799),  // Bilecik
        [12] = (38.8854, 40.4966),  // Bingöl
        [13] = (38.4006, 42.1095),  // Bitlis
        [14] = (40.7392, 31.6089),  // Bolu
        [15] = (37.7203, 30.2908),  // Burdur
        [16] = (40.1885, 29.0610),  // Bursa
        [17] = (40.1553, 26.4142),  // Çanakkale
        [18] = (40.6013, 33.6134),  // Çankırı
        [19] = (40.5506, 34.9556),  // Çorum
        [20] = (37.7765, 29.0864),  // Denizli
        [21] = (37.9144, 40.2306),  // Diyarbakır
        [22] = (41.6818, 26.5623),  // Edirne
        [23] = (38.6810, 39.2264),  // Elazığ
        [24] = (39.7500, 39.5000),  // Erzincan
        [25] = (39.9000, 41.2700),  // Erzurum
        [26] = (39.7767, 30.5206),  // Eskişehir
        [27] = (37.0662, 37.3833),  // Gaziantep
        [28] = (40.9128, 38.3895),  // Giresun
        [29] = (40.4386, 39.5086),  // Gümüşhane
        [30] = (37.5744, 43.7408),  // Hakkari
        [31] = (36.2025, 36.1606),  // Hatay
        [32] = (37.7648, 30.5566),  // Isparta
        [33] = (36.8000, 34.6333),  // Mersin
        [34] = (41.0082, 28.9784),  // İstanbul
        [35] = (38.4192, 27.1287),  // İzmir
        [36] = (40.6013, 43.0975),  // Kars
        [37] = (41.3887, 33.7827),  // Kastamonu
        [38] = (38.7312, 35.4787),  // Kayseri
        [39] = (41.7333, 27.2167),  // Kırklareli
        [40] = (39.1425, 34.1709),  // Kırşehir
        [41] = (40.8533, 29.8815),  // Kocaeli
        [42] = (37.8667, 32.4833),  // Konya
        [43] = (39.4167, 29.9833),  // Kütahya
        [44] = (38.3552, 38.3095),  // Malatya
        [45] = (38.6191, 27.4289),  // Manisa
        [46] = (37.5858, 36.9371),  // Kahramanmaraş
        [47] = (37.3212, 40.7245),  // Mardin
        [48] = (37.2153, 28.3636),  // Muğla
        [49] = (38.9462, 41.7539),  // Muş
        [50] = (38.6939, 34.6857),  // Nevşehir
        [51] = (37.9667, 34.6833),  // Niğde
        [52] = (40.9839, 37.8764),  // Ordu
        [53] = (41.0201, 40.5234),  // Rize
        [54] = (40.7569, 30.3783),  // Sakarya
        [55] = (41.2928, 36.3313),  // Samsun
        [56] = (37.9333, 41.9500),  // Siirt
        [57] = (42.0231, 35.1531),  // Sinop
        [58] = (39.7477, 37.0179),  // Sivas
        [59] = (40.9833, 27.5167),  // Tekirdağ
        [60] = (40.3167, 36.5500),  // Tokat
        [61] = (41.0015, 39.7178),  // Trabzon
        [62] = (39.3074, 39.4388),  // Tunceli
        [63] = (37.1591, 38.7969),  // Şanlıurfa
        [64] = (38.6823, 29.4082),  // Uşak
        [65] = (38.4891, 43.4089),  // Van
        [66] = (39.8181, 34.8147),  // Yozgat
        [67] = (41.4564, 31.7987),  // Zonguldak
        [68] = (38.3687, 34.0370),  // Aksaray
        [69] = (40.2552, 40.2249),  // Bayburt
        [70] = (37.1759, 33.2287),  // Karaman
        [71] = (39.8468, 33.5153),  // Kırıkkale
        [72] = (37.8812, 41.1351),  // Batman
        [73] = (37.4187, 42.4918),  // Şırnak
        [74] = (41.6344, 32.3375),  // Bartın
        [75] = (41.1105, 42.7022),  // Ardahan
        [76] = (39.8880, 44.0048),  // Iğdır
        [77] = (40.6500, 29.2667),  // Yalova
        [78] = (41.2061, 32.6204),  // Karabük
        [79] = (36.7184, 37.1212),  // Kilis
        [80] = (37.0742, 36.2464),  // Osmaniye
        [81] = (40.8438, 31.1565)   // Düzce
    };

    public static (double Lat, double Lon)? FindByPlate(int plateCode) =>
        ByPlate.TryGetValue(plateCode, out var coords) ? coords : null;
}
