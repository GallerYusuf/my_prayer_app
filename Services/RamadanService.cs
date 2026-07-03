using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class RamadanService : IRamadanService
{
    public RamadanInfo? BuildInfo(PrayerDayResult day, IReadOnlyList<PrayerTime> prayers, bool enabled)
    {
        if (!enabled || day.HijriMonth != 9 || prayers.Count == 0)
            return null;

        var fajr = prayers.FirstOrDefault(p => p.Name == "Fajr");
        var maghrib = prayers.FirstOrDefault(p => p.Name == "Maghrib");
        if (fajr is null || maghrib is null)
            return null;

        var info = new RamadanInfo
        {
            RamadanDay = day.HijriDay,
            IftarTime = maghrib.Time.ToString(@"hh\:mm"),
            SahurTime = fajr.Time.ToString(@"hh\:mm")
        };

        return UpdateCountdown(info, prayers);
    }

    public RamadanInfo? UpdateCountdown(RamadanInfo info, IReadOnlyList<PrayerTime> prayers)
    {
        var maghrib = prayers.FirstOrDefault(p => p.Name == "Maghrib");
        var fajr = prayers.FirstOrDefault(p => p.Name == "Fajr");
        if (maghrib is null || fajr is null)
            return info;

        var now = DateTime.Now.TimeOfDay;
        TimeSpan remaining;
        string label;
        bool iftarNext;

        if (now < maghrib.Time)
        {
            remaining = maghrib.Time - now;
            label = "İftara kalan";
            iftarNext = true;
        }
        else
        {
            var untilMidnight = TimeSpan.FromHours(24) - now;
            remaining = untilMidnight + fajr.Time;
            label = "Sahura kalan";
            iftarNext = false;
        }

        return info with
        {
            NextEventLabel = label,
            CountdownText = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}",
            IsIftarNext = iftarNext
        };
    }
}
