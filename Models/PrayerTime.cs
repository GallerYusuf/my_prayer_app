using CommunityToolkit.Mvvm.ComponentModel;

namespace PrayerApp.Models;

public partial class PrayerTime : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string TurkishName { get; init; } = string.Empty;

    /// <summary>Vector icon resource (e.g. "icon_fajr.png") for this prayer.</summary>
    public string Icon { get; init; } = string.Empty;

    public TimeSpan Time { get; init; }

    /// <summary>True for the 5 daily obligatory prayers; false for Sunrise.</summary>
    public bool IsTrackable { get; init; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateBadgeText))]
    [NotifyPropertyChangedFor(nameof(HasStateBadge))]
    private bool _isNext;

    [ObservableProperty]
    private bool _isPassed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackButtonText))]
    private bool _isCompleted;

    public string StateBadgeText => IsNext ? "Sıradaki" : (!IsTrackable ? "Doğuş" : string.Empty);
    public bool HasStateBadge => IsNext || !IsTrackable;
    public string TrackButtonText => IsCompleted ? "✓ Kılındı" : "Kıldım";
}
