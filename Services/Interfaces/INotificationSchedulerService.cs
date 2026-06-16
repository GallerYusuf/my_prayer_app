using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public interface INotificationSchedulerService
{
    Task RequestPermissionAsync();
    Task RescheduleAllAsync(AppSettings settings, IReadOnlyList<PrayerTime> prayerTimes);
    Task RescheduleIfStaleAsync(AppSettings settings, IReadOnlyList<PrayerTime> prayerTimes);
    Task<string> SendTestNotificationAsync();
    Task<string> GetDiagnosticStatusAsync();
    Task<string> GetScheduleSummaryAsync();
    Task OpenNotificationSettingsAsync();
    Task OpenExactAlarmSettingsAsync();
    Task OpenBatterySettingsAsync();
    void StartForegroundMonitor();
    Task<int> GetPendingCountAsync();
}
