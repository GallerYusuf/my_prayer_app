using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;

namespace PrayerApp.Platforms.Android;

/// <summary>
/// Shows a single ongoing (non-dismissible) status-bar notification with today's
/// prayer times and the upcoming prayer. The notification is updated in place
/// (fixed id) by a self-rechaining AlarmManager alarm that fires at each prayer
/// time and at midnight; no foreground service is needed.
/// </summary>
public sealed class PersistentNotificationService(
    IPrayerScheduleService scheduleService,
    ISettingsStore settingsStore,
    IClock clock,
    ILogger<PersistentNotificationService> logger) : IPersistentNotificationService
{
    public const string ChannelId = "channel_persistent_times";

    private const int NotificationId = 900_000_001;
    private const int AlarmRequestCode = 900_000_002;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = settingsStore.Get();
            if (!settings.HasCompletedOnboarding || !settings.PersistentNotificationEnabled)
            {
                Remove();
                return;
            }

            // Today + tomorrow so the "next prayer" rolls over to tomorrow's Fajr after Isha.
            var schedule = await scheduleService.GetScheduleAsync(
                clock.Today, 2, cancellationToken: cancellationToken);

            if (!schedule.IsSuccess)
            {
                logger.LogWarning("Persistent notification skipped: {Error}", schedule.Error.Message);
                return;
            }

            var today = schedule.Value.Days.FirstOrDefault(d => d.Date == clock.Today);
            if (today is null)
                return;

            var now = clock.LocalNow;
            var next = NextPrayerCalculator.FindNext(schedule.Value.Days, now);

            ShowOrUpdate(today, next, settings);
            ScheduleNextUpdate(next, now);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Persistent notification refresh failed");
        }
    }

    private void ShowOrUpdate(PrayerDay today, UpcomingPrayer? next, UserSettings settings)
    {
        var context = global::Android.App.Application.Context;
        var manager = NotificationManagerCompat.From(context);

        if (manager is null || !manager.AreNotificationsEnabled())
            return;

        EnsureChannel(context);

        var title = next is null
            ? "Namaz Vakitleri"
            : $"Sıradaki: {next.Kind.ToTurkishName()} {next.OccursAt:HH\\:mm}" +
              (next.IsTomorrow(clock.Today) ? " (yarın)" : "");

        var line1 = FormatTimes(today, PrayerKind.Fajr, PrayerKind.Sunrise, PrayerKind.Dhuhr);
        var line2 = FormatTimes(today, PrayerKind.Asr, PrayerKind.Maghrib, PrayerKind.Isha);

        var launchIntent = new Intent(context, typeof(MainActivity));
        launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        var contentIntent = PendingIntent.GetActivity(
            context, 0, launchIntent, WithImmutable(PendingIntentFlags.UpdateCurrent));

        var builder = new NotificationCompat.Builder(context, ChannelId);
        builder.SetSmallIcon(Resource.Drawable.notification_icon);
        builder.SetContentTitle(title);
        builder.SetContentText($"{line1}  {line2}");
        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText($"{line1}\n{line2}"));
        builder.SetSubText(settings.Location?.DisplayName);
        builder.SetOngoing(true);
        builder.SetOnlyAlertOnce(true);
        builder.SetSilent(true);
        builder.SetShowWhen(false);
        builder.SetVisibility(NotificationCompat.VisibilityPublic);
        builder.SetCategory(NotificationCompat.CategoryStatus);
        builder.SetColor(unchecked((int)0xFF0E6B5C));
        builder.SetContentIntent(contentIntent);

        var notification = builder.Build();
        if (notification is not null)
            manager.Notify(NotificationId, notification);
    }

    /// <summary>Re-arms the alarm for the next content change: next prayer time or midnight.</summary>
    private void ScheduleNextUpdate(UpcomingPrayer? next, DateTimeOffset now)
    {
        var context = global::Android.App.Application.Context;
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
            return;

        var midnight = new DateTimeOffset(
            clock.Today.AddDays(1).ToDateTime(TimeOnly.MinValue), now.Offset);

        var changeAt = next is not null && next.OccursAt < midnight ? next.OccursAt : midnight;
        var triggerAtMillis = changeAt.AddSeconds(5).ToUnixTimeMilliseconds();

        var pending = BuildAlarmPendingIntent(context, PendingIntentFlags.UpdateCurrent);
        if (pending is null)
            return;

        if (OperatingSystem.IsAndroidVersionAtLeast(31) && !alarmManager.CanScheduleExactAlarms())
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pending);
        else if (OperatingSystem.IsAndroidVersionAtLeast(23))
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pending);
        else
            alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pending);
    }

    private static void Remove()
    {
        var context = global::Android.App.Application.Context;
        NotificationManagerCompat.From(context)?.Cancel(NotificationId);

        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
            return;

        var pending = BuildAlarmPendingIntent(context, PendingIntentFlags.NoCreate);
        if (pending is not null)
        {
            alarmManager.Cancel(pending);
            pending.Cancel();
        }
    }

    private static PendingIntent? BuildAlarmPendingIntent(Context context, PendingIntentFlags flags)
    {
        var intent = new Intent(context, typeof(PersistentNotificationReceiver));
        intent.SetAction(PersistentNotificationReceiver.ActionRefresh);
        return PendingIntent.GetBroadcast(context, AlarmRequestCode, intent, WithImmutable(flags));
    }

    private static PendingIntentFlags WithImmutable(PendingIntentFlags flags) =>
        OperatingSystem.IsAndroidVersionAtLeast(23) ? flags | PendingIntentFlags.Immutable : flags;

    private static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;

        // Low importance: visible in the status bar and shade, but never makes sound.
        var channel = new NotificationChannel(
            ChannelId, "Sürekli Vakit Göstergesi", NotificationImportance.Low)
        {
            Description = "Günün namaz vakitlerini bildirim çubuğunda sabit gösterir"
        };
        channel.SetShowBadge(false);
        manager.CreateNotificationChannel(channel);
    }

    private static string FormatTimes(PrayerDay day, params PrayerKind[] kinds)
    {
        var parts = new List<string>(kinds.Length);
        foreach (var kind in kinds)
        {
            if (day.Times.TryGetValue(kind, out var time))
                parts.Add($"{kind.ToTurkishName()} {time:HH\\:mm}");
        }

        return string.Join("  ·  ", parts);
    }
}
