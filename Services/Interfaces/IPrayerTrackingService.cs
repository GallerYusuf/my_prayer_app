using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public interface IPrayerTrackingService
{
    IReadOnlyList<PrayerTrackItem> GetTodayTrackItems();
    void Toggle(string prayerName);
    int GetCompletedCountToday();
    int GetStreak();
    string GetProgressText();
}
