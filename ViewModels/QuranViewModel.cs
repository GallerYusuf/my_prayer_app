using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;

namespace PrayerApp.ViewModels;

/// <summary>Surah list + verse search + favorites hub.</summary>
public partial class QuranViewModel : ObservableObject
{
    private readonly IQuranRepository _quranRepository;
    private readonly ILogger<QuranViewModel> _logger;

    private IReadOnlyList<SurahInfo> _allSurahs = [];
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isSearchMode;
    [ObservableProperty] private bool _showFavorites;
    [ObservableProperty] private string _emptyMessage = string.Empty;

    public ObservableCollection<SurahInfo> Surahs { get; } = [];
    public ObservableCollection<Verse> SearchResults { get; } = [];
    public ObservableCollection<Verse> Favorites { get; } = [];

    public QuranViewModel(IQuranRepository quranRepository, ILogger<QuranViewModel> logger)
    {
        _quranRepository = quranRepository;
        _logger = logger;
    }

    public async Task OnAppearingAsync()
    {
        if (_allSurahs.Count == 0)
            await LoadSurahsAsync();

        if (ShowFavorites)
            await LoadFavoritesAsync();
    }

    private async Task LoadSurahsAsync()
    {
        try
        {
            IsBusy = true;
            _allSurahs = await _quranRepository.GetSurahsAsync();
            Surahs.Clear();
            foreach (var surah in _allSurahs)
                Surahs.Add(surah);
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
        ShowFavorites = !ShowFavorites;
        IsSearchMode = false;
        SearchText = string.Empty;

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
