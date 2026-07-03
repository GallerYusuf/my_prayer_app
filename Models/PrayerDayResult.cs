namespace PrayerApp.Models;

public record PrayerDayResult(
    IReadOnlyList<PrayerTime> Times,
    string HijriDate,
    int HijriMonth,
    int HijriDay,
    double Latitude,
    double Longitude);
