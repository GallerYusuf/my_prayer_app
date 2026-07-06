using PrayerApp.Core.Contracts;
using PrayerApp.Infrastructure.Database;

namespace PrayerApp.Infrastructure.Repositories;

/// <summary>SQLite-backed khatm plan storage with a per-day reading log.</summary>
public sealed class KhatmRepository(AppDatabase database) : IKhatmRepository
{
    public async Task<KhatmPlan?> GetActivePlanAsync(CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var row = await db.Table<Entities.KhatmPlanRow>()
            .Where(p => p.CompletedDayNumber == 0)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        return row is null ? null : ToPlan(row);
    }

    public async Task<KhatmPlan> StartPlanAsync(int targetDays, DateOnly today, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();

        // Only one active plan at a time; drop any unfinished one (user-confirmed in UI).
        var active = await db.Table<Entities.KhatmPlanRow>()
            .Where(p => p.CompletedDayNumber == 0)
            .ToListAsync();
        foreach (var stale in active)
        {
            await db.ExecuteAsync("DELETE FROM khatm_log WHERE PlanId = ?", stale.Id);
            await db.DeleteAsync(stale);
        }

        var row = new Entities.KhatmPlanRow
        {
            StartDayNumber = today.DayNumber,
            TargetDays = Math.Clamp(targetDays, 0, 365),
            LastReadPage = 0,
            CompletedDayNumber = 0
        };
        await db.InsertAsync(row);
        return ToPlan(row);
    }

    public async Task<KhatmPlan> SetLastReadPageAsync(int planId, int page, DateOnly today, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var row = await db.FindAsync<Entities.KhatmPlanRow>(planId)
            ?? throw new InvalidOperationException($"Khatm plan {planId} not found.");

        row.LastReadPage = Math.Clamp(page, 0, KhatmPlan.TotalPages);
        if (row.LastReadPage >= KhatmPlan.TotalPages && row.CompletedDayNumber == 0)
            row.CompletedDayNumber = today.DayNumber;

        await db.UpdateAsync(row);

        var log = await db.Table<Entities.KhatmLogRow>()
            .Where(l => l.PlanId == planId && l.DayNumber == today.DayNumber)
            .FirstOrDefaultAsync();

        if (log is null)
        {
            await db.InsertAsync(new Entities.KhatmLogRow
            {
                PlanId = planId,
                DayNumber = today.DayNumber,
                EndPage = row.LastReadPage
            });
        }
        else
        {
            log.EndPage = row.LastReadPage;
            await db.UpdateAsync(log);
        }

        return ToPlan(row);
    }

    public async Task<int> GetPagesReadOnAsync(int planId, DateOnly day, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var todayLog = await db.Table<Entities.KhatmLogRow>()
            .Where(l => l.PlanId == planId && l.DayNumber == day.DayNumber)
            .FirstOrDefaultAsync();

        if (todayLog is null)
            return 0;

        var previous = await db.Table<Entities.KhatmLogRow>()
            .Where(l => l.PlanId == planId && l.DayNumber < day.DayNumber)
            .OrderByDescending(l => l.DayNumber)
            .FirstOrDefaultAsync();

        return Math.Max(0, todayLog.EndPage - (previous?.EndPage ?? 0));
    }

    public async Task<IReadOnlyList<KhatmPlan>> GetCompletedPlansAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var rows = await db.Table<Entities.KhatmPlanRow>()
            .Where(p => p.CompletedDayNumber > 0)
            .OrderByDescending(p => p.CompletedDayNumber)
            .Take(limit)
            .ToListAsync();

        return rows.Select(ToPlan).ToList();
    }

    public async Task DeletePlanAsync(int planId, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM khatm_log WHERE PlanId = ?", planId);
        await db.ExecuteAsync("DELETE FROM khatm_plans WHERE Id = ?", planId);
    }

    private static KhatmPlan ToPlan(Entities.KhatmPlanRow row) => new(
        row.Id,
        DateOnly.FromDayNumber(row.StartDayNumber),
        row.TargetDays,
        row.LastReadPage,
        row.CompletedDayNumber > 0 ? DateOnly.FromDayNumber(row.CompletedDayNumber) : null);
}
