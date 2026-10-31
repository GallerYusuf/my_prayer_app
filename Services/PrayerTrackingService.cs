using System.Globalization;
using System.Text.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class PrayerTrackingService : IPrayerTrackingService
{
    private const string StreakKey = "prayer_track_streak";
    private const string LastFullDateKey = "prayer_track_last_full";
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
        UpdateStreakIfNeeded(completed);
    }

    public int GetCompletedCountToday() => LoadCompletedForDate(DateOnly.FromDateTime(DateTime.Today)).Count;

    public int GetStreak() => Preferences.Default.Get(StreakKey, 0);

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

    private static string DateKey(DateOnly date) =>
        $"prayer_track_{date:yyyy-MM-dd}";

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

    private static void SaveCompletedForDate(DateOnly date, HashSet<string> completed) =>
        Preferences.Default.Set(DateKey(date), JsonSerializer.Serialize(completed));

    private static void UpdateStreakIfNeeded(HashSet<string> completed)
    {
        if (completed.Count < TrackablePrayers.Length)
            return;

        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lastFull = Preferences.Default.Get(LastFullDateKey, string.Empty);
        if (lastFull == today)
            return;

        var streak = Preferences.Default.Get(StreakKey, 0);
        if (!string.IsNullOrEmpty(lastFull) &&
            DateTime.TryParseExact(lastFull, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastDate) &&
            lastDate == DateTime.Today.AddDays(-1))
        {
            streak++;
        }
        else
        {
            streak = 1;
        }

        Preferences.Default.Set(StreakKey, streak);
        Preferences.Default.Set(LastFullDateKey, today);
    }
}
