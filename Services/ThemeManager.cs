using PrayerApp.Core.Domain;

namespace PrayerApp.Services;

/// <summary>Applies the user's theme preference to the running application.</summary>
public static class ThemeManager
{
    public static void Apply(ThemePreference preference)
    {
        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = preference switch
        {
            ThemePreference.Light => AppTheme.Light,
            ThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
