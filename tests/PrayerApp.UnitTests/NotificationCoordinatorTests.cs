using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Notifications;
using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class NotificationCoordinatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 6);

    private readonly FakeClock _clock = new(TestData.At(Today, "10:00"));
    private readonly FakeScheduleService _scheduleService = new(Today);
    private readonly FakeSettingsStore _settingsStore = new();
    private readonly FakeScheduler _scheduler = new();
    private readonly InMemoryKeyValueStore _keyValueStore = new();
    private readonly SpyPersistentNotification _persistent = new();

    private NotificationCoordinator CreateCoordinator() => new(
        _scheduleService,
        new StubQuranRepository(),
        _scheduler,
        _settingsStore,
        _keyValueStore,
        _persistent,
        _clock);

    [Fact]
    public async Task RescheduleAsync_SchedulesPlan_AndRefreshesPersistentNotification()
    {
        var coordinator = CreateCoordinator();

        var result = await coordinator.RescheduleAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
        Assert.Equal(result.Value, _scheduler.Scheduled.Count);
        Assert.Equal(1, _persistent.RefreshCount);
        Assert.Equal(Today.ToString("O"), _keyValueStore.GetString("notification_last_plan_date"));
    }

    [Fact]
    public async Task RescheduleIfStale_FreshPlan_OnlyRefreshesPersistentNotification()
    {
        var coordinator = CreateCoordinator();
        _keyValueStore.SetString("notification_last_plan_date", Today.ToString("O"));

        await coordinator.RescheduleIfStaleAsync();

        Assert.Empty(_scheduler.Scheduled);
        Assert.Equal(1, _persistent.RefreshCount);
    }

    [Fact]
    public async Task RescheduleIfStale_StalePlan_RebuildsSchedule()
    {
        var coordinator = CreateCoordinator();
        _keyValueStore.SetString("notification_last_plan_date", Today.AddDays(-1).ToString("O"));

        await coordinator.RescheduleIfStaleAsync();

        Assert.NotEmpty(_scheduler.Scheduled);
        Assert.Equal(1, _persistent.RefreshCount);
    }

    [Fact]
    public async Task RescheduleAsync_AllCategoriesDisabled_CancelsPreviousAndStillRefreshesPersistent()
    {
        _settingsStore.Current = _settingsStore.Current with
        {
            PrayerNotificationsEnabled = false,
            VerseNotificationsEnabled = false,
            FridayNotificationsEnabled = false
        };
        _keyValueStore.SetString("notification_scheduled_ids", "11,22,33");
        var coordinator = CreateCoordinator();

        var result = await coordinator.RescheduleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal([11, 22, 33], _scheduler.Cancelled);
        Assert.Empty(_scheduler.Scheduled);
        // The ongoing notification is independent of one-shot categories and must stay alive.
        Assert.Equal(1, _persistent.RefreshCount);
    }

    [Fact]
    public async Task RescheduleAsync_WhenScheduleFetchFails_KeepsExistingPlan()
    {
        _scheduleService.NextResult = AppError.Network("Sunucuya ulaşılamadı.");
        _keyValueStore.SetString("notification_scheduled_ids", "11,22");
        var coordinator = CreateCoordinator();

        var result = await coordinator.RescheduleAsync();

        Assert.False(result.IsSuccess);
        Assert.Empty(_scheduler.Cancelled);        // old plan must not be cancelled blindly
        Assert.Empty(_scheduler.Scheduled);
        Assert.Equal("11,22", _keyValueStore.GetString("notification_scheduled_ids"));
    }

    [Fact]
    public async Task RescheduleAsync_ReplacesPreviouslyScheduledIds()
    {
        _keyValueStore.SetString("notification_scheduled_ids", "11,22");
        var coordinator = CreateCoordinator();

        var result = await coordinator.RescheduleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal([11, 22], _scheduler.Cancelled);
        var storedIds = _keyValueStore.GetString("notification_scheduled_ids");
        Assert.False(string.IsNullOrWhiteSpace(storedIds));
        Assert.Equal(
            _scheduler.Scheduled.Select(n => n.Id),
            storedIds!.Split(',').Select(int.Parse));
    }

    [Fact]
    public async Task RescheduleAsync_BeforeOnboarding_DoesNothing()
    {
        _settingsStore.Current = _settingsStore.Current with { HasCompletedOnboarding = false };
        var coordinator = CreateCoordinator();

        var result = await coordinator.RescheduleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Empty(_scheduler.Scheduled);
        Assert.Equal(0, _persistent.RefreshCount);
    }

    // ---- fakes ----

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset LocalNow => now;
        public DateTimeOffset UtcNow => now.ToUniversalTime();
        public DateOnly Today => DateOnly.FromDateTime(now.Date);
    }

    private sealed class FakeSettingsStore : ISettingsStore
    {
        public UserSettings Current { get; set; } = new()
        {
            HasCompletedOnboarding = true,
            Location = new GeoLocation
            {
                Latitude = 38.5,
                Longitude = 43.4,
                DisplayName = "Van",
                IsFromDevice = false
            },
            VerseNotificationsEnabled = false,
            FridayNotificationsEnabled = false
        };

        public UserSettings Get() => Current;
        public void Save(UserSettings settings) => Current = settings;
    }

    private sealed class FakeScheduleService(DateOnly today) : IPrayerScheduleService
    {
        public Result<PrayerScheduleResult>? NextResult { get; set; }

        public Task<Result<PrayerScheduleResult>> GetScheduleAsync(
            DateOnly from, int dayCount, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (NextResult is { } fixedResult)
                return Task.FromResult(fixedResult);

            var days = Enumerable.Range(0, dayCount).Select(i => TestData.Day(today.AddDays(i))).ToList();
            return Task.FromResult(Result<PrayerScheduleResult>.Success(
                new PrayerScheduleResult(days, IsFromCache: false, DateTimeOffset.UtcNow)));
        }
    }

    private sealed class FakeScheduler : INotificationScheduler
    {
        public List<PlannedNotification> Scheduled { get; } = [];
        public List<int> Cancelled { get; } = [];

        public Task<bool> AreNotificationsPermittedAsync() => Task.FromResult(true);
        public Task RequestPermissionAsync() => Task.CompletedTask;

        public Task ScheduleAsync(IReadOnlyList<PlannedNotification> notifications, CancellationToken cancellationToken = default)
        {
            Scheduled.AddRange(notifications);
            return Task.CompletedTask;
        }

        public Task CancelAsync(IReadOnlyCollection<int> notificationIds)
        {
            Cancelled.AddRange(notificationIds);
            return Task.CompletedTask;
        }

        public Task<int> GetPendingCountAsync() => Task.FromResult(Scheduled.Count);
    }

    private sealed class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = [];

        public string? GetString(string key) => _values.TryGetValue(key, out var value) ? value : null;
        public void SetString(string key, string value) => _values[key] = value;
        public void Remove(string key) => _values.Remove(key);
    }

    private sealed class SpyPersistentNotification : IPersistentNotificationService
    {
        public int RefreshCount { get; private set; }

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            RefreshCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubQuranRepository : IQuranRepository
    {
        public Task<IReadOnlyList<SurahInfo>> GetSurahsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SurahInfo>>([]);

        public Task<IReadOnlyList<Verse>> GetVersesAsync(int surahNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Verse>>([]);

        public Task<Verse?> GetVerseAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<Verse?>(null);

        public Task<IReadOnlyList<Verse>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Verse>>([]);

        public Task<int> GetTotalVerseCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<Verse?> GetVerseByGlobalIndexAsync(int globalIndex, CancellationToken cancellationToken = default) =>
            Task.FromResult<Verse?>(null);

        public Task<bool> IsFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task ToggleFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Verse>> GetFavoritesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Verse>>([]);
    }
}
