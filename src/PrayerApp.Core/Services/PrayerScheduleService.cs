using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Services;

/// <summary>Result of a schedule query, carrying data plus freshness metadata for the UI.</summary>
public sealed record PrayerScheduleResult(
    IReadOnlyList<PrayerDay> Days,
    bool IsFromCache,
    DateTimeOffset? LastFetchedAtUtc);

public interface IPrayerScheduleService
{
    /// <summary>
    /// Returns adjusted prayer days from <paramref name="from"/> for <paramref name="dayCount"/> days.
    /// Serves from local cache when possible; refreshes from the network when the cache
    /// is thin or stale; falls back to cache when offline.
    /// </summary>
    Task<Result<PrayerScheduleResult>> GetScheduleAsync(
        DateOnly from,
        int dayCount,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}

public sealed class PrayerScheduleService(
    IPrayerTimeProvider provider,
    IPrayerDayRepository repository,
    ISettingsStore settingsStore,
    IClock clock) : IPrayerScheduleService
{
    /// <summary>Cache is proactively refreshed when fewer than this many future days remain.</summary>
    private const int MinFutureDays = 7;

    /// <summary>How many days we fetch from the provider in one go.</summary>
    private const int FetchDays = 35;

    public async Task<Result<PrayerScheduleResult>> GetScheduleAsync(
        DateOnly from,
        int dayCount,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var settings = settingsStore.Get();
        var location = settings.Location;
        if (location is null || !location.IsValid)
            return AppError.NotFound("Konum seçilmedi. Ayarlardan şehir seçin veya konumu açın.");

        var method = settings.CalculationMethod;
        var key = location.CacheKey;
        var to = from.AddDays(dayCount - 1);

        var futureDayCount = await repository.CountFromAsync(key, method, clock.Today, cancellationToken);
        var needsRefresh = forceRefresh || futureDayCount < MinFutureDays;

        AppError? fetchError = null;
        if (needsRefresh)
        {
            var fetch = await provider.GetPrayerDaysAsync(location, method, clock.Today, FetchDays, cancellationToken);
            if (fetch.IsSuccess)
            {
                await repository.SaveAsync(key, method, fetch.Value, clock.UtcNow, cancellationToken);
                await repository.PurgeOldAsync(clock.Today.AddDays(-7), cancellationToken);
            }
            else
            {
                fetchError = fetch.Error;
            }
        }

        var cached = await repository.GetRangeAsync(key, method, from, to, cancellationToken);
        if (cached.Count == 0)
        {
            return fetchError ?? AppError.NoCache(
                "Bu konum için kayıtlı vakit verisi yok. İnternet bağlantısıyla bir kez yenileyin.");
        }

        var lastFetch = await repository.GetLastFetchTimeAsync(key, method, cancellationToken);
        var adjusted = cached
            .Select(d => d.WithAdjustments(settings.Adjustments))
            .OrderBy(d => d.Date)
            .ToList();

        return new PrayerScheduleResult(
            adjusted,
            IsFromCache: fetchError is not null || !needsRefresh,
            lastFetch);
    }
}
