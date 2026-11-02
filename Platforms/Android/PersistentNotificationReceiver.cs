using Android.App;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Services;

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

    /// <summary>goAsync() only grants ~10 s; stay below it so we finish cleanly.</summary>
    private static readonly TimeSpan RefreshBudget = TimeSpan.FromSeconds(8);

    /// <summary>Retry delay if this refresh is killed before it re-arms the real alarm.</summary>
    private static readonly TimeSpan FallbackDelay = TimeSpan.FromMinutes(15);

    public override void OnReceive(Context? context, Intent? intent)
    {
        var services = IPlatformApplication.Current?.Services;
        var service = services?.GetService<IPersistentNotificationService>();
        var coordinator = services?.GetService<INotificationCoordinator>();
        if (service is null && coordinator is null)
            return;

        var logger = services?.GetService<ILogger<PersistentNotificationReceiver>>();
        // The refresh below may fetch from the network (30 s HTTP timeout), which can outlive
        // the receiver's window; arm a safety alarm first so the chain never breaks.
        // Before onboarding nothing would cancel it, so only arm it once the chain is wanted.
        if (services?.GetService<ISettingsStore>()?.Get().HasCompletedOnboarding == true)
            PersistentNotificationService.ScheduleFallback(FallbackDelay);

        var pendingResult = GoAsync();
        _ = Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(RefreshBudget);
            try
            {
                // The coordinator rebuilds the rolling notification plan once per day when
                // it is stale and always refreshes the persistent surfaces (which re-arms
                // this alarm), so reminders keep flowing even if the app is never opened.
                if (coordinator is not null)
                    await coordinator.RescheduleIfStaleAsync(cts.Token);
                else
                    await service!.RefreshAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger?.LogWarning("Persistent notification refresh timed out; fallback alarm will retry");
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
