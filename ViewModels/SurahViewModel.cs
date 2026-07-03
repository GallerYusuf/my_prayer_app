using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;

namespace PrayerApp.ViewModels;

public partial class VerseItem : ObservableObject
{
    public required Verse Verse { get; init; }

    public int VerseNumber => Verse.VerseNumber;
    public string Text => Verse.Text;

    [ObservableProperty] private bool _isFavorite;
}

/// <summary>Verses of a single surah with favorites, sharing and reading-position memory.</summary>
[QueryProperty(nameof(SurahNumber), "surah")]
[QueryProperty(nameof(ScrollToVerse), "verse")]
public partial class SurahViewModel : ObservableObject
{
    private const string LastReadKey = "quran_last_read";

    private readonly IQuranRepository _quranRepository;
    private readonly Core.Contracts.IKeyValueStore _keyValueStore;

    [ObservableProperty] private int _surahNumber;
    [ObservableProperty] private int _scrollToVerse;
    [ObservableProperty] private string _surahTitle = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private double _fontScale = 1.0;

    public ObservableCollection<VerseItem> Verses { get; } = [];

    public SurahViewModel(IQuranRepository quranRepository, Core.Contracts.IKeyValueStore keyValueStore)
    {
        _quranRepository = quranRepository;
        _keyValueStore = keyValueStore;
    }

    public async Task OnAppearingAsync()
    {
        if (Verses.Count > 0)
            return;

        try
        {
            IsBusy = true;
            var verses = await _quranRepository.GetVersesAsync(SurahNumber);
            if (verses.Count == 0)
                return;

            SurahTitle = $"{verses[0].SurahName} Suresi";

            foreach (var verse in verses)
            {
                Verses.Add(new VerseItem
                {
                    Verse = verse,
                    IsFavorite = await _quranRepository.IsFavoriteAsync(verse.SurahNumber, verse.VerseNumber)
                });
            }

            _keyValueStore.SetString(LastReadKey, $"{SurahNumber}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(VerseItem item)
    {
        await _quranRepository.ToggleFavoriteAsync(item.Verse.SurahNumber, item.Verse.VerseNumber);
        item.IsFavorite = !item.IsFavorite;
    }

    [RelayCommand]
    private static async Task ShareVerseAsync(VerseItem item)
    {
        var text = $"\"{item.Verse.Text}\"\n\n— {item.Verse.Reference}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Ayet Paylaş", Text = text });
    }

    public static int? GetLastReadSurah(Core.Contracts.IKeyValueStore store) =>
        int.TryParse(store.GetString(LastReadKey), out var surah) ? surah : null;
}
