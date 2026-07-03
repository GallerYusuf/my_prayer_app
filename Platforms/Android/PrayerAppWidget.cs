using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace PrayerApp;

[BroadcastReceiver(Exported = true, Label = "Ezan Vakti Widget")]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/prayer_appwidget_info")]
public class PrayerAppWidget : AppWidgetProvider
{
    private const string PrefsName = "widget_data";

    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var prefs = context.GetSharedPreferences(PrefsName, global::Android.Content.FileCreationMode.Private);
        var city = prefs.GetString("city", "—") ?? "—";
        var nextName = prefs.GetString("next_name", "—") ?? "—";
        var nextTime = prefs.GetString("next_time", "--:--") ?? "--:--";
        var remaining = prefs.GetString("remaining", "--:--:--") ?? "--:--:--";

        foreach (var widgetId in appWidgetIds)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.widget_prayer);
            views.SetTextViewText(Resource.Id.widget_city, city);
            views.SetTextViewText(Resource.Id.widget_next_name, nextName);
            views.SetTextViewText(Resource.Id.widget_next_time, nextTime);
            views.SetTextViewText(Resource.Id.widget_countdown, remaining);

            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            var pending = PendingIntent.GetActivity(
                context,
                0,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.widget_root, pending);

            appWidgetManager.UpdateAppWidget(widgetId, views);
        }
    }
}
