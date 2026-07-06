using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Notifications;

namespace PrayerApp.Core.Services;

public interface INotificationCoordinator
{
    /// <summary>
    /// Rebuilds the rolling notification plan from local data and hands it to the platform scheduler.
    /// Atomic: existing notifications are only cancelled after a new valid plan is computed.
    /// Idempotent: calling repeatedly with unchanged inputs yields the same schedule.
    /// </summary>
    Task<Result<int>> RescheduleAsync(bool force = false, CancellationToken cancellationToken = default);

    /// <summary>Reschedules only if the plan was not already built today (cheap resume path).</summary>
    Task RescheduleIfStaleAsync(CancellationToken cancellationToken = default);
}

public sealed class NotificationCoordinator(
    IPrayerScheduleService scheduleService,
    IQuranRepository quranRepository,
    INotificationScheduler scheduler,
    ISettingsStore settingsStore,
    IKeyValueStore keyValueStore,
    IPersistentNotificationService persistentNotification,
    IClock clock) : INotificationCoordinator
{
    private const string ScheduledIdsKey = "notification_scheduled_ids";
    private const string LastPlanDateKey = "notification_last_plan_date";

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task RescheduleIfStaleAsync(CancellationToken cancellationToken = default)
    {
        var lastPlanDate = keyValueStore.GetString(LastPlanDateKey);
        if (lastPlanDate == clock.Today.ToString("O"))
        {
            // Plan is fresh, but the ongoing notification may have been lost
            // (e.g. force-stop cleared its alarm chain); restore it cheaply.
            await persistentNotification.RefreshAsync(cancellationToken);
            return;
        }

        await RescheduleAsync(cancellationToken: cancellationToken);
    }

    public async Task<Result<int>> RescheduleAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var settings = settingsStore.Get().Sanitized();
            if (!settings.HasCompletedOnboarding)
                return 0;

            var anyCategoryEnabled = settings.PrayerNotificationsEnabled
                || settings.VerseNotificationsEnabled
                || settings.FridayNotificationsEnabled;

            if (!anyCategoryEnabled)
            {
                await CancelPreviousAsync();
                keyValueStore.SetString(LastPlanDateKey, clock.Today.ToString("O"));
                await persistentNotification.RefreshAsync(cancellationToken);
                return 0;
            }

            // Cover today plus the rolling window (works fully offline from SQLite cache).
            var schedule = await scheduleService.GetScheduleAsync(
                clock.Today, NotificationPlanner.RollingDays + 1, force, cancellationToken);

            if (!schedule.IsSuccess)
                return schedule.Error; // keep the existing plan; do not cancel blindly

            var verseContent = settings.VerseNotificationsEnabled
                ? await BuildVerseContentAsync(settings, cancellationToken)
                : null;

            var plan = NotificationPlanner.BuildPlan(
                schedule.Value.Days,
                settings,
                clock.LocalNow,
                verseContent is null
                    ? null
                    : (date, slot) => verseContent.TryGetValue((date, slot), out var content)
                        ? content
                        : ("Günün Ayeti", string.Empty, string.Empty));

            var effectivePlan = plan.Where(n => n.Body.Length > 0 || n.Category != NotificationCategory.Verse).ToList();

            await CancelPreviousAsync();
            await scheduler.ScheduleAsync(effectivePlan, cancellationToken);

            keyValueStore.SetString(ScheduledIdsKey, string.Join(',', effectivePlan.Select(n => n.Id)));
            keyValueStore.SetString(LastPlanDateKey, clock.Today.ToString("O"));

            await persistentNotification.RefreshAsync(cancellationToken);
            return effectivePlan.Count;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task CancelPreviousAsync()
    {
        var raw = keyValueStore.GetString(ScheduledIdsKey);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var ids = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out var id))
                ids.Add(id);
        }

        if (ids.Count > 0)
            await scheduler.CancelAsync(ids);

        keyValueStore.Remove(ScheduledIdsKey);
    }

    private async Task<Dictionary<(DateOnly Date, int Slot), (string Title, string Body, string Payload)>>
        BuildVerseContentAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        var content = new Dictionary<(DateOnly, int), (string, string, string)>();
        var total = await quranRepository.GetTotalVerseCountAsync(cancellationToken);
        if (total <= 0)
            return content;

        for (var dayOffset = 0; dayOffset < NotificationPlanner.RollingDays; dayOffset++)
        {
            var date = clock.Today.AddDays(dayOffset);
            for (var slot = 0; slot < settings.VerseNotificationTimes.Count && slot < 5; slot++)
            {
                var globalIndex = VerseOfDaySelector.GetGlobalIndex(date, total, slot);
                var verse = await quranRepository.GetVerseByGlobalIndexAsync(globalIndex, cancellationToken);
                if (verse is null)
                    continue;

                var body = Truncate($"{verse.Reference} — {verse.Text}", 180);
                var payload = $"{verse.SurahNumber}:{verse.VerseNumber}";
                content[(date, slot)] = ("Günün Ayeti", body, payload);
            }
        }

        return content;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
}
