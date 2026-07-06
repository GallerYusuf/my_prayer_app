using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using PrayerApp.Core.Domain;

namespace PrayerApp.Platforms.Android;

/// <summary>
/// Renders the home-screen widget directly from cached schedule data.
/// The countdown uses a native <c>Chronometer</c> in count-down mode (API 24+),
/// so it keeps ticking without the app being alive; content changes (next prayer,
/// day rollover) are pushed by the same alarm chain that drives the ongoing notification.
/// </summary>
internal static class WidgetRenderer
{
    public static bool HasWidgets(Context context) => GetWidgetIds(context).Length > 0;

    public static void Render(
        Context context, PrayerDay today, UpcomingPrayer? next, string? city, DateTimeOffset now)
    {
        var manager = AppWidgetManager.GetInstance(context);
        var ids = GetWidgetIds(context);
        if (manager is null || ids.Length == 0)
            return;

        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_prayer);

        views.SetTextViewText(Resource.Id.widget_city, city ?? "—");

        if (next is not null)
        {
            var label = next.IsTomorrow(DateOnly.FromDateTime(now.Date)) ? "Sonraki vakit (yarın)" : "Sonraki vakit";
            views.SetTextViewText(Resource.Id.widget_next_label, label);
            views.SetTextViewText(Resource.Id.widget_next_name, next.Kind.ToTurkishName());
            views.SetTextViewText(Resource.Id.widget_next_time, next.OccursAt.ToString("HH\\:mm"));
        }
        else
        {
            views.SetTextViewText(Resource.Id.widget_next_name, "—");
            views.SetTextViewText(Resource.Id.widget_next_time, "--:--");
        }

        BindCountdown(views, next, now);
        BindTodayTimes(views, today);

        var launchIntent = new Intent(context, typeof(MainActivity));
        launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        var flags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
            flags |= PendingIntentFlags.Immutable;
        var pending = PendingIntent.GetActivity(context, 0, launchIntent, flags);
        views.SetOnClickPendingIntent(Resource.Id.widget_root, pending);

        manager.UpdateAppWidget(ids, views);
    }

    /// <summary>
    /// A system-driven Chronometer keeps the remaining time honest while the app
    /// process is dead. Below API 24 (no count-down support) the chronometer is
    /// hidden and only the absolute prayer time is shown.
    /// </summary>
    private static void BindCountdown(RemoteViews views, UpcomingPrayer? next, DateTimeOffset now)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24) || next is null)
        {
            views.SetViewVisibility(Resource.Id.widget_countdown, ViewStates.Gone);
            return;
        }

        var remainingMs = (long)(next.OccursAt - now).TotalMilliseconds;
        if (remainingMs < 0)
            remainingMs = 0;

        views.SetViewVisibility(Resource.Id.widget_countdown, ViewStates.Visible);
        views.SetChronometerCountDown(Resource.Id.widget_countdown, true);
        views.SetChronometer(
            Resource.Id.widget_countdown,
            SystemClock.ElapsedRealtime() + remainingMs,
            null,
            true);
    }

    private static void BindTodayTimes(RemoteViews views, PrayerDay today)
    {
        int[] nameIds =
        [
            Resource.Id.widget_time_0_name, Resource.Id.widget_time_1_name, Resource.Id.widget_time_2_name,
            Resource.Id.widget_time_3_name, Resource.Id.widget_time_4_name, Resource.Id.widget_time_5_name
        ];
        int[] valueIds =
        [
            Resource.Id.widget_time_0_value, Resource.Id.widget_time_1_value, Resource.Id.widget_time_2_value,
            Resource.Id.widget_time_3_value, Resource.Id.widget_time_4_value, Resource.Id.widget_time_5_value
        ];

        for (var i = 0; i < PrayerKindExtensions.All.Length && i < nameIds.Length; i++)
        {
            var kind = PrayerKindExtensions.All[i];
            views.SetTextViewText(nameIds[i], kind.ToTurkishName());
            views.SetTextViewText(valueIds[i],
                today.Times.TryGetValue(kind, out var time) ? time.ToString("HH\\:mm") : "--:--");
        }
    }

    private static int[] GetWidgetIds(Context context)
    {
        var manager = AppWidgetManager.GetInstance(context);
        if (manager is null)
            return [];

        var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(PrayerAppWidget)));
        return manager.GetAppWidgetIds(component) ?? [];
    }
}
