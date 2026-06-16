using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public interface ISettingsService
{
    AppSettings GetSettings();
    void SaveSettings(AppSettings settings);
}
