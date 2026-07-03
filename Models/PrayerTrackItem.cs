using CommunityToolkit.Mvvm.ComponentModel;

namespace PrayerApp.Models;

public partial class PrayerTrackItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string TurkishName { get; init; } = string.Empty;
    public string Glyph { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;
}
