namespace PrayerApp.Core.Domain;

/// <summary>
/// Prayer time calculation methods supported by the Aladhan API.
/// Values match Aladhan's numeric method ids.
/// </summary>
public enum CalculationMethod
{
    /// <summary>Diyanet İşleri Başkanlığı (Türkiye) — default for Turkish users.</summary>
    Diyanet = 13,
    MuslimWorldLeague = 3,
    Isna = 2,
    Egypt = 5,
    UmmAlQura = 4,
    Karachi = 1,
    Tehran = 7,
    Kuwait = 9,
    Qatar = 10,
    Singapore = 11,
    France = 12,
    Russia = 14
}

public static class CalculationMethodExtensions
{
    public static string ToDisplayName(this CalculationMethod method) => method switch
    {
        CalculationMethod.Diyanet => "Diyanet İşleri Başkanlığı (Türkiye)",
        CalculationMethod.MuslimWorldLeague => "Dünya İslam Birliği",
        CalculationMethod.Isna => "Kuzey Amerika İslam Topluluğu (ISNA)",
        CalculationMethod.Egypt => "Mısır Genel Araştırma Kurumu",
        CalculationMethod.UmmAlQura => "Ümmü'l-Kura Üniversitesi (Mekke)",
        CalculationMethod.Karachi => "Karaçi İslami İlimler Üniversitesi",
        CalculationMethod.Tehran => "Tahran Üniversitesi",
        CalculationMethod.Kuwait => "Kuveyt",
        CalculationMethod.Qatar => "Katar",
        CalculationMethod.Singapore => "Singapur",
        CalculationMethod.France => "Fransa İslam Teşkilatları Birliği",
        CalculationMethod.Russia => "Rusya Manevi İdaresi",
        _ => method.ToString()
    };

    public static readonly CalculationMethod[] All =
    [
        CalculationMethod.Diyanet,
        CalculationMethod.MuslimWorldLeague,
        CalculationMethod.Isna,
        CalculationMethod.Egypt,
        CalculationMethod.UmmAlQura,
        CalculationMethod.Karachi,
        CalculationMethod.Tehran,
        CalculationMethod.Kuwait,
        CalculationMethod.Qatar,
        CalculationMethod.Singapore,
        CalculationMethod.France,
        CalculationMethod.Russia
    ];
}
