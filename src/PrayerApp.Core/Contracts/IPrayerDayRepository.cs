using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Contracts;

/// <summary>Local persistent cache of prayer days keyed by location + calculation method.</summary>
public interface IPrayerDayRepository
{
    Task SaveAsync(
        string locationKey,
        CalculationMethod method,
        IReadOnlyList<PrayerDay> days,
        DateTimeOffset fetchedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrayerDay>> GetRangeAsync(
        string locationKey,
        CalculationMethod method,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>UTC timestamp of the most recent successful fetch for this location/method, if any.</summary>
    Task<DateTimeOffset?> GetLastFetchTimeAsync(
        string locationKey,
        CalculationMethod method,
        CancellationToken cancellationToken = default);

    /// <summary>Number of cached days at or after <paramref name="from"/>.</summary>
    Task<int> CountFromAsync(
        string locationKey,
        CalculationMethod method,
        DateOnly from,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes cached rows older than <paramref name="olderThan"/> across all locations.</summary>
    Task PurgeOldAsync(DateOnly olderThan, CancellationToken cancellationToken = default);
}
