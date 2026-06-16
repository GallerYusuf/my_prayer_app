using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class NotificationSchedulerService(IVerseService verseService) : INotificationSchedulerService
{
    private const int ImmediateTestId = 998;
    private const int ScheduledTestId = 999;
    private const int PrayerNotificationBaseId = 100;
    private const int VerseNotificationBaseId = 200;
    private const string ChannelId = "prayer_app_channel";
    private static readonly TimeSpan MinimumLeadTime = TimeSpan.FromSeconds(45);

    private DateTime _lastRescheduleDate = DateTime.MinValue;

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

#if ANDROID
        var android = LocalNotificationCenter.AndroidService;
        if (android is not null && !await android.CanScheduleExactNotifications())
            android.RequestExactAlarmsPermission();
#endif
    }

    public async Task<string> GetDiagnosticStatusAsync()
    {
        var status = await LocalNotificationCenter.Current.GetNotificationPermissionStatus();
        var pending = await GetPendingCountAsync();

        var lines = new List<string>
        {
            status.IsEnabled ? "✓ Bildirim izni: Açık" : "✗ Bildirim izni: Kapalı",
        };

#if ANDROID
        var android = LocalNotificationCenter.AndroidService;
        if (android is not null)
        {
            var exact = await android.CanScheduleExactNotifications();
            lines.Add(exact ? "✓ Tam alarm izni: Açık" : "✗ Tam alarm izni: Kapalı");
        }
#endif

        lines.Add($"Zamanlanmış bildirim: {pending} adet");

        var summary = await GetScheduleSummaryAsync();
        if (!summary.StartsWith("Hiç", StringComparison.Ordinal))
            lines.Add(summary);

        return string.Join("\n", lines);
    }

    public async Task<string> GetScheduleSummaryAsync()
    {
        var list = await LocalNotificationCenter.Current.GetPendingNotificationList();
        if (list is null || list.Count == 0)
            return "Hiç zamanlanmış bildirim yok.";

        var items = list
            .Where(n => n.Schedule?.NotifyTime is not null)
            .OrderBy(n => n.Schedule!.NotifyTime)
            .Take(5)
            .Select(n =>
            {
                var at = n.Schedule!.NotifyTime!.Value;
                return $"• {n.Title}: {at:dd.MM HH:mm:ss}";
            })
            .ToList();

        return items.Count > 0
            ? $"Sonraki:\n{string.Join("\n", items)}"
            : $"{list.Count} bildirim kuyrukta.";
    }

    public Task OpenNotificationSettingsAsync()
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionAppNotificationSettings);
        intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, context!.PackageName);
        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
