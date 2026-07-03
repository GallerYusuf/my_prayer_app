using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public interface IRamadanService
{
    RamadanInfo? BuildInfo(PrayerDayResult day, IReadOnlyList<PrayerTime> prayers, bool enabled);
    RamadanInfo? UpdateCountdown(RamadanInfo info, IReadOnlyList<PrayerTime> prayers);
}
