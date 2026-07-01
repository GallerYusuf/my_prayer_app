using System.Text.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsKey = "app_settings";

    public AppSettings GetSettings()
    {
        var json = Preferences.Default.Get(SettingsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new AppSettings();

        var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        if (!HasProperty(json, nameof(AppSettings.HasCompletedOnboarding)))
            settings.HasCompletedOnboarding = !string.IsNullOrWhiteSpace(settings.City)
                && !string.IsNullOrWhiteSpace(settings.Country);

        return settings;
    }

    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        Preferences.Default.Set(SettingsKey, json);
    }

    private static bool HasProperty(string json, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(propertyName, out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
