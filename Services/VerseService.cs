using System.Text.Json;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.Services;

public class VerseService : IVerseService
{
    private readonly object _lock = new();
    private List<QuranVerse> _verses = [];
    private List<int> _shuffleBag = [];

    public async Task InitializeAsync()
    {
        if (_verses.Count > 0)
            return;

        await using var stream = await FileSystem.OpenAppPackageFileAsync("quran_tr.json");
        var surahs = await JsonSerializer.DeserializeAsync<List<QuranSurahDto>>(stream)
            ?? throw new InvalidOperationException("Ayet dosyası okunamadı.");

        _verses = surahs
            .SelectMany(s => s.Verses.Select(v => new QuranVerse(
                SurahName: s.Translation,
                AyahNumber: v.Id,
                Text: v.Translation.Trim())))
            .Where(v => !string.IsNullOrWhiteSpace(v.Text))
            .ToList();

        if (_verses.Count == 0)
            throw new InvalidOperationException("Ayet listesi boş.");

        RefillShuffleBag();
    }

    public QuranVerse GetRandomVerse()
    {
        EnsureLoaded();

        lock (_lock)
        {
            if (_shuffleBag.Count == 0)
                RefillShuffleBag();

            var index = _shuffleBag[^1];
            _shuffleBag.RemoveAt(_shuffleBag.Count - 1);
            return _verses[index];
        }
    }

    public string GetRandomVerseText() => GetRandomVerse().DisplayText;

    private void RefillShuffleBag()
    {
        _shuffleBag = Enumerable.Range(0, _verses.Count).ToList();

        for (var i = _shuffleBag.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (_shuffleBag[i], _shuffleBag[j]) = (_shuffleBag[j], _shuffleBag[i]);
        }
    }

    private void EnsureLoaded()
    {
        if (_verses.Count == 0)
            throw new InvalidOperationException("Ayetler henüz yüklenmedi. Önce InitializeAsync çağırın.");
    }
}
