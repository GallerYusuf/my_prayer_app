using PrayerApp.Core.Contracts;
using PrayerApp.Infrastructure.Database;
using PrayerApp.Infrastructure.Repositories;
using Xunit;

namespace PrayerApp.IntegrationTests;

public sealed class KhatmRepositoryTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 7, 6);

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"prayerapp_khatm_{Guid.NewGuid():N}.db3");
    private AppDatabase _database = null!;
    private KhatmRepository _repository = null!;

    public Task InitializeAsync()
    {
        _database = new AppDatabase(_dbPath);
        _repository = new KhatmRepository(_database);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task NoPlan_ReturnsNull()
    {
        Assert.Null(await _repository.GetActivePlanAsync());
    }

    [Fact]
    public async Task StartPlan_CreatesActivePlan()
    {
        var plan = await _repository.StartPlanAsync(30, Today);

        Assert.Equal(Today, plan.StartDate);
        Assert.Equal(30, plan.TargetDays);
        Assert.Equal(0, plan.LastReadPage);
        Assert.False(plan.IsCompleted);

        var active = await _repository.GetActivePlanAsync();
        Assert.NotNull(active);
        Assert.Equal(plan.Id, active.Id);
    }

    [Fact]
    public async Task StartPlan_ReplacesPreviousUnfinishedPlan()
    {
        var first = await _repository.StartPlanAsync(30, Today);
        await _repository.SetLastReadPageAsync(first.Id, 100, Today);

        var second = await _repository.StartPlanAsync(60, Today);

        var active = await _repository.GetActivePlanAsync();
        Assert.NotNull(active);
        Assert.Equal(second.Id, active.Id);
        Assert.Equal(0, active.LastReadPage);
    }

    [Fact]
    public async Task SetLastReadPage_TracksDailyProgress()
    {
        var plan = await _repository.StartPlanAsync(30, Today);

        await _repository.SetLastReadPageAsync(plan.Id, 12, Today);
        Assert.Equal(12, await _repository.GetPagesReadOnAsync(plan.Id, Today));

        // Next day the daily count starts from the previous end page.
        var tomorrow = Today.AddDays(1);
        await _repository.SetLastReadPageAsync(plan.Id, 30, tomorrow);
        Assert.Equal(18, await _repository.GetPagesReadOnAsync(plan.Id, tomorrow));
        Assert.Equal(12, await _repository.GetPagesReadOnAsync(plan.Id, Today));
    }

    [Fact]
    public async Task SetLastReadPage_ClampsAndCompletesAtLastPage()
    {
        var plan = await _repository.StartPlanAsync(0, Today);

        var updated = await _repository.SetLastReadPageAsync(plan.Id, 9999, Today);

        Assert.Equal(KhatmPlan.TotalPages, updated.LastReadPage);
        Assert.True(updated.IsCompleted);
        Assert.Equal(Today, updated.CompletedDate);
        Assert.Null(await _repository.GetActivePlanAsync());

        var completed = await _repository.GetCompletedPlansAsync();
        Assert.Single(completed);
    }

    [Fact]
    public async Task PagesPerDayTarget_ComputesRemainingPace()
    {
        var plan = await _repository.StartPlanAsync(30, Today);
        var updated = await _repository.SetLastReadPageAsync(plan.Id, 4, Today);

        // 600 pages left over 30 remaining days => 20/day.
        Assert.Equal(20, updated.PagesPerDayTarget(Today));

        // Past the deadline everything remaining is due.
        Assert.Equal(600, updated.PagesPerDayTarget(Today.AddDays(31)));

        // Free-form plan has no daily target.
        var free = await _repository.StartPlanAsync(0, Today);
        Assert.Equal(0, free.PagesPerDayTarget(Today));
    }

    [Fact]
    public async Task DeletePlan_RemovesPlanAndLog()
    {
        var plan = await _repository.StartPlanAsync(30, Today);
        await _repository.SetLastReadPageAsync(plan.Id, 10, Today);

        await _repository.DeletePlanAsync(plan.Id);

        Assert.Null(await _repository.GetActivePlanAsync());
        Assert.Equal(0, await _repository.GetPagesReadOnAsync(plan.Id, Today));
    }
}
