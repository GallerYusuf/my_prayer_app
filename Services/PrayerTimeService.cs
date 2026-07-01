using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class PrayerTimeService(HttpClient httpClient) : IPrayerTimeService
{
    private const int DiyanetMethod = 13;
    private const string CacheKeyPrefix = "prayer_times_cache_";

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
        try
        {
            var url = $"timingsByCity?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method={DiyanetMethod}";
            var response = await httpClient.GetFromJsonAsync<AladhanResponse>(url, cancellationToken)
                ?? throw new InvalidOperationException("Namaz vakitleri alınamadı.");

            if (response.Code != 200)
                throw new InvalidOperationException($"API hatası: {response.Status}");

            var prayerTimes = BuildPrayerTimes(response.Data.Timings);
            SaveCachedTimes(city, country, prayerTimes);
            return prayerTimes;
        }
        catch (Exception ex) when (IsRecoverableFailure(ex))
        {
            var cachedTimes = LoadCachedTimes(city, country);
            if (cachedTimes is not null)
                return cachedTimes;

            throw new InvalidOperationException(GetUserFriendlyMessage(ex), ex);
        }
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

    private static IReadOnlyList<PrayerTime> BuildPrayerTimes(PrayerTimings timings)
    {
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

    private static void SaveCachedTimes(string city, string country, IReadOnlyList<PrayerTime> prayerTimes)
    {
        var cache = new CachedPrayerTimes
        {
            Date = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            City = city.Trim(),
            Country = country.Trim(),
            Times = prayerTimes
                .Select(p => new CachedPrayerTime
                {
                    Name = p.Name,
                    TurkishName = p.TurkishName,
                    Glyph = p.Glyph,
                    Time = p.Time.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
                })
                .ToList()
        };

        Preferences.Default.Set(BuildCacheKey(city, country), JsonSerializer.Serialize(cache));
    }

    private static IReadOnlyList<PrayerTime>? LoadCachedTimes(string city, string country)
    {
        var json = Preferences.Default.Get(BuildCacheKey(city, country), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var cache = JsonSerializer.Deserialize<CachedPrayerTimes>(json);
            if (cache is null || cache.Date != DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                return null;

            return cache.Times
                .Select(p => new PrayerTime
                {
                    Name = p.Name,
                    TurkishName = p.TurkishName,
                    Glyph = p.Glyph,
                    Time = ParsePrayerTime(p.Time)
                })
                .ToList();
        }
        catch (JsonException)
        {
            Preferences.Default.Remove(BuildCacheKey(city, country));
            return null;
        }
        catch (FormatException)
        {
            Preferences.Default.Remove(BuildCacheKey(city, country));
            return null;
        }
    }

    private static TimeSpan ParsePrayerTime(string value)
    {
        var clean = value.Split(' ')[0];
        return TimeSpan.ParseExact(clean, @"h\:m", CultureInfo.InvariantCulture);
    }

    private static string BuildCacheKey(string city, string country)
    {
        var source = $"{city.Trim().ToLowerInvariant()}|{country.Trim().ToLowerInvariant()}";
        return CacheKeyPrefix + Uri.EscapeDataString(source);
    }

    private static bool IsRecoverableFailure(Exception exception) =>
        exception is HttpRequestException
            or TaskCanceledException
            or JsonException
            or FormatException
            or InvalidOperationException;

    private static string GetUserFriendlyMessage(Exception exception)
    {
        if (exception is TaskCanceledException)
            return "Namaz vakitleri alınırken bağlantı zaman aşımına uğradı. Lütfen internet bağlantınızı kontrol edin.";

        if (exception is HttpRequestException)
            return "Namaz vakitleri alınamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.";

        if (exception is JsonException or FormatException)
            return "Namaz vakti verisi beklenen formatta gelmedi. Lütfen daha sonra tekrar deneyin.";

        return "Bu şehir için namaz vakitleri alınamadı. Şehir ve ülke bilgisini kontrol edip tekrar deneyin.";
    }

    private sealed class CachedPrayerTimes
    {
        public string Date { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<CachedPrayerTime> Times { get; set; } = [];
    }

    private sealed class CachedPrayerTime
    {
        public string Name { get; set; } = string.Empty;
        public string TurkishName { get; set; } = string.Empty;
        public string Glyph { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }
}
