using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;

namespace PrayerApp.ViewModels;

/// <summary>Surah/juz lists, verse search, favorites, reading resume and khatm tracking.</summary>
public partial class QuranViewModel : ObservableObject
{
    private readonly IQuranRepository _quranRepository;
    private readonly IKhatmRepository _khatmRepository;
    private readonly IKeyValueStore _keyValueStore;
    private readonly IClock _clock;
    private readonly ILogger<QuranViewModel> _logger;

    private IReadOnlyList<SurahInfo> _allSurahs = [];
    private CancellationTokenSource? _searchCts;
    private KhatmPlan? _activePlan;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isSearchMode;
    [ObservableProperty] private bool _showFavorites;
    [ObservableProperty] private bool _showJuzList;
    [ObservableProperty] private bool _showTopics;
    [ObservableProperty] private bool _showTopicVerses;
    [ObservableProperty] private string _selectedTopicName = string.Empty;
    [ObservableProperty] private bool _sortByNuzul;
    [ObservableProperty] private string _emptyMessage = string.Empty;

    // "Continue reading" card
    [ObservableProperty] private bool _hasLastRead;
    [ObservableProperty] private string _lastReadText = string.Empty;

    // Khatm card
    [ObservableProperty] private bool _hasActiveKhatm;
    [ObservableProperty] private double _khatmProgress;
    [ObservableProperty] private string _khatmProgressText = string.Empty;
    [ObservableProperty] private string _khatmDailyText = string.Empty;
    [ObservableProperty] private string _khatmPageText = string.Empty;
    [ObservableProperty] private bool _isKhatmPanelOpen;
    [ObservableProperty] private string _khatmCompletedMessage = string.Empty;

    public ObservableCollection<SurahInfo> Surahs { get; } = [];
    public ObservableCollection<JuzInfo> JuzList { get; } = [];
    public ObservableCollection<Verse> SearchResults { get; } = [];
    public ObservableCollection<Verse> Favorites { get; } = [];
    public ObservableCollection<QuranTopic> Topics { get; } = [];
    public ObservableCollection<Verse> TopicVerses { get; } = [];

    public QuranViewModel(
        IQuranRepository quranRepository,
        IKhatmRepository khatmRepository,
        IKeyValueStore keyValueStore,
        IClock clock,
        ILogger<QuranViewModel> logger)
    {
        _quranRepository = quranRepository;
        _khatmRepository = khatmRepository;
        _keyValueStore = keyValueStore;
        _clock = clock;
        _logger = logger;
    }

    public async Task OnAppearingAsync()
    {
        if (_allSurahs.Count == 0)
            await LoadSurahsAsync();

        if (ShowFavorites)
            await LoadFavoritesAsync();

        RefreshLastRead();
        await RefreshKhatmAsync();
    }

    private void SortSurahs()
    {
        var sorted = SortByNuzul 
            ? _allSurahs.OrderBy(s => s.NuzulNumber).ToList() 
            : _allSurahs.OrderBy(s => s.Number).ToList();
            
        Surahs.Clear();
        foreach (var surah in sorted)
            Surahs.Add(surah);
    }

