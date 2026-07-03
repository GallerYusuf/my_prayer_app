using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Notifications;

/// <summary>
/// Pure planner that turns cached prayer days + user settings into a rolling
/// list of notifications. Idempotent: same inputs always produce the same plan
/// (same ids, same times), so re-scheduling is safe.
/// </summary>
public static class NotificationPlanner
{
    /// <summary>How many days ahead the rolling schedule covers.</summary>
    public const int RollingDays = 3;

    public static IReadOnlyList<PlannedNotification> BuildPlan(
        IReadOnlyList<PrayerDay> days,
        UserSettings settings,
        DateTimeOffset localNow,
        Func<DateOnly, int, (string Title, string Body, string Payload)>? verseContentFactory = null)
    {
        var plan = new List<PlannedNotification>();
        var horizon = DateOnly.FromDateTime(localNow.Date).AddDays(RollingDays);

        foreach (var day in days.OrderBy(d => d.Date))
        {
            if (day.Date > horizon)
                break;

            var adjusted = day.WithAdjustments(settings.Adjustments);
            AddPrayerNotifications(plan, adjusted, settings, localNow);
        }

        if (settings.FridayNotificationsEnabled)
            AddFridayNotification(plan, settings, localNow);

        if (settings.VerseNotificationsEnabled && verseContentFactory is not null)
            AddVerseNotifications(plan, settings, localNow, verseContentFactory);

        return plan
            .Where(n => n.NotifyAt > localNow)
            .OrderBy(n => n.NotifyAt)
            .ToList();
    }

    private static void AddPrayerNotifications(
        List<PlannedNotification> plan, PrayerDay day, UserSettings settings, DateTimeOffset localNow)
    {
        foreach (var kind in PrayerKindExtensions.All)
        {
            if (!settings.IsPrayerNotificationEnabled(kind) || !day.Times.TryGetValue(kind, out var time))
                continue;

            var occursAt = new DateTimeOffset(day.Date.ToDateTime(time), localNow.Offset);
            var name = kind.ToTurkishName();

            plan.Add(new PlannedNotification(
                NotificationIds.ForPrayer(day.Date, kind),
                NotificationCategory.PrayerTime,
                $"{name} Vakti",
                $"{name} vakti girdi ({time:HH\\:mm}).",
                occursAt));

            if (settings.MinutesBeforePrayer > 0)
            {
                var remindAt = occursAt.AddMinutes(-settings.MinutesBeforePrayer);
                plan.Add(new PlannedNotification(
                    NotificationIds.ForPreReminder(day.Date, kind),
                    NotificationCategory.PreReminder,
                    $"{name} vaktine {settings.MinutesBeforePrayer} dakika",
                    $"{name} vakti {time:HH\\:mm}'te giriyor.",
                    remindAt));
            }
        }
    }

    private static void AddFridayNotification(
        List<PlannedNotification> plan, UserSettings settings, DateTimeOffset localNow)
    {
        var today = DateOnly.FromDateTime(localNow.Date);
        for (var i = 0; i <= 7; i++)
        {
            var date = today.AddDays(i);
            if (date.DayOfWeek != DayOfWeek.Friday)
                continue;

            var notifyAt = new DateTimeOffset(date.ToDateTime(settings.FridayNotificationTime), localNow.Offset);
            if (notifyAt > localNow)
            {
                plan.Add(new PlannedNotification(
                    NotificationIds.ForFriday(date),
                    NotificationCategory.Friday,
                    "Cuma Hatırlatıcısı",
                    "Hayırlı cumalar! Cuma namazı için hazırlık vakti.",
                    notifyAt));
                break;
            }
        }
    }

    private static void AddVerseNotifications(
        List<PlannedNotification> plan,
        UserSettings settings,
        DateTimeOffset localNow,
        Func<DateOnly, int, (string Title, string Body, string Payload)> contentFactory)
    {
        var today = DateOnly.FromDateTime(localNow.Date);

        for (var dayOffset = 0; dayOffset < RollingDays; dayOffset++)
        {
            var date = today.AddDays(dayOffset);
            var slot = 0;
            foreach (var time in settings.VerseNotificationTimes.Take(5))
            {
                var notifyAt = new DateTimeOffset(date.ToDateTime(time), localNow.Offset);
                if (notifyAt > localNow)
                {
                    var (title, body, payload) = contentFactory(date, slot);
                    plan.Add(new PlannedNotification(
                        NotificationIds.ForVerse(date, slot),
                        NotificationCategory.Verse,
                        title,
                        body,
                        notifyAt,
                        payload));
                }

                slot++;
            }
        }
    }
}
