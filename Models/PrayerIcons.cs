using PrayerApp.Core.Domain;

namespace PrayerApp.Models;

/// <summary>Maps each prayer to its vector icon resource used across the UI.</summary>
public static class PrayerIcons
{
    public static string For(PrayerKind kind) => kind switch
    {
        PrayerKind.Fajr => "icon_fajr.png",
        PrayerKind.Sunrise => "icon_sunrise.png",
        PrayerKind.Dhuhr => "icon_dhuhr.png",
        PrayerKind.Asr => "icon_asr.png",
        PrayerKind.Maghrib => "icon_maghrib.png",
        PrayerKind.Isha => "icon_isha.png",
        _ => "icon_dhuhr.png"
    };
}
