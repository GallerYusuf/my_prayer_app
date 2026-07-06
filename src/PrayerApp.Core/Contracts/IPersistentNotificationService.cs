namespace PrayerApp.Core.Contracts;

/// <summary>
/// Keeps a persistent (ongoing) status-bar notification showing today's prayer
/// times up to date, or removes it when disabled in settings.
/// Only meaningful on Android; other platforms use <see cref="NullPersistentNotificationService"/>.
/// </summary>
public interface IPersistentNotificationService
{
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>No-op implementation for platforms without ongoing notifications.</summary>
public sealed class NullPersistentNotificationService : IPersistentNotificationService
{
    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
