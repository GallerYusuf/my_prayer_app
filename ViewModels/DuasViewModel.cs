using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;
using PrayerApp.Data;

namespace PrayerApp.ViewModels;

public partial class DuaItem : ObservableObject
{
    public required string Title { get; init; }
    public required string ArabicText { get; init; }
    public required string Translation { get; init; }
    public required string Reference { get; init; }
    public required int Surah { get; init; }
    public required int Verse { get; init; }

    [ObservableProperty] private bool _isFavorite;
}

/// <summary>
/// Well-known duas from the Quran with search, favorites (synced with Quran favorites), copy, share, and open-in-Quran.
/// </summary>
public partial class DuasViewModel : ObservableObject
{
    private readonly IQuranRepository _quranRepository;
    private readonly ILogger<DuasViewModel> _logger;
    private readonly List<DuaItem> _allDuas = [];

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showFavoritesOnly;
    [ObservableProperty] private string _emptyMessage = string.Empty;
    [ObservableProperty] private string _actionMessage = string.Empty;

    public ObservableCollection<DuaItem> Duas { get; } = [];

    public DuasViewModel(IQuranRepository quranRepository, ILogger<DuasViewModel> logger)
    {
        _quranRepository = quranRepository;
        _logger = logger;
    }

    public async Task OnAppearingAsync()
    {
        try
        {
            if (_allDuas.Count == 0)
            {
                IsBusy = true;
                foreach (var dua in QuranicDuas.All)
                {
                    var item = await BuildItemAsync(dua);
                    if (item is not null)
                        _allDuas.Add(item);
                }
            }
            else
            {
                foreach (var item in _allDuas)
                    item.IsFavorite = await _quranRepository.IsFavoriteAsync(item.Surah, item.Verse);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dua list load failed");
            EmptyMessage = "Dualar yüklenemedi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void ShowAll()
    {
        ShowFavoritesOnly = false;
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleFavoritesFilter()
    {
        ShowFavoritesOnly = !ShowFavoritesOnly;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim();
        IEnumerable<DuaItem> query = _allDuas;

        if (ShowFavoritesOnly)
            query = query.Where(d => d.IsFavorite);

        if (q.Length > 0)
        {
            query = query.Where(d =>
                d.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                d.Translation.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                d.Reference.Contains(q, StringComparison.CurrentCultureIgnoreCase));
        }

        var filtered = query.ToList();
        Duas.Clear();
        foreach (var item in filtered)
            Duas.Add(item);

        if (filtered.Count == 0)
        {
            EmptyMessage = ShowFavoritesOnly
                ? "Henüz favori duanız yok. Kalp simgesine dokunarak duaları favorilere ekleyebilirsiniz."
                : "Aramanızla eşleşen dua bulunamadı.";
        }
        else
        {
            EmptyMessage = string.Empty;
        }
    }

    private async Task<DuaItem?> BuildItemAsync(QuranicDua dua)
    {
        var arabic = new StringBuilder();
        var translation = new StringBuilder();
        string surahName = string.Empty;

        for (var verse = dua.Verse; verse <= dua.EffectiveEnd; verse++)
        {
            var row = await _quranRepository.GetVerseAsync(dua.Surah, verse);
            if (row is null)
                return null;

            surahName = row.SurahName;
            if (arabic.Length > 0)
                arabic.Append(' ');
            arabic.Append(row.ArabicText);

            if (translation.Length > 0)
                translation.Append(' ');
            translation.Append(row.Text);
        }

        var reference = dua.Verse == dua.EffectiveEnd
            ? $"{surahName} Suresi, {dua.Verse}. Ayet"
            : $"{surahName} Suresi, {dua.Verse}-{dua.EffectiveEnd}. Ayetler";

        var isFav = await _quranRepository.IsFavoriteAsync(dua.Surah, dua.Verse);

        return new DuaItem
        {
            Title = dua.Title,
            ArabicText = arabic.ToString(),
            Translation = translation.ToString(),
            Reference = reference,
            Surah = dua.Surah,
            Verse = dua.Verse,
            IsFavorite = isFav
        };
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(DuaItem item)
    {
        await _quranRepository.ToggleFavoriteAsync(item.Surah, item.Verse);
        item.IsFavorite = !item.IsFavorite;
        ShowToast(item.IsFavorite ? "Dua favorilere eklendi" : "Favorilerden çıkarıldı");
        if (ShowFavoritesOnly)
            ApplyFilter();
    }

    [RelayCommand]
    private async Task CopyAsync(DuaItem item)
    {
        var text = $"{item.ArabicText}\n\n\"{item.Translation}\" — {item.Reference}";
        await Clipboard.Default.SetTextAsync(text);
        ShowToast($"\"{item.Title}\" panoya kopyalandı");
    }

    [RelayCommand]
    private static async Task ShareAsync(DuaItem item)
    {
        var text = $"{item.ArabicText}\n\n\"{item.Translation}\"\n\n— {item.Reference}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = item.Title, Text = text });
    }

    [RelayCommand]
    private static async Task OpenInQuranAsync(DuaItem item) =>
        await Shell.Current.GoToAsync($"SurahPage?surah={item.Surah}&verse={item.Verse}");

    private void ShowToast(string msg)
    {
        ActionMessage = msg;
        _ = ClearToastAsync();
    }

    private async Task ClearToastAsync()
    {
        await Task.Delay(2200);
        ActionMessage = string.Empty;
    }
}
