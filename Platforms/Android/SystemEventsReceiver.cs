using Android.App;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Services;

namespace PrayerApp.Platforms.Android;

/// <summary>
/// Reschedules the notification plan when the device time, timezone or the
/// exact-alarm permission changes. Boot/app-update rescheduling is handled by
/// Plugin.LocalNotification's own restore receiver; this covers the rest.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter([
    Intent.ActionTimeChanged,
    Intent.ActionTimezoneChanged,
    "android.app.action.SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED"
])]
public sealed class SystemEventsReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        var services = IPlatformApplication.Current?.Services;
        if (services is null)
            return;

        var coordinator = services.GetService<INotificationCoordinator>();
        var logger = services.GetService<ILogger<SystemEventsReceiver>>();
        if (coordinator is null)
            return;

        logger?.LogInformation("System event {Action}; rebuilding notification plan", intent?.Action);

        var pendingResult = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                await coordinator.RescheduleAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Reschedule after system event failed");
            }
            finally
            {
                pendingResult?.Finish();
            }
        });
    }
}