#endif
        return Task.CompletedTask;
    }

    public Task OpenExactAlarmSettingsAsync()
    {
#if ANDROID
        LocalNotificationCenter.AndroidService?.RequestExactAlarmsPermission();
#endif
        return Task.CompletedTask;
    }

    public async Task RescheduleIfStaleAsync(AppSettings settings, IReadOnlyList<PrayerTime> prayerTimes)
    {
        var today = DateTime.Now.Date;
        if (_lastRescheduleDate == today)
            return;

        await RescheduleAllAsync(settings, prayerTimes);
    }

    public async Task RescheduleAllAsync(AppSettings settings, IReadOnlyList<PrayerTime> prayerTimes)
    {
        await RequestPermissionAsync();

        for (var i = 0; i < 6; i++)
            LocalNotificationCenter.Current.Cancel(PrayerNotificationBaseId + i);

        for (var i = 0; i < 5; i++)
            LocalNotificationCenter.Current.Cancel(VerseNotificationBaseId + i);

        if (settings.PrayerNotificationsEnabled)
            await SchedulePrayerNotificationsAsync(settings, prayerTimes);

        if (settings.VerseNotificationsEnabled)
            await ScheduleVerseNotificationsAsync(settings);

        _lastRescheduleDate = DateTime.Now.Date;
    }

    public async Task<string> SendTestNotificationAsync()
    {
        await RequestPermissionAsync();

        var status = await LocalNotificationCenter.Current.GetNotificationPermissionStatus();
        if (!status.IsEnabled)
            return "Bildirim izni kapalı. 'Bildirim Ayarlarını Aç' butonuna basıp izin verin.";

        LocalNotificationCenter.Current.Cancel(ImmediateTestId);
        LocalNotificationCenter.Current.Cancel(ScheduledTestId);

        await LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = ImmediateTestId,
            Title = "Anlık Test Bildirimi",
            Description = "Bu bildirimi görüyorsanız temel bildirim çalışıyor.",
            Android = CreateAndroidOptions()
        });

        var notifyAt = DateTimeOffset.Now.AddSeconds(10);
        await ScheduleNotificationAsync(new NotificationRequest
        {
            NotificationId = ScheduledTestId,
            Title = "Zamanlanmış Test",
            Description = "10 saniyelik zamanlama testi başarılı!",
            Android = CreateAndroidOptions(),
            Schedule = CreateSchedule(notifyAt)
        });

        var pending = await GetPendingCountAsync();
        return $"Anlık bildirim gönderildi. 10 sn sonra ikinci bildirim gelecek. Bekleyen: {pending}";
    }

    public async Task<int> GetPendingCountAsync()
    {
        var list = await LocalNotificationCenter.Current.GetPendingNotificationList();
        return list?.Count ?? 0;
    }

    private async Task SchedulePrayerNotificationsAsync(AppSettings settings, IReadOnlyList<PrayerTime> prayerTimes)
    {
        for (var i = 0; i < prayerTimes.Count; i++)
        {
            var prayer = prayerTimes[i];
            var notifyTime = settings.NotifyAtPrayerTime
                ? prayer.Time
                : prayer.Time - TimeSpan.FromMinutes(settings.MinutesBeforePrayer);

            if (notifyTime < TimeSpan.Zero)
                notifyTime = TimeSpan.Zero;

            var scheduleDate = ResolveNextOccurrence(notifyTime, DateTimeOffset.Now);

            var title = settings.NotifyAtPrayerTime
                ? $"{prayer.TurkishName} Vakti"
                : $"{prayer.TurkishName} Vaktine {settings.MinutesBeforePrayer} dk";

            await ScheduleNotificationAsync(new NotificationRequest
            {
                NotificationId = PrayerNotificationBaseId + i,
                Title = title,
                Description = $"{prayer.TurkishName} vakti: {prayer.Time:hh\\:mm}",
                Android = CreateAndroidOptions(),
                Schedule = CreateSchedule(scheduleDate)
            });
        }
    }

    private async Task ScheduleVerseNotificationsAsync(AppSettings settings)
    {
        await verseService.InitializeAsync();

        var times = settings.VerseNotificationTimes
            .Take(settings.VerseNotificationCount)
            .ToList();

        for (var i = 0; i < times.Count; i++)
        {
            var scheduleDate = ResolveNextOccurrence(times[i], DateTimeOffset.Now);
            var verse = verseService.GetRandomVerse();

            await ScheduleNotificationAsync(new NotificationRequest
            {
                NotificationId = VerseNotificationBaseId + i,
                Title = "Günün Ayeti",
                Description = Truncate($"{verse.SurahName}, {verse.AyahNumber}. ayet — {verse.Text}", 180),
                Android = CreateAndroidOptions(),
                Schedule = CreateSchedule(scheduleDate)
            });
        }
    }

    private static async Task ScheduleNotificationAsync(NotificationRequest request)
    {
        if (request.Schedule?.NotifyTime is DateTimeOffset notifyAt)
            request.Schedule.NotifyTime = NormalizeNotifyTime(notifyAt);

        await LocalNotificationCenter.Current.Show(request);
    }

    private static AndroidOptions CreateAndroidOptions() => new()
    {
        ChannelId = ChannelId,
        IconSmallName = new AndroidIcon("notification_icon"),
        Color = new AndroidColor(unchecked((int)0xFF0E6B5C))
    };

    private static DateTimeOffset ResolveNextOccurrence(TimeSpan timeOfDay, DateTimeOffset now)
    {
        var candidate = new DateTimeOffset(now.Date + timeOfDay, now.Offset);

        if (candidate <= now)
            candidate = candidate.AddDays(1);

        return candidate;
    }

    private static DateTimeOffset NormalizeNotifyTime(DateTimeOffset notifyAt)
    {
        var minimum = DateTimeOffset.Now.Add(MinimumLeadTime);
        if (notifyAt >= minimum)
            return notifyAt;

        // Bugün için seçilmiş saat geçmiş veya çok yakınsa kısa gecikmeyle planla
        if (notifyAt.Date <= DateTimeOffset.Now.Date)
            return minimum;

        return notifyAt;
    }

    private static NotificationRequestSchedule CreateSchedule(DateTimeOffset notifyAt) => new()
    {
        NotifyTime = notifyAt,
        RepeatType = NotificationRepeat.No,
        Android = new AndroidScheduleOptions
        {
            ScheduleMode = AndroidScheduleMode.ExactAllowWhileIdle,
            AlarmType = AndroidAlarmType.RtcWakeup,
            AllowedDelay = TimeSpan.Zero
        }
    };

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }
}
