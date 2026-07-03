namespace PrayerApp.Core.Domain;

public enum PrayerKind
{
    Fajr = 0,
    Sunrise = 1,
    Dhuhr = 2,
    Asr = 3,
    Maghrib = 4,
    Isha = 5
}

public static class PrayerKindExtensions
{
    public static readonly PrayerKind[] All =
        [PrayerKind.Fajr, PrayerKind.Sunrise, PrayerKind.Dhuhr, PrayerKind.Asr, PrayerKind.Maghrib, PrayerKind.Isha];

    /// <summary>Prayers that can be notified about (Sunrise is informational; kept configurable).</summary>
    public static string ToTurkishName(this PrayerKind kind) => kind switch
    {
        PrayerKind.Fajr => "İmsak",
        PrayerKind.Sunrise => "Güneş",
        PrayerKind.Dhuhr => "Öğle",
        PrayerKind.Asr => "İkindi",
        PrayerKind.Maghrib => "Akşam",
        PrayerKind.Isha => "Yatsı",
        _ => kind.ToString()
    };
}
