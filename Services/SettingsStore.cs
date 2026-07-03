using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Data;

namespace PrayerApp.Services;

/// <summary>
/// Preferences-backed <see cref="ISettingsStore"/> with one-time migration from the
/// legacy "app_settings" JSON blob. Corrupt data never throws; it falls back to defaults.
/// </summary>
public sealed class SettingsStore(ILogger<SettingsStore> logger) : ISettingsStore
{
    private const string SettingsKey = "user_settings_v2";
    private const string LegacySettingsKey = "app_settings";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private UserSettings? _cached;

    public UserSettings Get()
    {
        if (_cached is not null)
            return _cached;

        var json = Preferences.Default.Get(SettingsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                if (settings is not null)
                    return _cached = settings.Sanitized();
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "User settings JSON was corrupt; falling back");
            }
        }

        var migrated = TryMigrateLegacySettings();
        if (migrated is not null)
        {
            Save(migrated);
            return _cached = migrated;
        }

        return _cached = new UserSettings();
    }

    public void Save(UserSettings settings)
    {
        _cached = settings.Sanitized();
        Preferences.Default.Set(SettingsKey, JsonSerializer.Serialize(_cached, JsonOptions));
    }

    /// <summary>Maps the legacy schema (city name based) onto the new coordinate-based model.</summary>
    private UserSettings? TryMigrateLegacySettings()
    {
        var legacyJson = Preferences.Default.Get(LegacySettingsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(legacyJson))
            return null;

        try
        {
            var legacy = JsonSerializer.Deserialize<LegacySettings>(legacyJson);
            if (legacy is null)
                return null;

            GeoLocation? location = null;
            if (legacy.Latitude != 0 || legacy.Longitude != 0)
            {
                location = new GeoLocation
                {
                    Latitude = legacy.Latitude,
                    Longitude = legacy.Longitude,
                    DisplayName = string.IsNullOrWhiteSpace(legacy.District)
                        ? legacy.CityDisplay
                        : $"{legacy.District}, {legacy.CityDisplay}",
                    IsFromDevice = false
                };
            }
            else if (CityCoordinates.FindByPlate(legacy.PlateCode) is { } coords)
            {
                location = new GeoLocation
                {
                    Latitude = coords.Lat,
                    Longitude = coords.Lon,
                    DisplayName = legacy.CityDisplay,
                    IsFromDevice = false
                };
            }

            logger.LogInformation("Migrated legacy settings (city: {City})", legacy.CityDisplay);

            return new UserSettings
            {
                HasCompletedOnboarding = legacy.HasCompletedOnboarding && location is not null,
                Location = location,
                CityPlateCode = legacy.PlateCode,
                DistrictApiName = legacy.DistrictApiName,
                PrayerNotificationsEnabled = legacy.PrayerNotificationsEnabled,
                MinutesBeforePrayer = legacy.NotifyAtPrayerTime ? 0 : Math.Clamp(legacy.MinutesBeforePrayer, 0, 120),
                FridayNotificationsEnabled = legacy.FridayNotificationsEnabled,
                FridayNotificationTime = TimeOnly.FromTimeSpan(legacy.FridayNotificationTime),
                VerseNotificationsEnabled = legacy.VerseNotificationsEnabled,
                VerseNotificationTimes = legacy.VerseNotificationTimes
                    .Take(Math.Clamp(legacy.VerseNotificationCount, 1, 5))
                    .Select(TimeOnly.FromTimeSpan)
                    .ToList(),
                RamadanModeEnabled = legacy.RamadanModeEnabled
            }.Sanitized();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Legacy settings could not be migrated");
            return null;
        }
    }

    private sealed class LegacySettings
    {
        public string City { get; set; } = string.Empty;
        public string CityDisplay { get; set; } = string.Empty;
        public int PlateCode { get; set; }
        public string District { get; set; } = string.Empty;
        public string DistrictApiName { get; set; } = string.Empty;
        public bool HasCompletedOnboarding { get; set; }
        public bool PrayerNotificationsEnabled { get; set; } = true;
        public bool NotifyAtPrayerTime { get; set; } = true;
        public int MinutesBeforePrayer { get; set; }
        public bool FridayNotificationsEnabled { get; set; } = true;
        public TimeSpan FridayNotificationTime { get; set; } = new(11, 30, 0);
        public bool VerseNotificationsEnabled { get; set; } = true;
        public int VerseNotificationCount { get; set; } = 3;
        public List<TimeSpan> VerseNotificationTimes { get; set; } = [];
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool RamadanModeEnabled { get; set; } = true;
    }
}

/// <summary>Preferences-backed generic key-value store.</summary>
public sealed class PreferencesKeyValueStore : IKeyValueStore
{
    public string? GetString(string key)
    {
        var value = Preferences.Default.Get(key, string.Empty);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public void SetString(string key, string value) => Preferences.Default.Set(key, value);

    public void Remove(string key) => Preferences.Default.Remove(key);
}
