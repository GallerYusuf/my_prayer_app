using System.Resources;

namespace PrayerApp.Resources.Localization;

/// <summary>
/// Localization accessor over AppStrings.resx. Default culture is Turkish;
/// add AppStrings.{culture}.resx files to support more languages.
/// New user-facing strings should be added here rather than hardcoded.
/// </summary>
public static class AppStrings
{
    private static readonly ResourceManager ResourceManager =
        new("PrayerApp.Resources.Localization.AppStrings", typeof(AppStrings).Assembly);

    public static string Get(string key) => ResourceManager.GetString(key) ?? key;

    public static string AppName => Get(nameof(AppName));
    public static string VerseOfDay => Get(nameof(VerseOfDay));
    public static string Offline => Get(nameof(Offline));
    public static string Share => Get(nameof(Share));
    public static string Save => Get(nameof(Save));
    public static string Retry => Get(nameof(Retry));
}
