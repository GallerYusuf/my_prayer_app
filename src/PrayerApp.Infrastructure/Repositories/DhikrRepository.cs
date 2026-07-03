using PrayerApp.Core.Contracts;
using PrayerApp.Infrastructure.Database;

namespace PrayerApp.Infrastructure.Repositories;

public sealed class DhikrRepository(AppDatabase database) : IDhikrRepository
{
    public async Task<DhikrSession?> GetTodaySessionAsync(
        string dhikrName, DateOnly today, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var row = await db.Table<Entities.DhikrSessionRow>()
            .Where(s => s.DayNumber == today.DayNumber && s.DhikrName == dhikrName)
            .FirstOrDefaultAsync();

        return row is null ? null : ToDomain(row);
    }

    public async Task<DhikrSession> UpsertAsync(DhikrSession session, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var row = new Entities.DhikrSessionRow
        {
            Id = session.Id,
            DayNumber = session.Date.DayNumber,
            DhikrName = session.DhikrName,
            Count = session.Count,
            TargetCount = session.TargetCount
        };

        if (row.Id == 0)
        {
            await db.InsertAsync(row);
            return session with { Id = row.Id };
        }

        await db.UpdateAsync(row);
        return session;
    }

    public async Task<IReadOnlyList<DhikrSession>> GetHistoryAsync(int limit = 30, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var rows = await db.Table<Entities.DhikrSessionRow>()
            .OrderByDescending(s => s.DayNumber)
            .Take(limit)
            .ToListAsync();

        return rows.Select(ToDomain).ToList();
    }

    private static DhikrSession ToDomain(Entities.DhikrSessionRow row) =>
        new(row.Id, row.DhikrName, row.Count, row.TargetCount, DateOnly.FromDayNumber(row.DayNumber));
}
