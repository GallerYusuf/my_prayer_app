namespace PrayerApp.Models;

public class AppSettings
{
    public string City { get; set; } = "Van";
    public string Country { get; set; } = "Turkey";
    public bool HasCompletedOnboarding { get; set; }

    public bool PrayerNotificationsEnabled { get; set; } = true;
    public bool NotifyAtPrayerTime { get; set; } = true;
    public int MinutesBeforePrayer { get; set; } = 10;

    public bool FridayNotificationsEnabled { get; set; } = true;
    public TimeSpan FridayNotificationTime { get; set; } = new(11, 30, 0);

    public bool VerseNotificationsEnabled { get; set; } = true;
    public int VerseNotificationCount { get; set; } = 3;
    public List<TimeSpan> VerseNotificationTimes { get; set; } =
    [
        new(10, 0, 0),
        new(14, 30, 0),
        new(20, 0, 0)
    ];
}
