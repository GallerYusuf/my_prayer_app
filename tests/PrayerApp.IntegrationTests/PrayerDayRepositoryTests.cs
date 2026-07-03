using PrayerApp.Core.Domain;
using PrayerApp.Infrastructure.Database;
using PrayerApp.Infrastructure.Repositories;
using Xunit;

namespace PrayerApp.IntegrationTests;

public sealed class PrayerDayRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"prayerapp_test_{Guid.NewGuid():N}.db3");
    private AppDatabase _database = null!;
    private PrayerDayRepository _repository = null!;

    public Task InitializeAsync()
    {
        _database = new AppDatabase(_dbPath);
        _repository = new PrayerDayRepository(_database);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task SaveAndGetRange_RoundTrips()
    {
        var from = new DateOnly(2026, 7, 1);
        var days = FakePrayerTimeProvider.MakeDays(from, 10);
        var fetchedAt = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

        await _repository.SaveAsync("38.50:43.37", CalculationMethod.Diyanet, days, fetchedAt);
        var range = await _repository.GetRangeAsync("38.50:43.37", CalculationMethod.Diyanet, from, from.AddDays(4));

        Assert.Equal(5, range.Count);
        Assert.Equal(from, range[0].Date);
        Assert.Equal(new TimeOnly(4, 30), range[0][PrayerKind.Fajr]);
        Assert.Equal(new TimeOnly(21, 15), range[0][PrayerKind.Isha]);
        Assert.Equal(fetchedAt, await _repository.GetLastFetchTimeAsync("38.50:43.37", CalculationMethod.Diyanet));
    }

    [Fact]
    public async Task SaveSameDayTwice_DoesNotDuplicate()
    {
        var from = new DateOnly(2026, 7, 1);
        var days = FakePrayerTimeProvider.MakeDays(from, 3);

        await _repository.SaveAsync("k", CalculationMethod.Diyanet, days, DateTimeOffset.UtcNow);
        await _repository.SaveAsync("k", CalculationMethod.Diyanet, days, DateTimeOffset.UtcNow);

        var range = await _repository.GetRangeAsync("k", CalculationMethod.Diyanet, from, from.AddDays(2));
        Assert.Equal(3, range.Count);
    }

    [Fact]
    public async Task DifferentMethodOrLocation_IsIsolated()
    {
        var from = new DateOnly(2026, 7, 1);
        await _repository.SaveAsync("locA", CalculationMethod.Diyanet,
            FakePrayerTimeProvider.MakeDays(from, 2), DateTimeOffset.UtcNow);

        var otherLocation = await _repository.GetRangeAsync("locB", CalculationMethod.Diyanet, from, from.AddDays(1));
        var otherMethod = await _repository.GetRangeAsync("locA", CalculationMethod.MuslimWorldLeague, from, from.AddDays(1));

        Assert.Empty(otherLocation);
        Assert.Empty(otherMethod);
    }

    [Fact]
    public async Task PurgeOld_RemovesOnlyOlderRows()
    {
        var from = new DateOnly(2026, 7, 1);
        await _repository.SaveAsync("k", CalculationMethod.Diyanet,
            FakePrayerTimeProvider.MakeDays(from, 10), DateTimeOffset.UtcNow);

        await _repository.PurgeOldAsync(from.AddDays(5));

        var all = await _repository.GetRangeAsync("k", CalculationMethod.Diyanet, from, from.AddDays(9));
        Assert.Equal(5, all.Count);
        Assert.Equal(from.AddDays(5), all[0].Date);
    }

    [Fact]
    public async Task CountFrom_CountsFutureDays()
    {
        var from = new DateOnly(2026, 7, 1);
        await _repository.SaveAsync("k", CalculationMethod.Diyanet,
            FakePrayerTimeProvider.MakeDays(from, 10), DateTimeOffset.UtcNow);

        Assert.Equal(5, await _repository.CountFromAsync("k", CalculationMethod.Diyanet, from.AddDays(5)));
        Assert.Equal(0, await _repository.CountFromAsync("k", CalculationMethod.Diyanet, from.AddDays(100)));
    }
}
