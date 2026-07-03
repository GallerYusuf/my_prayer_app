using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class WidgetDataService : IWidgetDataService
{
    private const string PrefsName = "widget_data";
    private const string KeyCity = "city";
    private const string KeyNextName = "next_name";
    private const string KeyNextTime = "next_time";
    private const string KeyRemaining = "remaining";

    public void Update(string cityDisplay, string nextPrayerName, string nextPrayerTime, string remainingTime)
    {
#if ANDROID
        var context = Platform.AppContext;
        var prefs = context.GetSharedPreferences(PrefsName, global::Android.Content.FileCreationMode.Private);
        prefs.Edit()!
            .PutString(KeyCity, cityDisplay)
            .PutString(KeyNextName, nextPrayerName)
            .PutString(KeyNextTime, nextPrayerTime)
            .PutString(KeyRemaining, remainingTime)
            .Apply();

        WidgetUpdateHelper.RequestUpdate(context);
#else
        _ = cityDisplay;
        _ = nextPrayerName;
        _ = nextPrayerTime;
        _ = remainingTime;
#endif
    }
}

#if ANDROID
internal static class WidgetUpdateHelper
{
    public static void RequestUpdate(global::Android.Content.Context context)
    {
        var manager = global::Android.Appwidget.AppWidgetManager.GetInstance(context);
        if (manager is null)
            return;

        var component = new global::Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(PrayerAppWidget)));
        var ids = manager.GetAppWidgetIds(component);
        if (ids is null || ids.Length == 0)
            return;

        var intent = new global::Android.Content.Intent(context, typeof(PrayerAppWidget));
        intent.SetAction(global::Android.Appwidget.AppWidgetManager.ActionAppwidgetUpdate);
        intent.PutExtra(global::Android.Appwidget.AppWidgetManager.ExtraAppwidgetIds, ids);
        context.SendBroadcast(intent);
    }
}
#endif
