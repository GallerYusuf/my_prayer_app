using PrayerApp.Core.Common;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Infrastructure.Database;
using PrayerApp.Infrastructure.Repositories;
using Xunit;

namespace PrayerApp.IntegrationTests;

public sealed class PrayerScheduleServiceTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 7, 3);

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"prayerapp_test_{Guid.NewGuid():N}.db3");
    private AppDatabase _database = null!;
    private PrayerDayRepository _repository = null!;
    private FakePrayerTimeProvider _provider = null!;
    private FakeSettingsStore _settings = null!;
    private FakeClock _clock = null!;
    private PrayerScheduleService _service = null!;

    public Task InitializeAsync()
    {
        _database = new AppDatabase(_dbPath);
        _repository = new PrayerDayRepository(_database);
        _provider = new FakePrayerTimeProvider();
        _settings = new FakeSettingsStore();
        _clock = new FakeClock(TestData(Today, "10:00"));
        _service = new PrayerScheduleService(_provider, _repository, _settings, _clock);
        return Task.CompletedTask;
    }

    private static DateTimeOffset TestData(DateOnly date, string time) =>
        new(date.ToDateTime(TimeOnly.Parse(time)), TimeSpan.FromHours(3));

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task EmptyCache_FetchesFromProviderAndCaches()
    {
        var result = await _service.GetScheduleAsync(Today, 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Days.Count);
        Assert.Equal(1, _provider.CallCount);
        Assert.False(result.Value.IsFromCache);
    }

    [Fact]
    public async Task WarmCache_DoesNotHitProvider()
    {
        await _service.GetScheduleAsync(Today, 7);
        var second = await _service.GetScheduleAsync(Today, 7);

        Assert.True(second.IsSuccess);
        Assert.Equal(1, _provider.CallCount);
        Assert.True(second.Value.IsFromCache);
    }

    [Fact]
    public async Task OfflineWithCache_FallsBackToCache()
    {
        await _service.GetScheduleAsync(Today, 7);

        _provider.NextResult = AppError.Network();
        var result = await _service.GetScheduleAsync(Today, 7, forceRefresh: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Days.Count);
        Assert.True(result.Value.IsFromCache);
    }

    [Fact]
    public async Task OfflineWithoutCache_ReturnsError()
    {
        _provider.NextResult = AppError.Network();
        var result = await _service.GetScheduleAsync(Today, 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Network, result.Error.Kind);
    }

    [Fact]
    public async Task NoLocation_ReturnsNotFound()
    {
        _settings.Current = _settings.Current with { Location = null };
        var result = await _service.GetScheduleAsync(Today, 7);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task LocationChange_TriggersFreshFetch()
    {
        await _service.GetScheduleAsync(Today, 7);
        Assert.Equal(1, _provider.CallCount);

        _settings.Current = _settings.Current with
        {
            Location = new GeoLocation { Latitude = 41.0082, Longitude = 28.9784, DisplayName = "İstanbul" }
        };

        var result = await _service.GetScheduleAsync(Today, 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _provider.CallCount);
    }

    [Fact]
    public async Task Adjustments_AreAppliedToReturnedDays()
    {
        _settings.Current = _settings.Current with
        {
            Adjustments = new PrayerAdjustments { Fajr = 5 }
        };

        var result = await _service.GetScheduleAsync(Today, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeOnly(4, 35), result.Value.Days[0][PrayerKind.Fajr]);
    }

    [Fact]
    public async Task ThinningCache_RefreshesProactively()
    {
        // Seed only 3 future days (< MinFutureDays); next query should refetch.
        await _repository.SaveAsync(
            _settings.Current.Location!.CacheKey,
            CalculationMethod.Diyanet,
            FakePrayerTimeProvider.MakeDays(Today, 3),
            _clock.UtcNow);

        var result = await _service.GetScheduleAsync(Today, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _provider.CallCount);
    }
}
