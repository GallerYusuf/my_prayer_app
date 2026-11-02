using System.Globalization;
using System.Text.Json;
using PrayerApp.Core.Common;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class PrayerTrackingService : IPrayerTrackingService
{
    private const string StreakKey = StorageKeys.PrayerTrackStreak;

    /// <summary>Upper bound for the backwards streak walk (protects against pathological data).</summary>
    private const int MaxStreakDays = 3660;
    private static readonly CultureInfo TurkishCulture = new("tr-TR");
    private static readonly string[] TrackablePrayers = ["Fajr", "Dhuhr", "Asr", "Maghrib", "Isha"];

    private static readonly (string Name, string Turkish, string Icon)[] TrackDefinitions =
    [
        ("Fajr", "İmsak / Sabah", "icon_fajr.png"),
        ("Dhuhr", "Öğle", "icon_dhuhr.png"),
        ("Asr", "İkindi", "icon_asr.png"),
        ("Maghrib", "Akşam", "icon_maghrib.png"),
        ("Isha", "Yatsı", "icon_isha.png")
    ];

    public IReadOnlyList<PrayerTrackItem> GetTodayTrackItems()
    {
        var completed = LoadCompletedForDate(DateOnly.FromDateTime(DateTime.Today));
        return TrackDefinitions
            .Select(p => new PrayerTrackItem
            {
                Name = p.Name,
                TurkishName = p.Turkish,
                Icon = p.Icon,
                IsCompleted = completed.Contains(p.Name)
            })
            .ToList();
    }

    public bool IsCompletedToday(string prayerName)
    {
        if (!TrackablePrayers.Contains(prayerName))
            return false;

        return LoadCompletedForDate(DateOnly.FromDateTime(DateTime.Today)).Contains(prayerName);
    }

    public void Toggle(string prayerName)
    {
        if (!TrackablePrayers.Contains(prayerName))
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var completed = LoadCompletedForDate(today);
        if (completed.Contains(prayerName))
            completed.Remove(prayerName);
        else
            completed.Add(prayerName);

        SaveCompletedForDate(today, completed);

        // Cached copy for backups / older readers; the source of truth is the daily records.
        Preferences.Default.Set(StreakKey, GetStreak());
    }

    public int GetCompletedCountToday() => LoadCompletedForDate(DateOnly.FromDateTime(DateTime.Today)).Count;

    /// <summary>
    /// Consecutive fully-completed days, derived from the daily records so it is always
    /// consistent: today counts once all five are marked; an unfinished today does not
    /// break a streak that ran through yesterday, but a missed day resets it to 0, and
    /// un-marking a prayer immediately lowers it again.
    /// </summary>
    public int GetStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var date = IsFullDay(today) ? today : today.AddDays(-1);

        var streak = 0;
        while (streak < MaxStreakDays && IsFullDay(date))
        {
            streak++;
            date = date.AddDays(-1);
        }

        return streak;
    }

    public string GetProgressText()
    {
        var done = GetCompletedCountToday();
        return $"{done}/{TrackablePrayers.Length} vakit kılındı";
    }

    public IReadOnlyList<DailyPrayerSummary> GetRecentDaysSummary(int days = 7)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var result = new List<DailyPrayerSummary>(days);
        for (var i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var completed = LoadCompletedForDate(date).Count;
            var label = i == 0
                ? "Bugün"
                : date.ToDateTime(TimeOnly.MinValue).ToString("ddd", TurkishCulture);
            result.Add(new DailyPrayerSummary(date, label, completed, TrackablePrayers.Length));
        }

        return result;
    }

    private static string DateKey(DateOnly date) => StorageKeys.PrayerTrackDay(date);

    private static bool IsFullDay(DateOnly date) =>
        LoadCompletedForDate(date).Count(TrackablePrayers.Contains) >= TrackablePrayers.Length;

    private static HashSet<string> LoadCompletedForDate(DateOnly date)
    {
        var json = Preferences.Default.Get(DateKey(date), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void SaveCompletedForDate(DateOnly date, HashSet<string> completed)
    {
        Preferences.Default.Set(DateKey(date), JsonSerializer.Serialize(completed));
        RememberFirstTrackedDate(date);
    }

    /// <summary>Lets the backup service know how far back daily records go.</summary>
    private static void RememberFirstTrackedDate(DateOnly date)
    {
        var raw = Preferences.Default.Get(StorageKeys.PrayerTrackFirstDate, string.Empty);
        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first)
            && first <= date)
        {
            return;
        }

        Preferences.Default.Set(StorageKeys.PrayerTrackFirstDate,
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
