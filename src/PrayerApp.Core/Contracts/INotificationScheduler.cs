using PrayerApp.Core.Notifications;

namespace PrayerApp.Core.Contracts;

/// <summary>Platform notification scheduling abstraction (implemented per platform in the app head).</summary>
public interface INotificationScheduler
{
    Task<bool> AreNotificationsPermittedAsync();

    /// <summary>Requests notification permission (and exact alarm capability where relevant).</summary>
    Task RequestPermissionAsync();

    Task ScheduleAsync(IReadOnlyList<PlannedNotification> notifications, CancellationToken cancellationToken = default);

    Task CancelAsync(IReadOnlyCollection<int> notificationIds);

    Task<int> GetPendingCountAsync();
}

/// <summary>Small persistent key-value store (Preferences-backed on device).</summary>
public interface IKeyValueStore
{
    string? GetString(string key);
    void SetString(string key, string value);
    void Remove(string key);
}
