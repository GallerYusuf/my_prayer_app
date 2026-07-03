using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Notifications;
using NotificationCategory = PrayerApp.Core.Notifications.NotificationCategory;

namespace PrayerApp.Services;

/// <summary>
/// Platform notification scheduling via Plugin.LocalNotification.
/// Uses exact native alarms (AlarmManager on Android) so notifications fire while
/// the app is closed; the plugin's boot receiver restores them after reboot.
/// </summary>
public sealed class LocalNotificationScheduler(
    ISettingsStore settingsStore,
    ILogger<LocalNotificationScheduler> logger) : INotificationScheduler
{
    public const string ChannelPrayer = "channel_prayer_time";
    public const string ChannelPreReminder = "channel_pre_reminder";
    public const string ChannelVerse = "channel_verse";
    public const string ChannelOther = "channel_other";

    public async Task<bool> AreNotificationsPermittedAsync()
    {
        var status = await LocalNotificationCenter.Current.GetNotificationPermissionStatus();
        return status.IsEnabled;
    }

    public async Task RequestPermissionAsync()
    {
        var permission = new NotificationPermission
        {
            Android = new AndroidNotificationPermission
            {
                RequestPermissionToScheduleExactAlarm = true
            }
        };

        if (!await LocalNotificationCenter.Current.AreNotificationsEnabled(permission))
            await LocalNotificationCenter.Current.RequestNotificationPermission(permission);
    }

    public async Task ScheduleAsync(
        IReadOnlyList<PlannedNotification> notifications, CancellationToken cancellationToken = default)
    {
        var settings = settingsStore.Get();

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new NotificationRequest
            {
                NotificationId = notification.Id,
                Title = notification.Title,
                Description = notification.Body,
                ReturningData = notification.Payload,
                Silent = !settings.NotificationSoundEnabled,
                Android = new AndroidOptions
                {
                    ChannelId = GetChannelId(notification.Category),
                    IconSmallName = new AndroidIcon("notification_icon"),
                    Color = new AndroidColor(unchecked((int)0xFF0E6B5C)),
                    VibrationPattern = settings.NotificationVibrationEnabled ? [0, 250, 100, 250] : [0L]
                },
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notification.NotifyAt,
                    RepeatType = NotificationRepeat.No,
                    Android = new AndroidScheduleOptions
                    {
                        // AlarmClock mode: exact delivery even in Doze; falls back
                        // gracefully when the exact alarm permission is revoked.
                        ScheduleMode = AndroidScheduleMode.AlarmClock,
                        AlarmType = AndroidAlarmType.RtcWakeup,
                        AllowedDelay = TimeSpan.FromMinutes(1)
                    }
                }
            };

            try
            {
                await LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                // One failed schedule must not break the whole plan.
                logger.LogError(ex, "Failed to schedule notification {Id}", notification.Id);
            }
        }

        logger.LogInformation("Scheduled {Count} notifications", notifications.Count);
    }

    public Task CancelAsync(IReadOnlyCollection<int> notificationIds)
    {
        foreach (var id in notificationIds)
            LocalNotificationCenter.Current.Cancel(id);

        return Task.CompletedTask;
    }

    public async Task<int> GetPendingCountAsync()
    {
        var list = await LocalNotificationCenter.Current.GetPendingNotificationList();
        return list?.Count ?? 0;
    }

    private static string GetChannelId(NotificationCategory category) => category switch
    {
        NotificationCategory.PrayerTime => ChannelPrayer,
        NotificationCategory.PreReminder => ChannelPreReminder,
        NotificationCategory.Verse => ChannelVerse,
        _ => ChannelOther
    };
}