    private async Task LoadSurahsAsync()
    {
        try
        {
            IsBusy = true;
            _allSurahs = await _quranRepository.GetSurahsAsync();
            SortSurahs();

            var juzList = await _quranRepository.GetJuzListAsync();
            JuzList.Clear();
            foreach (var juz in juzList)
                JuzList.Add(juz);

            if (Topics.Count == 0)
            {
                var topics = QuranTopicProvider.GetTopics();
                foreach (var topic in topics)
                    Topics.Add(topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Surah list load failed");
            EmptyMessage = "Sure listesi yüklenemedi. Uygulamayı yeniden başlatmayı deneyin.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshLastRead()
    {
        var lastRead = SurahViewModel.GetLastRead(_keyValueStore);
        if (lastRead is not { } position ||
            _allSurahs.FirstOrDefault(s => s.Number == position.Surah) is not { } surah)
        {
            HasLastRead = false;
            return;
        }

        HasLastRead = true;
        LastReadText = $"{surah.TranslatedName} Suresi, {position.Verse}. ayet";
    }

    private async Task RefreshKhatmAsync()
    {
        try
        {
            _activePlan = await _khatmRepository.GetActivePlanAsync();
            if (_activePlan is null)
            {
                HasActiveKhatm = false;
                return;
            }

            HasActiveKhatm = true;
            KhatmProgress = _activePlan.Progress;
            KhatmProgressText = $"%{_activePlan.Progress * 100:0} · {_activePlan.LastReadPage}/{KhatmPlan.TotalPages} sayfa";
            KhatmPageText = _activePlan.LastReadPage.ToString();

            var readToday = await _khatmRepository.GetPagesReadOnAsync(_activePlan.Id, _clock.Today);
            var target = _activePlan.PagesPerDayTarget(_clock.Today);
            KhatmDailyText = target > 0
                ? $"Bugün {readToday} sayfa okundu · günlük hedef {target} sayfa"
                : $"Bugün {readToday} sayfa okundu";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khatm state load failed");
            HasActiveKhatm = false;
        }
    }

    [RelayCommand]
    private async Task ContinueReadingAsync()
    {
        if (SurahViewModel.GetLastRead(_keyValueStore) is { } position)
            await Shell.Current.GoToAsync($"SurahPage?surah={position.Surah}&verse={position.Verse}");
    }

    private void ResetViews()
    {
        ShowJuzList = false;
        ShowFavorites = false;
        ShowTopics = false;
        ShowTopicVerses = false;
        IsSearchMode = false;
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ShowSurahList() => ResetViews();

    [RelayCommand]
    private void ToggleJuzList()
    {
        var next = !ShowJuzList;
        ResetViews();
        ShowJuzList = next;
    }

    [RelayCommand]
    private void ToggleTopics()
    {
        var next = !ShowTopics;
        ResetViews();
        ShowTopics = next;
    }

    [RelayCommand]
    private async Task OpenTopicAsync(QuranTopic topic)
    {
        try
        {
            IsBusy = true;
            SelectedTopicName = topic.Name;
            ResetViews();
            ShowTopicVerses = true;
            TopicVerses.Clear();
            
            foreach (var refVal in topic.Verses)
            {
                var verse = await _quranRepository.GetVerseAsync(refVal.Surah, refVal.Verse);
                if (verse != null)
                {
                    TopicVerses.Add(verse);
                }
            }
            
            EmptyMessage = TopicVerses.Count == 0 ? "Bu konuya ait ayet bulunamadı." : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load topic verses");
            EmptyMessage = "Ayetler yüklenemedi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleSortOrder()
    {
        SortByNuzul = !SortByNuzul;
        SortSurahs();
    }

    [RelayCommand]
    private async Task OpenJuzAsync(JuzInfo juz) =>
        await Shell.Current.GoToAsync($"SurahPage?surah={juz.StartSurah}&verse={juz.StartVerse}");

    // ---- Khatm ----

    [RelayCommand]
    private async Task StartKhatmAsync(string targetDays)
    {
        try
        {
            if (_activePlan is not null)
            {
                var confirmed = await Shell.Current.DisplayAlertAsync(
                    "Yeni Hatim",
                    "Devam eden hatminiz silinip yeni bir hatim başlatılacak. Emin misiniz?",
                    "Evet, yeni başlat", "Vazgeç");
                if (!confirmed)
                    return;
            }

            _ = int.TryParse(targetDays, out var days);
            await _khatmRepository.StartPlanAsync(days, _clock.Today);
            KhatmCompletedMessage = string.Empty;
            IsKhatmPanelOpen = false;
            await RefreshKhatmAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khatm start failed");
        }
    }

    [RelayCommand]
    private void ToggleKhatmPanel() => IsKhatmPanelOpen = !IsKhatmPanelOpen;

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
            if (updated.IsCompleted)
                KhatmCompletedMessage = "Hatminiz tamamlandı, tebrikler! Yeni bir hatim başlatabilirsiniz.";

            await RefreshKhatmAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khatm page save failed");
        }
    }

    [RelayCommand]
    private async Task OpenKhatmPageAsync()
    {
        if (_activePlan is null)
            return;

        var page = Math.Clamp(_activePlan.LastReadPage + 1, 1, KhatmPlan.TotalPages);
        var verse = await _quranRepository.GetPageStartAsync(page);
        if (verse is not null)
            await Shell.Current.GoToAsync($"SurahPage?surah={verse.SurahNumber}&verse={verse.VerseNumber}");
    }

    // ---- Search / favorites ----

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        var trimmed = value.Trim();

        if (trimmed.Length < 2)
        {
            IsSearchMode = false;
            SearchResults.Clear();
            return;
        }

        var cts = _searchCts = new CancellationTokenSource();
        _ = DebouncedSearchAsync(trimmed, cts.Token);
    }

    private async Task DebouncedSearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);
            var results = await _quranRepository.SearchAsync(query, 100, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            IsSearchMode = true;
            ShowFavorites = false;
            ShowJuzList = false;
            ShowTopics = false;
            ShowTopicVerses = false;
            SearchResults.Clear();
            foreach (var verse in results)
                SearchResults.Add(verse);

            EmptyMessage = results.Count == 0 ? "Aramanızla eşleşen ayet bulunamadı." : string.Empty;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer search.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verse search failed");
        }
    }

    [RelayCommand]
    private async Task ToggleFavoritesViewAsync()
    {
        var next = !ShowFavorites;
        ResetViews();
        ShowFavorites = next;

        if (ShowFavorites)
            await LoadFavoritesAsync();
    }

    private async Task LoadFavoritesAsync()
    {
        var favorites = await _quranRepository.GetFavoritesAsync();
        Favorites.Clear();
        foreach (var verse in favorites)
            Favorites.Add(verse);

        EmptyMessage = favorites.Count == 0 ? "Henüz favori ayetiniz yok. Ayetlere dokunarak favorilere ekleyin." : string.Empty;
    }

    [RelayCommand]
    private static async Task OpenSurahAsync(SurahInfo surah) =>
        await Shell.Current.GoToAsync($"SurahPage?surah={surah.Number}");

    [RelayCommand]
    private static async Task OpenVerseAsync(Verse verse) =>
        await Shell.Current.GoToAsync($"SurahPage?surah={verse.SurahNumber}&verse={verse.VerseNumber}");
}
