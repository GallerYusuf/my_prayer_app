using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;
using PrayerApp.Data;

namespace PrayerApp.ViewModels;

public partial class EsmaItem : ObservableObject
{
    public required EsmaName Esma { get; init; }

    public int Number => Esma.Number;
    public string Arabic => Esma.Arabic;
    public string Transliteration => Esma.Transliteration;
    public string Meaning => Esma.Meaning;

    [ObservableProperty] private bool _isFavorite;
}

/// <summary>The 99 names of Allah with search, favorites, copy/share, and direct dhikr launch.</summary>
public partial class EsmaulHusnaViewModel : ObservableObject
{
    private const string FavoritesKey = "esma_favorites";

    private readonly IKeyValueStore _keyValueStore;
    private readonly List<EsmaItem> _allItems = [];
    private HashSet<int> _favoriteNumbers = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showFavoritesOnly;
    [ObservableProperty] private string _emptyMessage = string.Empty;
    [ObservableProperty] private string _actionMessage = string.Empty;

    public ObservableCollection<EsmaItem> Names { get; } = [];

    public EsmaulHusnaViewModel(IKeyValueStore keyValueStore)
    {
        _keyValueStore = keyValueStore;
        LoadFavoritesSet();

        foreach (var esma in EsmaulHusnaData.All)
        {
            _allItems.Add(new EsmaItem
            {
                Esma = esma,
                IsFavorite = _favoriteNumbers.Contains(esma.Number)
            });
        }

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void ToggleFavoritesFilter()
    {
        ShowFavoritesOnly = !ShowFavoritesOnly;
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleFavorite(EsmaItem item)
    {
        item.IsFavorite = !item.IsFavorite;
        if (item.IsFavorite)
            _favoriteNumbers.Add(item.Number);
        else
            _favoriteNumbers.Remove(item.Number);

        SaveFavoritesSet();
        ShowToast(item.IsFavorite ? $"{item.Transliteration} favorilere eklendi" : "Favorilerden çıkarıldı");

        if (ShowFavoritesOnly)
            ApplyFilter();
    }

    [RelayCommand]
    private async Task CopyAsync(EsmaItem item)
    {
        var text = $"{item.Arabic} — {item.Transliteration}: {item.Meaning}";
        await Clipboard.Default.SetTextAsync(text);
        ShowToast($"{item.Transliteration} panoya kopyalandı");
    }

    [RelayCommand]
    private static async Task ShareAsync(EsmaItem item)
    {
        var text = $"{item.Arabic}\n{item.Number}. {item.Transliteration}\n\"{item.Meaning}\"\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = item.Transliteration, Text = text });
    }

    [RelayCommand]
    private static async Task StartDhikrAsync(EsmaItem item)
    {
        var preset = Uri.EscapeDataString($"Ya {item.Transliteration.Replace("El-", "").Replace("Er-", "").Replace("Es-", "").Replace("Eş-", "").Replace("Ez-", "").Replace("Et-", "").Replace("En-", "").Replace("Ed-", "")}");
        await Shell.Current.GoToAsync($"DhikrPage?preset={preset}&target=99");
    }

    private void ApplyFilter()
    {
        var trimmed = SearchText.Trim();
        IEnumerable<EsmaItem> query = _allItems;

        if (ShowFavoritesOnly)
            query = query.Where(n => n.IsFavorite);

        if (trimmed.Length > 0)
        {
            query = query.Where(n =>
                n.Transliteration.Contains(trimmed, StringComparison.CurrentCultureIgnoreCase) ||
                n.Meaning.Contains(trimmed, StringComparison.CurrentCultureIgnoreCase) ||
                n.Number.ToString() == trimmed);
        }

        var matches = query.ToList();
        Names.Clear();
        foreach (var name in matches)
            Names.Add(name);

        if (matches.Count == 0)
        {
            EmptyMessage = ShowFavoritesOnly
                ? "Henüz favori esmanız yok. Kalp simgesine dokunarak favorilere ekleyebilirsiniz."
                : "Aramanızla eşleşen isim bulunamadı.";
        }
        else
        {
            EmptyMessage = string.Empty;
        }
    }

    private void LoadFavoritesSet()
    {
        var json = _keyValueStore.GetString(FavoritesKey);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            _favoriteNumbers = JsonSerializer.Deserialize<HashSet<int>>(json) ?? [];
        }
        catch (JsonException)
        {
            _favoriteNumbers = [];
        }
    }

    private void SaveFavoritesSet() =>
        _keyValueStore.SetString(FavoritesKey, JsonSerializer.Serialize(_favoriteNumbers));

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
