using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Services;

/// <summary>A time window in which prayer is makruh (kerahat).</summary>
public sealed record KerahatWindow(string Name, DateTimeOffset Start, DateTimeOffset End)
{
    public bool Contains(DateTimeOffset moment) => moment >= Start && moment < End;
}

/// <summary>
/// Pure calculation of the three daily kerahat windows from a day's prayer times:
/// while the sun rises (sunrise + 45 min), at its zenith (45 min before Dhuhr)
/// and while it sets (45 min before Maghrib). The 45-minute convention follows
/// common Turkish practice; exact fiqh opinions vary by a few minutes.
/// </summary>
public static class KerahatCalculator
{
    public static readonly TimeSpan WindowLength = TimeSpan.FromMinutes(45);

    public static IReadOnlyList<KerahatWindow> GetWindows(PrayerDay day, TimeSpan utcOffset)
    {
        var windows = new List<KerahatWindow>(3);

        if (day.Times.TryGetValue(PrayerKind.Sunrise, out var sunrise))
        {
            var start = At(day.Date, sunrise, utcOffset);
            windows.Add(new KerahatWindow("Güneş doğarken", start, start + WindowLength));
        }

        if (day.Times.TryGetValue(PrayerKind.Dhuhr, out var dhuhr))
        {
            var end = At(day.Date, dhuhr, utcOffset);
            windows.Add(new KerahatWindow("Güneş tepe noktasında", end - WindowLength, end));
        }

        if (day.Times.TryGetValue(PrayerKind.Maghrib, out var maghrib))
        {
            var end = At(day.Date, maghrib, utcOffset);
            windows.Add(new KerahatWindow("Güneş batarken", end - WindowLength, end));
        }

        return windows;
    }

    /// <summary>The window containing <paramref name="now"/>, or null outside kerahat times.</summary>
    public static KerahatWindow? GetCurrent(PrayerDay day, DateTimeOffset now) =>
        GetWindows(day, now.Offset).FirstOrDefault(w => w.Contains(now));

    private static DateTimeOffset At(DateOnly date, TimeOnly time, TimeSpan offset) =>
        new(date.ToDateTime(time), offset);
}
