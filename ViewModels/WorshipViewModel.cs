using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.ViewModels;

public partial class WorshipViewModel : ObservableObject
{
    private const string KazaStorageKey = "kaza_counters";
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private static readonly (string Name, int Target)[] TesbihatSteps =
    [
        ("Sübhanallah", 33),
        ("Elhamdülillah", 33),
        ("Allahu Ekber", 33)
    ];

    private static readonly (string Key, string Name, string Icon)[] KazaDefinitions =
    [
        ("Fajr", "Sabah", "icon_fajr.png"),
        ("Dhuhr", "Öğle", "icon_dhuhr.png"),
        ("Asr", "İkindi", "icon_asr.png"),
        ("Maghrib", "Akşam", "icon_maghrib.png"),
        ("Isha", "Yatsı", "icon_isha.png"),
        ("Witr", "Vitir", "icon_isha.png"),
        ("Fast", "Oruç", "icon_crescent.png")
    ];

    private readonly IDhikrRepository _dhikrRepository;
    private readonly IKhatmRepository _khatmRepository;
    private readonly IQuranRepository _quranRepository;
    private readonly IPrayerTrackingService _trackingService;
    private readonly IKeyValueStore _keyValueStore;
    private readonly IClock _clock;
    private readonly ILogger<WorshipViewModel> _logger;

    private DhikrSession? _session;
    private int _tesbihatStep = -1;
    private int? _lastResetCount;
    private string? _lastResetDhikrName;
    private (string Key, string Name, int OldValue)? _lastKazaUndo;
    private KhatmPlan? _activePlan;

