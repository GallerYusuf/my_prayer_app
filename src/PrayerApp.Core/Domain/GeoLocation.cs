using System.Globalization;

namespace PrayerApp.Core.Domain;

/// <summary>A resolved user location (either from GPS or manual city selection).</summary>
public sealed record GeoLocation
{
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }

    /// <summary>Human-readable place name, e.g. "Van" or "İpekyolu, Van".</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>IANA timezone id if known (e.g. "Europe/Istanbul"); empty means device timezone.</summary>
    public string TimeZoneId { get; init; } = string.Empty;

    /// <summary>True when the location came from device sensors rather than manual selection.</summary>
    public bool IsFromDevice { get; init; }

    /// <summary>Stable key used for cache partitioning; rounds coordinates to ~1km precision.</summary>
    public string CacheKey =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(Latitude, 2):F2}:{Math.Round(Longitude, 2):F2}");

    public bool IsValid =>
        Latitude is >= -90 and <= 90 &&
        Longitude is >= -180 and <= 180 &&
        (Latitude != 0 || Longitude != 0);
}
