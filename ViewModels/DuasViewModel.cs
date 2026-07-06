using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;
using PrayerApp.Data;

namespace PrayerApp.ViewModels;

public sealed record DuaItem(
    string Title,
    string ArabicText,
    string Translation,
    string Reference,
    int Surah,
    int Verse);

/// <summary>
/// Well-known duas from the Quran, rendered from the app's own verse database
/// (Arabic + meal) — fully offline, no external content.
/// </summary>
public partial class DuasViewModel : ObservableObject
{
    private readonly IQuranRepository _quranRepository;
    private readonly ILogger<DuasViewModel> _logger;

    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<DuaItem> Duas { get; } = [];

    public DuasViewModel(IQuranRepository quranRepository, ILogger<DuasViewModel> logger)
    {
        _quranRepository = quranRepository;
        _logger = logger;
    }

    public async Task OnAppearingAsync()
    {
        if (Duas.Count > 0)
            return;

        try
        {
            IsBusy = true;
            foreach (var dua in QuranicDuas.All)
            {
                var item = await BuildItemAsync(dua);
                if (item is not null)
                    Duas.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dua list load failed");
        }
        finally
        {
            IsBusy = false;
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

        return new DuaItem(dua.Title, arabic.ToString(), translation.ToString(), reference, dua.Surah, dua.Verse);
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
}
