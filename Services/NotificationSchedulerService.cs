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
    private const int FridayNotificationId = 300;
    private const string ChannelId = "prayer_app_channel";

    private DateTime _lastRescheduleDate = DateTime.MinValue;
    private IDispatcherTimer? _foregroundMonitor;

    public void StartForegroundMonitor()
    {
        if (_foregroundMonitor is not null)
            return;

        _foregroundMonitor = Application.Current?.Dispatcher.CreateTimer();
        if (_foregroundMonitor is null)
            return;

        _foregroundMonitor.Interval = TimeSpan.FromSeconds(10);
        _foregroundMonitor.Tick += async (_, _) => await DeliverOverdueNotificationsAsync();
        _foregroundMonitor.Start();
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

        lines.Add(IsBatteryOptimizationDisabled()
            ? "✓ Pil optimizasyonu: Kapalı"
            : "✗ Pil optimizasyonu: Açık (bildirimleri engelleyebilir)");
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

        var now = DateTimeOffset.Now;
        var items = list
            .Where(n => n.Schedule?.NotifyTime is not null)
            .OrderBy(n => n.Schedule!.NotifyTime)
            .Take(5)
            .Select(n =>
            {
                var at = n.Schedule!.NotifyTime!.Value;
                var marker = at < now ? " (geçti!)" : string.Empty;
                return $"• {n.Title}: {at:dd.MM HH:mm:ss}{marker}";
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

    public Task OpenBatterySettingsAsync()
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        var packageName = context!.PackageName!;
        var powerManager = (Android.OS.PowerManager?)context.GetSystemService(Android.Content.Context.PowerService);

        if (powerManager is not null && !powerManager.IsIgnoringBatteryOptimizations(packageName))
        {
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse($"package:{packageName}"));
            context.StartActivity(intent);
        }
        else
        {
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
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

        LocalNotificationCenter.Current.Cancel(FridayNotificationId);

        if (settings.PrayerNotificationsEnabled)
            await SchedulePrayerNotificationsAsync(settings, prayerTimes);

        if (settings.FridayNotificationsEnabled)
            await ScheduleFridayNotificationAsync(settings);

        if (settings.VerseNotificationsEnabled)
            await ScheduleVerseNotificationsAsync(settings);

        _lastRescheduleDate = DateTime.Now.Date;
        StartForegroundMonitor();
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

        var notifyAt = DateTimeOffset.Now.AddSeconds(15);
        await ScheduleNotificationAsync(new NotificationRequest
        {
            NotificationId = ScheduledTestId,
            Title = "Zamanlanmış Test",
            Description = "15 saniyelik zamanlama testi başarılı!",
            Android = CreateAndroidOptions(),
            Schedule = CreateSchedule(notifyAt)
        });

        StartForegroundMonitor();

        var pending = await GetPendingCountAsync();
        return $"Anlık bildirim gönderildi. 15 sn sonra ikinci bildirim gelecek. Bekleyen: {pending}";
    }

    public async Task<int> GetPendingCountAsync()
    {
        var list = await LocalNotificationCenter.Current.GetPendingNotificationList();
        return list?.Count ?? 0;
    }

    private async Task DeliverOverdueNotificationsAsync()
    {
        var list = await LocalNotificationCenter.Current.GetPendingNotificationList();
        if (list is null || list.Count == 0)
            return;

        var now = DateTimeOffset.Now;

        foreach (var pending in list)
        {
            if (pending.Schedule?.NotifyTime is not DateTimeOffset notifyAt)
                continue;

            // Alarm receiver'a 5 sn süre ver, sonra yedek olarak göster
            if (notifyAt > now.AddSeconds(-5))
                continue;

            LocalNotificationCenter.Current.Cancel(pending.NotificationId);

            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = pending.NotificationId,
                Title = pending.Title ?? "Bildirim",
                Description = pending.Description ?? string.Empty,
                Android = pending.Android ?? CreateAndroidOptions()
            });
        }
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

    private async Task ScheduleFridayNotificationAsync(AppSettings settings)
    {
        var scheduleDate = ResolveNextWeekdayOccurrence(
            DayOfWeek.Friday,
            settings.FridayNotificationTime,
            DateTimeOffset.Now);

        await ScheduleNotificationAsync(new NotificationRequest
        {
            NotificationId = FridayNotificationId,
            Title = "Cuma Hatırlatıcısı",
            Description = "Cuma namazı için hazırlık vakti yaklaşıyor.",
            Android = CreateAndroidOptions(),
            Schedule = CreateSchedule(scheduleDate)
        });
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

    private static DateTimeOffset ResolveNextWeekdayOccurrence(DayOfWeek dayOfWeek, TimeSpan timeOfDay, DateTimeOffset now)
    {
        var daysUntilTarget = ((int)dayOfWeek - (int)now.DayOfWeek + 7) % 7;
        var candidate = new DateTimeOffset(now.Date + timeOfDay, now.Offset).AddDays(daysUntilTarget);

        if (candidate <= now)
            candidate = candidate.AddDays(7);

        return candidate;
    }

    private static DateTimeOffset NormalizeNotifyTime(DateTimeOffset notifyAt)
    {
        var now = DateTimeOffset.Now;
        return notifyAt > now ? notifyAt : now.AddSeconds(30);
    }

    private static NotificationRequestSchedule CreateSchedule(DateTimeOffset notifyAt) => new()
    {
        NotifyTime = notifyAt,
        RepeatType = NotificationRepeat.No,
        Android = new AndroidScheduleOptions
        {
            ScheduleMode = AndroidScheduleMode.AlarmClock,
            AlarmType = AndroidAlarmType.RtcWakeup,
            AllowedDelay = TimeSpan.FromMinutes(1)
        }
    };

#if ANDROID
    private static bool IsBatteryOptimizationDisabled()
    {
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        var powerManager = (Android.OS.PowerManager?)context!.GetSystemService(Android.Content.Context.PowerService);
        return powerManager?.IsIgnoringBatteryOptimizations(context.PackageName) == true;
    }
#else
    private static bool IsBatteryOptimizationDisabled() => true;
#endif

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }
}
