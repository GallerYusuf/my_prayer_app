using System.Globalization;
using System.Net.Http.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class PrayerTimeService(HttpClient httpClient) : IPrayerTimeService
{
    private static readonly (string Key, string TurkishName, string Glyph)[] PrayerMap =
    [
        ("Fajr", "İmsak", "🌙"),
        ("Sunrise", "Güneş", "🌅"),
        ("Dhuhr", "Öğle", "☀️"),
        ("Asr", "İkindi", "🌤️"),
        ("Maghrib", "Akşam", "🌇"),
        ("Isha", "Yatsı", "🌃")
    ];

    public async Task<IReadOnlyList<PrayerTime>> GetTodayPrayerTimesAsync(
        string city,
        string country,
        CancellationToken cancellationToken = default)
    {
        var url = $"timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}";
        var response = await httpClient.GetFromJsonAsync<AladhanResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Namaz vakitleri alınamadı.");

        if (response.Code != 200)
            throw new InvalidOperationException($"API hatası: {response.Status}");

        var timings = response.Data.Timings;
        var timeByKey = new Dictionary<string, string>
        {
            ["Fajr"] = timings.Fajr,
            ["Sunrise"] = timings.Sunrise,
            ["Dhuhr"] = timings.Dhuhr,
            ["Asr"] = timings.Asr,
            ["Maghrib"] = timings.Maghrib,
            ["Isha"] = timings.Isha
        };

        return PrayerMap
            .Select(p => new PrayerTime
            {
                Name = p.Key,
                TurkishName = p.TurkishName,
                Glyph = p.Glyph,
                Time = ParsePrayerTime(timeByKey[p.Key])
            })
            .ToList();
    }

    public (PrayerTime? NextPrayer, TimeSpan Remaining) GetNextPrayer(IReadOnlyList<PrayerTime> prayers)
    {
        var now = DateTime.Now.TimeOfDay;

        foreach (var prayer in prayers.OrderBy(p => p.Time))
        {
            if (prayer.Time > now)
                return (prayer, prayer.Time - now);
        }

        var firstTomorrow = prayers.OrderBy(p => p.Time).First();
        var untilMidnight = TimeSpan.FromHours(24) - now;
        return (firstTomorrow, untilMidnight + firstTomorrow.Time);
    }

    private static TimeSpan ParsePrayerTime(string value)
    {
        var clean = value.Split(' ')[0];
        return TimeSpan.ParseExact(clean, @"h\:m", CultureInfo.InvariantCulture);
    }
}
