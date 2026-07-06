using CommunityToolkit.Mvvm.ComponentModel;

namespace PrayerApp.Models;

public partial class PrayerTrackItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string TurkishName { get; init; } = string.Empty;

    /// <summary>Vector icon resource (e.g. "icon_fajr.png") for this prayer.</summary>
    public string Icon { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;
}
