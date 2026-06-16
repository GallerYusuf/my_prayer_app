using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public interface IPrayerTimeService
{
    Task<IReadOnlyList<PrayerTime>> GetTodayPrayerTimesAsync(string city, string country, CancellationToken cancellationToken = default);
    (PrayerTime? NextPrayer, TimeSpan Remaining) GetNextPrayer(IReadOnlyList<PrayerTime> prayers);
}
