using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Infrastructure.Database;

namespace PrayerApp.Infrastructure.Repositories;

public sealed class PrayerDayRepository(AppDatabase database) : IPrayerDayRepository
{
    public async Task SaveAsync(
        string locationKey,
        CalculationMethod method,
        IReadOnlyList<PrayerDay> days,
        DateTimeOffset fetchedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var methodId = (int)method;

        await db.RunInTransactionAsync(sync =>
        {
            foreach (var day in days)
            {
                if (!day.IsComplete)
                    continue;

                sync.Execute(
                    "DELETE FROM prayer_days WHERE LocationKey = ? AND Method = ? AND DayNumber = ?",
                    locationKey, methodId, day.Date.DayNumber);

                sync.Insert(new Entities.PrayerDayRow
                {
                    LocationKey = locationKey,
                    Method = methodId,
                    DayNumber = day.Date.DayNumber,
                    FajrMinutes = ToMinutes(day[PrayerKind.Fajr]),
                    SunriseMinutes = ToMinutes(day[PrayerKind.Sunrise]),
                    DhuhrMinutes = ToMinutes(day[PrayerKind.Dhuhr]),
                    AsrMinutes = ToMinutes(day[PrayerKind.Asr]),
                    MaghribMinutes = ToMinutes(day[PrayerKind.Maghrib]),
                    IshaMinutes = ToMinutes(day[PrayerKind.Isha]),
                    HijriDateText = day.HijriDateText,
                    HijriMonth = day.HijriMonth,
                    HijriDay = day.HijriDay,
                    HijriYear = day.HijriYear
                });
            }

            sync.InsertOrReplace(new Entities.CacheMetaRow
            {
                Key = MetaKey(locationKey, methodId),
                LastFetchUnixSeconds = fetchedAtUtc.ToUnixTimeSeconds()
            });
        });
    }

    public async Task<IReadOnlyList<PrayerDay>> GetRangeAsync(
        string locationKey,
        CalculationMethod method,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var rows = await db.Table<Entities.PrayerDayRow>()
            .Where(r => r.LocationKey == locationKey
                        && r.Method == (int)method
                        && r.DayNumber >= from.DayNumber
                        && r.DayNumber <= to.DayNumber)
            .OrderBy(r => r.DayNumber)
            .ToListAsync();

        return rows.Select(ToDomain).ToList();
    }

    public async Task<DateTimeOffset?> GetLastFetchTimeAsync(
        string locationKey,
        CalculationMethod method,
        CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        var row = await db.FindAsync<Entities.CacheMetaRow>(MetaKey(locationKey, (int)method));
        return row is null ? null : DateTimeOffset.FromUnixTimeSeconds(row.LastFetchUnixSeconds);
    }

    public async Task<int> CountFromAsync(
        string locationKey,
        CalculationMethod method,
        DateOnly from,
        CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Entities.PrayerDayRow>()
            .Where(r => r.LocationKey == locationKey
                        && r.Method == (int)method
                        && r.DayNumber >= from.DayNumber)
            .CountAsync();
    }

    public async Task PurgeOldAsync(DateOnly olderThan, CancellationToken cancellationToken = default)
    {
        var db = await database.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM prayer_days WHERE DayNumber < ?", olderThan.DayNumber);
    }

    private static string MetaKey(string locationKey, int methodId) => $"prayer:{locationKey}:{methodId}";

    private static int ToMinutes(TimeOnly time) => (int)(time.ToTimeSpan().TotalMinutes);

    private static TimeOnly FromMinutes(int minutes) => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes));

    private static PrayerDay ToDomain(Entities.PrayerDayRow row) => new()
    {
        Date = DateOnly.FromDayNumber(row.DayNumber),
        Times = new Dictionary<PrayerKind, TimeOnly>
        {
            [PrayerKind.Fajr] = FromMinutes(row.FajrMinutes),
            [PrayerKind.Sunrise] = FromMinutes(row.SunriseMinutes),
            [PrayerKind.Dhuhr] = FromMinutes(row.DhuhrMinutes),
            [PrayerKind.Asr] = FromMinutes(row.AsrMinutes),
            [PrayerKind.Maghrib] = FromMinutes(row.MaghribMinutes),
            [PrayerKind.Isha] = FromMinutes(row.IshaMinutes)
        },
        HijriDateText = row.HijriDateText,
        HijriMonth = row.HijriMonth,
        HijriDay = row.HijriDay,
        HijriYear = row.HijriYear
    };
}
