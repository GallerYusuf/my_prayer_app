using Android.App;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;

namespace PrayerApp.Platforms.Android;

/// <summary>
/// Refreshes the persistent prayer-times notification when its self-scheduled
/// alarm fires (each prayer time / midnight), after device reboot and after an
/// app update, since AlarmManager alarms do not survive either.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter([
    ActionRefresh,
    Intent.ActionBootCompleted,
    Intent.ActionMyPackageReplaced
])]
public sealed class PersistentNotificationReceiver : BroadcastReceiver
{
    public const string ActionRefresh = "prayerapp.action.REFRESH_PERSISTENT_NOTIFICATION";

    public override void OnReceive(Context? context, Intent? intent)
    {
        var services = IPlatformApplication.Current?.Services;
        var service = services?.GetService<IPersistentNotificationService>();
        if (service is null)
            return;

        var logger = services?.GetService<ILogger<PersistentNotificationReceiver>>();
        var pendingResult = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                await service.RefreshAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Persistent notification receiver failed");
            }
            finally
            {
                pendingResult?.Finish();
            }
        });
    }
}
