using System.Globalization;
using System.Text.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class PrayerTrackingService : IPrayerTrackingService
{
    private const string StreakKey = "prayer_track_streak";
    private const string LastFullDateKey = "prayer_track_last_full";
    private static readonly string[] TrackablePrayers = ["Fajr", "Dhuhr", "Asr", "Maghrib", "Isha"];

    private static readonly (string Name, string Turkish, string Glyph)[] TrackDefinitions =
    [
        ("Fajr", "Sabah", "🌙"),
        ("Dhuhr", "Öğle", "☀️"),
        ("Asr", "İkindi", "🌤️"),
        ("Maghrib", "Akşam", "🌇"),
        ("Isha", "Yatsı", "🌃")
    ];

    public IReadOnlyList<PrayerTrackItem> GetTodayTrackItems()
    {
        var completed = LoadCompletedToday();
        return TrackDefinitions
            .Select(p => new PrayerTrackItem
            {
                Name = p.Name,
                TurkishName = p.Turkish,
                Glyph = p.Glyph,
                IsCompleted = completed.Contains(p.Name)
            })
            .ToList();
    }

    public void Toggle(string prayerName)
    {
        if (!TrackablePrayers.Contains(prayerName))
            return;

        var completed = LoadCompletedToday();
        if (completed.Contains(prayerName))
            completed.Remove(prayerName);
        else
            completed.Add(prayerName);

        SaveCompletedToday(completed);
        UpdateStreakIfNeeded(completed);
    }

    public int GetCompletedCountToday() => LoadCompletedToday().Count;

    public int GetStreak() => Preferences.Default.Get(StreakKey, 0);

    public string GetProgressText()
    {
        var done = GetCompletedCountToday();
        return $"{done}/{TrackablePrayers.Length} kılındı";
    }

    private static string TodayKey() =>
        $"prayer_track_{DateTime.Today:yyyy-MM-dd}";

    private static HashSet<string> LoadCompletedToday()
    {
        var json = Preferences.Default.Get(TodayKey(), string.Empty);
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

    private static void SaveCompletedToday(HashSet<string> completed) =>
        Preferences.Default.Set(TodayKey(), JsonSerializer.Serialize(completed));

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
