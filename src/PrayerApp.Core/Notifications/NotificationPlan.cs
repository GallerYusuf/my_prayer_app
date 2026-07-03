using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Notifications;

public enum NotificationCategory
{
    PrayerTime = 1,
    PreReminder = 2,
    Verse = 3,
    Friday = 4
}

/// <summary>A single notification to be scheduled with the platform.</summary>
public sealed record PlannedNotification(
    int Id,
    NotificationCategory Category,
    string Title,
    string Body,
    DateTimeOffset NotifyAt,
    string Payload = "");

public static class NotificationIds
{
    // Category bases keep ids deterministic, unique and stable across reschedules.
    private const int PrayerBase = 100_000_000;
    private const int PreReminderBase = 200_000_000;
    private const int VerseBase = 300_000_000;
    private const int FridayBase = 400_000_000;

    public static int ForPrayer(DateOnly date, PrayerKind kind) =>
        PrayerBase + date.DayNumber * 10 + (int)kind;

    public static int ForPreReminder(DateOnly date, PrayerKind kind) =>
        PreReminderBase + date.DayNumber * 10 + (int)kind;

    public static int ForVerse(DateOnly date, int slot) =>
        VerseBase + date.DayNumber * 10 + slot;

    public static int ForFriday(DateOnly date) =>
        FridayBase + date.DayNumber;

    public static NotificationCategory? GetCategory(int id) => id switch
    {
        >= FridayBase => NotificationCategory.Friday,
        >= VerseBase => NotificationCategory.Verse,
        >= PreReminderBase => NotificationCategory.PreReminder,
        >= PrayerBase => NotificationCategory.PrayerTime,
        _ => null
    };
}
