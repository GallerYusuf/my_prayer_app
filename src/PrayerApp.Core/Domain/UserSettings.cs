namespace PrayerApp.Core.Domain;

public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2
}

public enum LocationMode
{
    Manual = 0,
    Automatic = 1
}

/// <summary>All user-configurable settings. Persisted by <see cref="Contracts.ISettingsStore"/>.</summary>
public sealed record UserSettings
{
    public const int SchemaVersion = 2;

    public int Version { get; init; } = SchemaVersion;
    public bool HasCompletedOnboarding { get; init; }

    // Location
    public LocationMode LocationMode { get; init; } = LocationMode.Manual;
    public GeoLocation? Location { get; init; }
    public int CityPlateCode { get; init; }
    public string DistrictApiName { get; init; } = string.Empty;

    // Calculation
    public CalculationMethod CalculationMethod { get; init; } = CalculationMethod.Diyanet;
    public PrayerAdjustments Adjustments { get; init; } = PrayerAdjustments.None;

    // Prayer notifications
    public bool PrayerNotificationsEnabled { get; init; } = true;
    /// <summary>Per-prayer notification toggles; missing key means enabled.</summary>
    public IReadOnlyDictionary<PrayerKind, bool> PrayerNotificationToggles { get; init; }
        = new Dictionary<PrayerKind, bool> { [PrayerKind.Sunrise] = false };
    public int MinutesBeforePrayer { get; init; }
    public bool NotificationSoundEnabled { get; init; } = true;
    public bool NotificationVibrationEnabled { get; init; } = true;

    /// <summary>Ongoing status-bar notification with today's prayer times (Android).</summary>
    public bool PersistentNotificationEnabled { get; init; } = true;

    // Verse notifications
    public bool VerseNotificationsEnabled { get; init; } = true;
    public IReadOnlyList<TimeOnly> VerseNotificationTimes { get; init; } = [new(9, 0)];

    // Friday reminder
    public bool FridayNotificationsEnabled { get; init; } = true;
    public TimeOnly FridayNotificationTime { get; init; } = new(11, 30);

    // Appearance
    public ThemePreference Theme { get; init; } = ThemePreference.System;
    public double FontScale { get; init; } = 1.0;

    public bool RamadanModeEnabled { get; init; } = true;

    public bool IsPrayerNotificationEnabled(PrayerKind kind) =>
        PrayerNotificationsEnabled &&
        (!PrayerNotificationToggles.TryGetValue(kind, out var enabled) || enabled);

    /// <summary>Clamps out-of-range values (possibly from corrupted persisted data).</summary>
    public UserSettings Sanitized() => this with
    {
        MinutesBeforePrayer = Math.Clamp(MinutesBeforePrayer, 0, 120),
        FontScale = Math.Clamp(FontScale, 0.8, 1.4),
        Adjustments = Adjustments.Sanitized(),
        VerseNotificationTimes = VerseNotificationTimes.Take(5).ToList()
    };
}
