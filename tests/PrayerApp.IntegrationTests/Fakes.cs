using System.Globalization;
using System.Net;
using System.Text;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;

namespace PrayerApp.IntegrationTests;

/// <summary>Fixed-time clock for deterministic tests.</summary>
public sealed class FakeClock(DateTimeOffset localNow) : IClock
{
    public DateTimeOffset LocalNow { get; set; } = localNow;
    public DateTimeOffset UtcNow => LocalNow.ToUniversalTime();
    public DateOnly Today => DateOnly.FromDateTime(LocalNow.Date);
}

/// <summary>HTTP handler returning canned responses; never touches the network.</summary>
public sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public int RequestCount { get; private set; }
    public List<string> RequestedUrls { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        RequestedUrls.Add(request.RequestUri?.ToString() ?? string.Empty);
        return Task.FromResult(responder(request));
    }
}

public sealed class FakeSettingsStore : ISettingsStore
{
    public UserSettings Current { get; set; } = new()
    {
        HasCompletedOnboarding = true,
        Location = new GeoLocation
        {
            Latitude = 38.5012,
            Longitude = 43.3730,
            DisplayName = "Van",
            IsFromDevice = false
        }
    };

    public UserSettings Get() => Current;
    public void Save(UserSettings settings) => Current = settings;
}

public sealed class FakePrayerTimeProvider : IPrayerTimeProvider
{
    public Result<IReadOnlyList<PrayerDay>>? NextResult { get; set; }
    public int CallCount { get; private set; }

    public Task<Result<IReadOnlyList<PrayerDay>>> GetPrayerDaysAsync(
        GeoLocation location, CalculationMethod method, DateOnly from, int dayCount,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(NextResult ?? Result<IReadOnlyList<PrayerDay>>.Success(MakeDays(from, dayCount)));
    }

    public static IReadOnlyList<PrayerDay> MakeDays(DateOnly from, int count)
    {
        var days = new List<PrayerDay>(count);
        for (var i = 0; i < count; i++)
        {
            days.Add(new PrayerDay
            {
                Date = from.AddDays(i),
                Times = new Dictionary<PrayerKind, TimeOnly>
                {
                    [PrayerKind.Fajr] = new(4, 30),
                    [PrayerKind.Sunrise] = new(6, 0),
                    [PrayerKind.Dhuhr] = new(12, 45),
                    [PrayerKind.Asr] = new(16, 30),
                    [PrayerKind.Maghrib] = new(19, 45),
                    [PrayerKind.Isha] = new(21, 15)
                }
            });
        }

        return days;
    }
}

public static class AladhanJson
{
    /// <summary>Builds a minimal valid Aladhan calendar month response.</summary>
    public static string Month(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var sb = new StringBuilder();
        sb.Append("{\"code\":200,\"status\":\"OK\",\"data\":[");
        for (var d = 1; d <= daysInMonth; d++)
        {
            if (d > 1)
                sb.Append(',');
            var date = new DateOnly(year, month, d);
            sb.Append("{\"timings\":{\"Fajr\":\"04:30 (+03)\",\"Sunrise\":\"06:00 (+03)\",\"Dhuhr\":\"12:45 (+03)\",");
            sb.Append("\"Asr\":\"16:30 (+03)\",\"Maghrib\":\"19:45 (+03)\",\"Isha\":\"21:15 (+03)\"},");
            sb.Append("\"date\":{\"gregorian\":{\"date\":\"");
            sb.Append(date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            sb.Append("\"},\"hijri\":{\"day\":\"");
            sb.Append(d);
            sb.Append("\",\"year\":\"1447\",\"month\":{\"number\":1}}}}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    public static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
}
