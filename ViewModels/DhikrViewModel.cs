using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;

namespace PrayerApp.ViewModels;

public sealed record DhikrPreset(string Name, int Target);

public sealed record DhikrHistoryItem(string DateText, string Name, string CountText);

public partial class DhikrViewModel : ObservableObject
{
    private readonly IDhikrRepository _dhikrRepository;
    private readonly ISettingsStore _settingsStore;
    private readonly IClock _clock;

    private DhikrSession? _session;

    [ObservableProperty] private string _dhikrName = "Sübhanallah";
    [ObservableProperty] private int _count;
    [ObservableProperty] private int _targetCount = 33;
    [ObservableProperty] private string _progressText = "0 / 33";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _vibrationEnabled = true;
    [ObservableProperty] private bool _targetReached;
    [ObservableProperty] private string _customDhikr = string.Empty;

    public ObservableCollection<DhikrPreset> Presets { get; } =
    [
        new("Sübhanallah", 33),
        new("Elhamdülillah", 33),
        new("Allahu Ekber", 34),
        new("La ilahe illallah", 100),
        new("Estağfirullah", 100),
        new("Salavat", 100)
    ];

    public ObservableCollection<DhikrHistoryItem> History { get; } = [];

    public DhikrViewModel(IDhikrRepository dhikrRepository, ISettingsStore settingsStore, IClock clock)
    {
        _dhikrRepository = dhikrRepository;
        _settingsStore = settingsStore;
        _clock = clock;
    }

    public async Task OnAppearingAsync()
    {
        await LoadSessionAsync();
        await LoadHistoryAsync();
    }

    private async Task LoadSessionAsync()
    {
        _session = await _dhikrRepository.GetTodaySessionAsync(DhikrName, _clock.Today);
        Count = _session?.Count ?? 0;
        if (_session is not null && _session.TargetCount > 0)
            TargetCount = _session.TargetCount;
        UpdateProgress();
    }

    [RelayCommand]
    private async Task IncrementAsync()
    {
        Count++;
        UpdateProgress();

        if (VibrationEnabled)
            TryVibrate(Count == TargetCount ? 250 : 30);

        await PersistAsync();
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (Count > 0)
        {
            var confirmed = await Shell.Current.DisplayAlertAsync(
                "Sayacı Sıfırla",
                $"{DhikrName} sayacındaki {Count} zikri sıfırlamak istiyor musunuz?",
                "Sıfırla",
                "Vazgeç");

            if (!confirmed)
                return;
        }

        Count = 0;
        UpdateProgress();
        await PersistAsync();
    }

    [RelayCommand]
    private async Task SelectPresetAsync(DhikrPreset preset)
    {
        DhikrName = preset.Name;
        TargetCount = preset.Target;
        await LoadSessionAsync();
    }

    [RelayCommand]
    private async Task ApplyCustomDhikrAsync()
    {
        var name = CustomDhikr.Trim();
        if (name.Length is < 2 or > 60)
            return;

        DhikrName = name;
        CustomDhikr = string.Empty;
        await LoadSessionAsync();
    }

    private async Task PersistAsync()
    {
        _session = await _dhikrRepository.UpsertAsync(new DhikrSession(
            _session?.Id ?? 0, DhikrName, Count, TargetCount, _clock.Today));

        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _dhikrRepository.GetHistoryAsync(20);
        History.Clear();
        foreach (var item in history)
        {
            History.Add(new DhikrHistoryItem(
                item.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMM", new System.Globalization.CultureInfo("tr-TR")),
                item.DhikrName,
                item.TargetCount > 0 ? $"{item.Count} / {item.TargetCount}" : item.Count.ToString()));
        }
    }

    private void UpdateProgress()
    {
        ProgressText = TargetCount > 0 ? $"{Count} / {TargetCount}" : Count.ToString();
        Progress = TargetCount > 0 ? Math.Clamp((double)Count / TargetCount, 0, 1) : 0;
        TargetReached = TargetCount > 0 && Count >= TargetCount;
    }

    private static void TryVibrate(int milliseconds)
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
        }
        catch (FeatureNotSupportedException)
        {
            // Device has no vibrator; counting still works.
        }
    }
}
