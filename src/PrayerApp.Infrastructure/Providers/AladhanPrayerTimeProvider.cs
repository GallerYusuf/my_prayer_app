using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;

namespace PrayerApp.Infrastructure.Providers;

/// <summary>
/// Aladhan API provider using coordinate-based monthly calendar queries with an
/// explicit calculation method. One retry on transient failures; strict response validation.
/// </summary>
public sealed class AladhanPrayerTimeProvider(HttpClient httpClient, ILogger<AladhanPrayerTimeProvider> logger)
    : IPrayerTimeProvider
{
    private static readonly string[] HijriMonthsTr =
    [
        "", "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir",
        "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban",
        "Ramazan", "Şevval", "Zilkade", "Zilhicce"
    ];

    public async Task<Result<IReadOnlyList<PrayerDay>>> GetPrayerDaysAsync(
        GeoLocation location,
        CalculationMethod method,
        DateOnly from,
        int dayCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var to = from.AddDays(dayCount - 1);
            var days = new List<PrayerDay>();

            // Fetch every month the range touches (usually 2 requests).
            var cursor = new DateOnly(from.Year, from.Month, 1);
            var lastMonth = new DateOnly(to.Year, to.Month, 1);

            while (cursor <= lastMonth)
            {
                var monthDays = await FetchMonthAsync(location, method, cursor.Year, cursor.Month, cancellationToken);
                days.AddRange(monthDays.Where(d => d.Date >= from && d.Date <= to));
                cursor = cursor.AddMonths(1);
            }

            if (days.Count == 0)
                return AppError.InvalidData("Sunucudan geçerli namaz vakti verisi alınamadı.");

            return days;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AppError.Timeout();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Aladhan request failed");
            return AppError.Network("Namaz vakitleri alınamadı. İnternet bağlantınızı kontrol edin.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Aladhan response could not be parsed");
            return AppError.InvalidData("Namaz vakti verisi beklenen formatta gelmedi.");
        }
    }

    private async Task<List<PrayerDay>> FetchMonthAsync(
        GeoLocation location, CalculationMethod method, int year, int month, CancellationToken cancellationToken)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"calendar/{year}/{month}?latitude={location.Latitude:F6}&longitude={location.Longitude:F6}&method={(int)method}");

        AladhanCalendarResponse? payload = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(url, cancellationToken);
                if ((int)response.StatusCode >= 500 && attempt == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                payload = await response.Content.ReadFromJsonAsync<AladhanCalendarResponse>(
                    cancellationToken: cancellationToken);
                break;
            }
            catch (HttpRequestException) when (attempt == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        if (payload is null || payload.Code != 200)
            throw new JsonException($"Unexpected Aladhan response (code {payload?.Code.ToString(CultureInfo.InvariantCulture) ?? "null"}).");

        var result = new List<PrayerDay>(payload.Data.Count);
        foreach (var item in payload.Data)
        {
            var day = TryMapDay(item);
            if (day is not null)
                result.Add(day);
        }

        return result;
    }

    private static PrayerDay? TryMapDay(AladhanCalendarDay item)
    {
        if (item.Timings is null || item.Date?.Gregorian is null)
            return null;

        if (!DateOnly.TryParseExact(item.Date.Gregorian.Date, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        var times = new Dictionary<PrayerKind, TimeOnly>(6);
        if (!TryAdd(times, PrayerKind.Fajr, item.Timings.Fajr) ||
            !TryAdd(times, PrayerKind.Sunrise, item.Timings.Sunrise) ||
            !TryAdd(times, PrayerKind.Dhuhr, item.Timings.Dhuhr) ||
            !TryAdd(times, PrayerKind.Asr, item.Timings.Asr) ||
            !TryAdd(times, PrayerKind.Maghrib, item.Timings.Maghrib) ||
            !TryAdd(times, PrayerKind.Isha, item.Timings.Isha))
        {
            return null;
        }

        var hijri = item.Date.Hijri;
        var hijriMonth = hijri?.Month?.Number ?? 0;
        _ = int.TryParse(hijri?.Day, out var hijriDay);
        _ = int.TryParse(hijri?.Year, out var hijriYear);

        var hijriText = hijriMonth is >= 1 and <= 12 && hijriDay > 0
            ? $"{hijriDay} {HijriMonthsTr[hijriMonth]} {hijriYear}"
            : string.Empty;

        return new PrayerDay
        {
            Date = date,
            Times = times,
            HijriDateText = hijriText,
            HijriMonth = hijriMonth,
            HijriDay = hijriDay,
            HijriYear = hijriYear
        };
    }

    private static bool TryAdd(Dictionary<PrayerKind, TimeOnly> times, PrayerKind kind, string raw)
    {
        // Timings come as "05:23 (+03)" — strip the timezone suffix.
        var clean = raw.Split(' ')[0];
        if (!TimeOnly.TryParseExact(clean, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) &&
            !TimeOnly.TryParseExact(clean, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
        {
            return false;
        }

        times[kind] = time;
        return true;
    }
}
