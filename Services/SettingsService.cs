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

        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        Preferences.Default.Set(SettingsKey, json);
    }
}
