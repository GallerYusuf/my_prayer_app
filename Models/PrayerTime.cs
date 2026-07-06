using CommunityToolkit.Mvvm.ComponentModel;

namespace PrayerApp.Models;

public partial class PrayerTime : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string TurkishName { get; init; } = string.Empty;

    /// <summary>Vector icon resource (e.g. "icon_fajr.png") for this prayer.</summary>
    public string Icon { get; init; } = string.Empty;

    public TimeSpan Time { get; init; }

    [ObservableProperty]
    private bool _isNext;

    [ObservableProperty]
    private bool _isPassed;
}
