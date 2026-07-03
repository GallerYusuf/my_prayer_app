using PrayerApp.Core.Domain;

namespace PrayerApp.UnitTests;

internal static class TestData
{
    public static PrayerDay Day(DateOnly date,
        string fajr = "04:30", string sunrise = "06:00", string dhuhr = "12:45",
        string asr = "16:30", string maghrib = "19:45", string isha = "21:15") => new()
    {
        Date = date,
        Times = new Dictionary<PrayerKind, TimeOnly>
        {
            [PrayerKind.Fajr] = TimeOnly.Parse(fajr),
            [PrayerKind.Sunrise] = TimeOnly.Parse(sunrise),
            [PrayerKind.Dhuhr] = TimeOnly.Parse(dhuhr),
            [PrayerKind.Asr] = TimeOnly.Parse(asr),
            [PrayerKind.Maghrib] = TimeOnly.Parse(maghrib),
            [PrayerKind.Isha] = TimeOnly.Parse(isha)
        }
    };

    public static DateTimeOffset At(DateOnly date, string time, double offsetHours = 3) =>
        new(date.ToDateTime(TimeOnly.Parse(time)), TimeSpan.FromHours(offsetHours));
}