    // Active section tab: 0 = Zikir, 1 = Namaz & Kaza, 2 = Hatim
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDhikrSection))]
    [NotifyPropertyChangedFor(nameof(IsPrayerSection))]
    [NotifyPropertyChangedFor(nameof(IsKhatmSection))]
    private int _activeSection;

    public bool IsDhikrSection => ActiveSection == 0;
    public bool IsPrayerSection => ActiveSection == 1;
    public bool IsKhatmSection => ActiveSection == 2;

    // ---- Dhikr state ----
    [ObservableProperty] private string _dhikrName = "Sübhanallah";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDecrementDhikr))]
    private int _count;
    [ObservableProperty] private int _targetCount = 33;
    [ObservableProperty] private string _progressText = "0 / 33";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _vibrationEnabled = true;
    [ObservableProperty] private bool _targetReached;
    [ObservableProperty] private string _customDhikr = string.Empty;
    [ObservableProperty] private bool _isTesbihatMode;
    [ObservableProperty] private string _tesbihatStepText = string.Empty;
    [ObservableProperty] private bool _canUndoReset;
    [ObservableProperty] private string _undoResetText = string.Empty;

    public bool CanDecrementDhikr => Count > 0;

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

    // ---- Daily Prayer & Kaza state ----
    [ObservableProperty] private string _trackProgressText = "0/5 vakit kılındı";
    [ObservableProperty] private double _trackProgressRatio;
    [ObservableProperty] private int _trackStreak;
    [ObservableProperty] private string _kazaTotalText = string.Empty;
    [ObservableProperty] private bool _canUndoKaza;
    [ObservableProperty] private string _undoKazaText = string.Empty;

    public ObservableCollection<PrayerTrackItem> TrackItems { get; } = [];
    public ObservableCollection<DailyPrayerSummary> RecentDays { get; } = [];
    public ObservableCollection<KazaItem> KazaItems { get; } = [];

    // ---- Khatm state ----
    [ObservableProperty] private bool _hasActiveKhatm;
    [ObservableProperty] private double _khatmProgress;
    [ObservableProperty] private string _khatmProgressText = string.Empty;
    [ObservableProperty] private string _khatmDailyText = string.Empty;
    [ObservableProperty] private string _khatmPageText = string.Empty;
    [ObservableProperty] private bool _isKhatmPanelOpen;
    [ObservableProperty] private string _khatmStatusMessage = string.Empty;
    [ObservableProperty] private string _lastReadQuranText = "Kur'an okumaya başla";

    public WorshipViewModel(
        IDhikrRepository dhikrRepository,
        IKhatmRepository khatmRepository,
        IQuranRepository quranRepository,
        IPrayerTrackingService trackingService,
        IKeyValueStore keyValueStore,
        IClock clock,
        ILogger<WorshipViewModel> logger)
    {
        _dhikrRepository = dhikrRepository;
        _khatmRepository = khatmRepository;
        _quranRepository = quranRepository;
        _trackingService = trackingService;
        _keyValueStore = keyValueStore;
        _clock = clock;
        _logger = logger;
    }

    public async Task OnAppearingAsync()
    {
        await LoadDhikrSessionAsync();
        await LoadDhikrHistoryAsync();
        LoadPrayerTracking();
        LoadKazaCounters();
        await RefreshKhatmAsync();
    }

    [RelayCommand]
    private void SelectSection(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx))
            ActiveSection = Math.Clamp(idx, 0, 2);
    }

    // ==================== DHIKR ====================

    private async Task LoadDhikrSessionAsync()
    {
        _session = await _dhikrRepository.GetTodaySessionAsync(DhikrName, _clock.Today);
        Count = _session?.Count ?? 0;
        if (_session is not null && _session.TargetCount > 0)
            TargetCount = _session.TargetCount;
        UpdateDhikrProgress();
    }

    [RelayCommand]
    private async Task IncrementDhikrAsync()
    {
        CanUndoReset = false;
        Count++;
        UpdateDhikrProgress();

        if (VibrationEnabled)
            TryVibrate(Count == TargetCount ? 250 : 25);

        await PersistDhikrAsync();

        if (IsTesbihatMode && Count >= TargetCount)
            await AdvanceTesbihatAsync();
    }

    [RelayCommand]
    private async Task DecrementDhikrAsync()
    {
        if (Count <= 0)
            return;

        Count--;
        UpdateDhikrProgress();
        if (VibrationEnabled)
            TryVibrate(15);

        await PersistDhikrAsync();
    }

    [RelayCommand]
    private async Task ResetDhikrAsync()
    {
        if (Count <= 0)
            return;

        _lastResetCount = Count;
        _lastResetDhikrName = DhikrName;
        UndoResetText = $"{DhikrName} ({Count}) sıfırlandı";
        CanUndoReset = true;

        Count = 0;
        UpdateDhikrProgress();
        await PersistDhikrAsync();
    }

    [RelayCommand]
    private async Task UndoResetDhikrAsync()
    {
        if (!CanUndoReset || _lastResetCount is null)
            return;

        if (!string.IsNullOrEmpty(_lastResetDhikrName) && _lastResetDhikrName != DhikrName)
        {
            DhikrName = _lastResetDhikrName;
            _session = await _dhikrRepository.GetTodaySessionAsync(DhikrName, _clock.Today);
        }

        Count = _lastResetCount.Value;
        CanUndoReset = false;
        _lastResetCount = null;
        UpdateDhikrProgress();
        await PersistDhikrAsync();
    }

    [RelayCommand]
    private async Task SelectPresetAsync(DhikrPreset preset)
    {
        ExitTesbihat();
        CanUndoReset = false;
        DhikrName = preset.Name;
        TargetCount = preset.Target;
        await LoadDhikrSessionAsync();
    }

    [RelayCommand]
    private async Task ApplyCustomDhikrAsync()
    {
        var name = CustomDhikr.Trim();
        if (name.Length is < 2 or > 60)
            return;

        ExitTesbihat();
        CanUndoReset = false;
        DhikrName = name;
        CustomDhikr = string.Empty;
        await LoadDhikrSessionAsync();
    }

    [RelayCommand]
    private async Task StartTesbihatAsync()
    {
        _tesbihatStep = 0;
        IsTesbihatMode = true;
        CanUndoReset = false;
        await LoadTesbihatStepAsync();
    }

    [RelayCommand]
    private void ExitTesbihat()
    {
        IsTesbihatMode = false;
        _tesbihatStep = -1;
        TesbihatStepText = string.Empty;
    }

    private async Task AdvanceTesbihatAsync()
    {
        if (_tesbihatStep >= TesbihatSteps.Length - 1)
        {
            IsTesbihatMode = false;
            _tesbihatStep = -1;
            TesbihatStepText = "Namaz tesbihatı tamamlandı (99/99)";
            if (VibrationEnabled)
                TryVibrate(350);
            return;
        }

        _tesbihatStep++;
        if (VibrationEnabled)
            TryVibrate(200);
        await LoadTesbihatStepAsync();
    }

    private async Task LoadTesbihatStepAsync()
    {
        var (name, target) = TesbihatSteps[_tesbihatStep];
        DhikrName = name;
        TargetCount = target;
        TesbihatStepText = $"Adım {_tesbihatStep + 1}/{TesbihatSteps.Length} · {name} ({target})";

        _session = await _dhikrRepository.GetTodaySessionAsync(DhikrName, _clock.Today);
        Count = _session?.Count ?? 0;
        UpdateDhikrProgress();
    }

    private async Task PersistDhikrAsync()
    {
        _session = await _dhikrRepository.UpsertAsync(new DhikrSession(
            _session?.Id ?? 0, DhikrName, Count, TargetCount, _clock.Today));

        await LoadDhikrHistoryAsync();
    }

    private async Task LoadDhikrHistoryAsync()
    {
        var history = await _dhikrRepository.GetHistoryAsync(10);
        History.Clear();
        foreach (var item in history)
        {
            History.Add(new DhikrHistoryItem(
                item.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMM", TurkishCulture),
                item.DhikrName,
                item.TargetCount > 0 ? $"{item.Count} / {item.TargetCount}" : item.Count.ToString()));
        }
    }

    private void UpdateDhikrProgress()
    {
        ProgressText = TargetCount > 0 ? $"{Count} / {TargetCount}" : Count.ToString();
        Progress = TargetCount > 0 ? Math.Clamp((double)Count / TargetCount, 0, 1) : 0;
        TargetReached = TargetCount > 0 && Count >= TargetCount;
    }

    [RelayCommand]
    private static async Task OpenFullDhikrAsync() => await Shell.Current.GoToAsync("DhikrPage");

    // ==================== PRAYER & KAZA ====================

    private void LoadPrayerTracking()
    {
        TrackItems.Clear();
        foreach (var item in _trackingService.GetTodayTrackItems())
            TrackItems.Add(item);

        RefreshTrackingSummary();
    }

    [RelayCommand]
    private void TogglePrayer(PrayerTrackItem item)
    {
        _trackingService.Toggle(item.Name);
        item.IsCompleted = !item.IsCompleted;
        RefreshTrackingSummary();
    }

    private void RefreshTrackingSummary()
    {
        var done = _trackingService.GetCompletedCountToday();
        TrackProgressText = $"{done}/5 vakit kılındı";
        TrackProgressRatio = done / 5.0;
        TrackStreak = _trackingService.GetStreak();

        RecentDays.Clear();
        foreach (var day in _trackingService.GetRecentDaysSummary(7))
            RecentDays.Add(day);
    }

    private void LoadKazaCounters()
    {
        var counts = ReadKazaDict();
        KazaItems.Clear();
        foreach (var (key, name, icon) in KazaDefinitions)
            KazaItems.Add(new KazaItem { Key = key, Name = name, Icon = icon, Count = counts.GetValueOrDefault(key) });

        UpdateKazaTotal();
    }

    [RelayCommand]
    private void IncrementKaza(KazaItem item) => AdjustKaza(item, +1);

    [RelayCommand]
    private void DecrementKaza(KazaItem item) => AdjustKaza(item, -1);

    [RelayCommand]
    private void IncrementKazaTen(KazaItem item) => AdjustKaza(item, +10);

    [RelayCommand]
    private void DecrementKazaTen(KazaItem item) => AdjustKaza(item, -10);

    private void AdjustKaza(KazaItem item, int delta)
    {
        var oldVal = item.Count;
        var newVal = Math.Clamp(oldVal + delta, 0, 99_999);
        if (newVal == oldVal)
            return;

        _lastKazaUndo = (item.Key, item.Name, oldVal);
        CanUndoKaza = true;
        var diff = newVal - oldVal;
        UndoKazaText = $"{item.Name}: {(diff > 0 ? $"+{diff}" : diff.ToString())} ({newVal})";

        item.Count = newVal;
        SaveKazaDict();
        UpdateKazaTotal();
    }

    [RelayCommand]
    private void UndoKaza()
    {
        if (!CanUndoKaza || _lastKazaUndo is not { } undo)
            return;

        var target = KazaItems.FirstOrDefault(i => i.Key == undo.Key);
        if (target is not null)
        {
            target.Count = undo.OldValue;
            SaveKazaDict();
            UpdateKazaTotal();
        }

        CanUndoKaza = false;
        _lastKazaUndo = null;
    }

    private void UpdateKazaTotal()
    {
        var prayers = KazaItems.Where(i => i.Key != "Fast").Sum(i => i.Count);
        var fasts = KazaItems.FirstOrDefault(i => i.Key == "Fast")?.Count ?? 0;
        if (prayers == 0 && fasts == 0)
            KazaTotalText = "Kayıtlı kaza borcu bulunmuyor.";
        else if (fasts > 0)
            KazaTotalText = $"Toplam {prayers} vakit namaz · {fasts} gün oruç kazası";
        else
            KazaTotalText = $"Toplam {prayers} vakit namaz kazası";
    }

    private Dictionary<string, int> ReadKazaDict()
    {
        var json = _keyValueStore.GetString(KazaStorageKey);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void SaveKazaDict()
    {
        var counts = KazaItems.ToDictionary(i => i.Key, i => i.Count);
        _keyValueStore.SetString(KazaStorageKey, JsonSerializer.Serialize(counts));
    }

    // ==================== KHATM ====================

    private async Task RefreshKhatmAsync()
    {
        try
        {
            var lastRead = SurahViewModel.GetLastRead(_keyValueStore);
            if (lastRead is { } pos)
            {
                var surahs = await _quranRepository.GetSurahsAsync();
                var s = surahs.FirstOrDefault(x => x.Number == pos.Surah);
                LastReadQuranText = s is not null
                    ? $"Son okunan: {s.TranslatedName} Suresi, {pos.Verse}. ayet"
                    : $"Son okunan: {pos.Surah}. Sure, {pos.Verse}. ayet";
            }
            else
            {
                LastReadQuranText = "Henüz okuma kaydı yok · Fatiha'dan başlayın";
            }

            _activePlan = await _khatmRepository.GetActivePlanAsync();
            if (_activePlan is null)
            {
                HasActiveKhatm = false;
                return;
            }

            HasActiveKhatm = true;
            KhatmProgress = _activePlan.Progress;
            KhatmProgressText = $"%{_activePlan.Progress * 100:0} tamamlandı · {_activePlan.LastReadPage} / {KhatmPlan.TotalPages} sayfa";
            KhatmPageText = _activePlan.LastReadPage.ToString();

            var readToday = await _khatmRepository.GetPagesReadOnAsync(_activePlan.Id, _clock.Today);
            var target = _activePlan.PagesPerDayTarget(_clock.Today);
            KhatmDailyText = target > 0
                ? $"Bugün {readToday} sayfa okundu · Günlük öneri: {target} sayfa"
                : $"Bugün {readToday} sayfa okundu · Serbest plan";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worship Khatm refresh failed");
            HasActiveKhatm = false;
        }
    }

    [RelayCommand]
    private void ToggleKhatmPanel() => IsKhatmPanelOpen = !IsKhatmPanelOpen;

    [RelayCommand]
    private async Task StartKhatmAsync(string targetDays)
    {
        try
        {
            if (_activePlan is not null)
            {
                var confirmed = await Shell.Current.DisplayAlertAsync(
                    "Yeni Hatim Planı",
                    "Mevcut hatim planınız sıfırlanarak yeni bir hatim başlatılacak. Onaylıyor musunuz?",
                    "Başlat", "Vazgeç");
                if (!confirmed)
                    return;
            }

            _ = int.TryParse(targetDays, out var days);
            await _khatmRepository.StartPlanAsync(days, _clock.Today);
            KhatmStatusMessage = days > 0 ? $"{days} günlük hatim planı başlatıldı." : "Serbest hatim planı başlatıldı.";
            IsKhatmPanelOpen = false;
            await RefreshKhatmAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartKhatmAsync failed");
        }
    }

    [RelayCommand]
    private async Task SaveKhatmPageAsync()
    {
        if (_activePlan is null)
            return;

        if (!int.TryParse(KhatmPageText?.Trim(), out var page) || page < 0 || page > KhatmPlan.TotalPages)
        {
            KhatmPageText = _activePlan.LastReadPage.ToString();
            return;
        }

        try
        {
            var updated = await _khatmRepository.SetLastReadPageAsync(_activePlan.Id, page, _clock.Today);
            KhatmStatusMessage = updated.IsCompleted
                ? "Elhamdülillah, hatminiz tamamlandı! Yeni bir hatim başlatabilirsiniz."
                : $"Kaldığınız sayfa {page} olarak kaydedildi.";

            await RefreshKhatmAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveKhatmPageAsync failed");
        }
    }

    [RelayCommand]
    private async Task IncrementKhatmPageAsync()
    {
        if (_activePlan is null)
            return;

        var next = Math.Clamp(_activePlan.LastReadPage + 1, 0, KhatmPlan.TotalPages);
        KhatmPageText = next.ToString();
        await SaveKhatmPageAsync();
    }

    [RelayCommand]
    private async Task OpenKhatmPageInQuranAsync()
    {
        if (_activePlan is null)
        {
            await Shell.Current.GoToAsync("//QuranPage");
            return;
        }

        var page = Math.Clamp(_activePlan.LastReadPage + 1, 1, KhatmPlan.TotalPages);
        var verse = await _quranRepository.GetPageStartAsync(page);
        if (verse is not null)
            await Shell.Current.GoToAsync($"SurahPage?surah={verse.SurahNumber}&verse={verse.VerseNumber}");
    }

    [RelayCommand]
    private async Task ContinueLastReadQuranAsync()
    {
        if (SurahViewModel.GetLastRead(_keyValueStore) is { } pos)
            await Shell.Current.GoToAsync($"SurahPage?surah={pos.Surah}&verse={pos.Verse}");
        else
            await Shell.Current.GoToAsync("SurahPage?surah=1&verse=1");
    }

    private static void TryVibrate(int milliseconds)
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
        }
        catch (FeatureNotSupportedException)
        {
        }
    }
}
