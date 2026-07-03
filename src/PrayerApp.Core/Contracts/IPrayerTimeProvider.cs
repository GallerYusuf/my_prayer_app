using PrayerApp.Core.Common;
using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Contracts;

/// <summary>A source of prayer time data (remote API, local calculation, etc.).</summary>
public interface IPrayerTimeProvider
{
    /// <summary>
    /// Fetches prayer days covering <paramref name="from"/> for <paramref name="dayCount"/> days
    /// at the given location using the given calculation method.
    /// </summary>
    Task<Result<IReadOnlyList<PrayerDay>>> GetPrayerDaysAsync(
        GeoLocation location,
        CalculationMethod method,
        DateOnly from,
        int dayCount,
        CancellationToken cancellationToken = default);
}
