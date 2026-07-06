using Android.App;
using Android.Appwidget;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;

namespace PrayerApp;

/// <summary>
/// Home-screen widget provider. Rendering is delegated to the shared refresh path
/// (<see cref="IPersistentNotificationService"/>), which reads the cached schedule,
/// draws the widget and re-arms the alarm chain that keeps it accurate while the
/// app is closed. System-triggered periodic updates act as a safety net.
/// </summary>
[BroadcastReceiver(Exported = true, Label = "Ezan Vakti Widget")]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData("android.appwidget.provider", Resource = "@xml/prayer_appwidget_info")]
public class PrayerAppWidget : AppWidgetProvider
{
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        var services = IPlatformApplication.Current?.Services;
        var refresher = services?.GetService<IPersistentNotificationService>();
        if (refresher is null)
            return;

        var logger = services?.GetService<ILogger<PrayerAppWidget>>();
        var pendingResult = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                await refresher.RefreshAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Widget update failed");
            }
            finally
            {
                pendingResult?.Finish();
            }
        });
    }
}
