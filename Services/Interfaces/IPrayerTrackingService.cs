using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public sealed record DailyPrayerSummary(DateOnly Date, string DayLabel, int CompletedCount, int TotalCount = 5)
{
    public string CountText => $"{CompletedCount}/{TotalCount}";
    public double Progress => TotalCount > 0 ? (double)CompletedCount / TotalCount : 0;
}

public interface IPrayerTrackingService
{
    IReadOnlyList<PrayerTrackItem> GetTodayTrackItems();
    bool IsCompletedToday(string prayerName);
    void Toggle(string prayerName);
    int GetCompletedCountToday();
    int GetStreak();
    string GetProgressText();
    IReadOnlyList<DailyPrayerSummary> GetRecentDaysSummary(int days = 7);
}
