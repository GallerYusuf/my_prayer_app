using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Services;

/// <summary>
/// Pure calculation of the next upcoming prayer across day boundaries.
/// Works on adjusted <see cref="PrayerDay"/> data and absolute local time,
/// so midnight rollover and "first prayer of tomorrow" are handled correctly.
/// </summary>
public static class NextPrayerCalculator
{
    /// <summary>
    /// Finds the next prayer at or after <paramref name="localNow"/>.
    /// <paramref name="days"/> should contain at least today; tomorrow enables correct
    /// post-Isha countdown to next Fajr.
    /// </summary>
    public static UpcomingPrayer? FindNext(IReadOnlyList<PrayerDay> days, DateTimeOffset localNow)
    {
        foreach (var occurrence in EnumerateOccurrences(days))
        {
            var occursAt = ToLocalDateTimeOffset(occurrence.Date, occurrence.Time, localNow.Offset);
            if (occursAt > localNow)
                return new UpcomingPrayer(occurrence.Kind, occurrence.Date, occursAt, occursAt - localNow);
        }

        return null;
    }

    /// <summary>Returns prayers of <paramref name="date"/> partitioned by state relative to now.</summary>
    public static (IReadOnlyList<PrayerKind> Passed, PrayerKind? Current) GetDayState(
        PrayerDay day, DateTimeOffset localNow, PrayerKind? nextKind, DateOnly? nextDate)
    {
        var passed = new List<PrayerKind>();
        var today = DateOnly.FromDateTime(localNow.Date);

        foreach (var kind in PrayerKindExtensions.All)
        {
            if (!day.Times.TryGetValue(kind, out var time))
                continue;

            var occursAt = ToLocalDateTimeOffset(day.Date, time, localNow.Offset);
            var isNext = nextKind == kind && nextDate == day.Date;
            if (occursAt <= localNow && !isNext && day.Date <= today)
                passed.Add(kind);
        }

        return (passed, passed.Count > 0 ? passed[^1] : null);
    }

    private static IEnumerable<(DateOnly Date, PrayerKind Kind, TimeOnly Time)> EnumerateOccurrences(
        IReadOnlyList<PrayerDay> days)
    {
        foreach (var day in days.OrderBy(d => d.Date))
        {
            foreach (var kind in PrayerKindExtensions.All)
            {
                if (day.Times.TryGetValue(kind, out var time))
                    yield return (day.Date, kind, time);
            }
        }
    }

    private static DateTimeOffset ToLocalDateTimeOffset(DateOnly date, TimeOnly time, TimeSpan offset) =>
        new(date.ToDateTime(time), offset);
}
