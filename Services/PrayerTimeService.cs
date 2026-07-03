using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class PrayerTimeService(HttpClient httpClient) : IPrayerTimeService
{
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

    private static readonly string[] HijriMonthsTr =
    [
        "", "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir",
        "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban",
        "Ramazan", "Şevval", "Zilkade", "Zilhicce"
    ];

    public async Task<PrayerDayResult> GetTodayPrayerTimesAsync(
        string city,
        string country,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var address = string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
            var url = $"timingsByAddress?address={Uri.EscapeDataString(address)}";

            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Namaz vakitleri alınamadı (HTTP {(int)response.StatusCode}).");

            var payload = await response.Content.ReadFromJsonAsync<AladhanResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Namaz vakitleri alınamadı.");

            if (payload.Code != 200)
                throw new InvalidOperationException($"API hatası: {payload.Status}");

            var prayerTimes = BuildPrayerTimes(payload.Data.Timings);
            var hijri = payload.Data.Date.Hijri;
            var hijriDate = FormatHijriDate(hijri);
            var hijriMonth = hijri?.Month.Number ?? 0;
            var hijriDay = ParseHijriDay(hijri?.Day);
            var latitude = payload.Data.Meta?.Latitude ?? 0;
            var longitude = payload.Data.Meta?.Longitude ?? 0;
            SaveCachedTimes(city, country, prayerTimes, hijriDate, hijriMonth, hijriDay, latitude, longitude);
            return new PrayerDayResult(prayerTimes, hijriDate, hijriMonth, hijriDay, latitude, longitude);
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

    private static string FormatHijriDate(AladhanHijri? hijri)
    {
        if (hijri is null)
            return string.Empty;

        var monthIndex = Math.Clamp(hijri.Month.Number, 1, 12);
        var monthName = HijriMonthsTr[monthIndex];
        return $"{hijri.Day} {monthName} {hijri.Year}";
    }

    private static int ParseHijriDay(string? day) =>
        int.TryParse(day, out var value) ? value : 0;

    private static void SaveCachedTimes(
        string city,
        string country,
        IReadOnlyList<PrayerTime> prayerTimes,
        string hijriDate,
        int hijriMonth,
        int hijriDay,
        double latitude,
        double longitude)
    {
        var cache = new CachedPrayerTimes
        {
            Date = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            City = city.Trim(),
            Country = country.Trim(),
            HijriDate = hijriDate,
            HijriMonth = hijriMonth,
            HijriDay = hijriDay,
            Latitude = latitude,
            Longitude = longitude,
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

    private static PrayerDayResult? LoadCachedTimes(string city, string country)
    {
        var json = Preferences.Default.Get(BuildCacheKey(city, country), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var cache = JsonSerializer.Deserialize<CachedPrayerTimes>(json);
            if (cache is null || cache.Date != DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                return null;

            return new PrayerDayResult(
                cache.Times
                    .Select(p => new PrayerTime
                    {
                        Name = p.Name,
                        TurkishName = p.TurkishName,
                        Glyph = p.Glyph,
                        Time = ParsePrayerTime(p.Time)
                    })
                    .ToList(),
                cache.HijriDate,
                cache.HijriMonth,
                cache.HijriDay,
                cache.Latitude,
                cache.Longitude);
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
            or OperationCanceledException
            or JsonException
            or FormatException
            or InvalidOperationException;

    private static string GetUserFriendlyMessage(Exception exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        if (message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("TLS", StringComparison.OrdinalIgnoreCase))
        {
            return "Sunucuya güvenli bağlantı kurulamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.";
        }

        if (exception is TaskCanceledException or OperationCanceledException)
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
        public string HijriDate { get; set; } = string.Empty;
        public int HijriMonth { get; set; }
        public int HijriDay { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
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
