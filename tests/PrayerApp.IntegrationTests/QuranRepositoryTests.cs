using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PrayerApp.Infrastructure.Database;
using PrayerApp.Infrastructure.Repositories;
using Xunit;

namespace PrayerApp.IntegrationTests;

public sealed class QuranRepositoryTests : IAsyncLifetime
{
    private const string SampleJson = """
    [
      {"id":1,"name":"x","transliteration":"Al-Fatihah","translation":"Fâtiha","total_verses":2,
       "verses":[{"id":1,"text":"a","translation":"Rahman ve Rahim olan Allah'ın adıyla"},
                 {"id":2,"text":"b","translation":"Hamd, Alemlerin Rabbi Allah'a mahsustur"}]},
      {"id":2,"name":"y","transliteration":"Al-Baqarah","translation":"Bakara","total_verses":3,
       "verses":[{"id":1,"text":"c","translation":"Elif, Lam, Mim"},
                 {"id":2,"text":"d","translation":"Bu, kendisinde şüphe olmayan kitaptır"},
                 {"id":3,"text":"e","translation":"Onlar gayba inanırlar, namaz kılarlar"}]}
    ]
    """;

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"prayerapp_test_{Guid.NewGuid():N}.db3");
    private AppDatabase _database = null!;
    private QuranRepository _repository = null!;

    public Task InitializeAsync()
    {
        _database = new AppDatabase(_dbPath);
        _repository = new QuranRepository(
            _database,
            _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson))),
            new FakeClock(new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.FromHours(3))),
            NullLogger<QuranRepository>.Instance);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task Import_PopulatesSurahsAndVerses()
    {
        var surahs = await _repository.GetSurahsAsync();

        Assert.Equal(2, surahs.Count);
        Assert.Equal("Fâtiha", surahs[0].TranslatedName);
        Assert.Equal(3, surahs[1].VerseCount);
        Assert.Equal(5, await _repository.GetTotalVerseCountAsync());
    }

    [Fact]
    public async Task GetVerses_ReturnsOrderedVersesWithSurahName()
    {
        var verses = await _repository.GetVersesAsync(2);

        Assert.Equal(3, verses.Count);
        Assert.Equal("Bakara", verses[0].SurahName);
        Assert.Equal(1, verses[0].VerseNumber);
        Assert.Equal("Elif, Lam, Mim", verses[0].Text);
    }

    [Fact]
    public async Task Search_FindsCaseInsensitiveMatches()
    {
        var results = await _repository.SearchAsync("kitap");

        var verse = Assert.Single(results);
        Assert.Equal(2, verse.SurahNumber);
        Assert.Equal(2, verse.VerseNumber);
    }

    [Fact]
    public async Task Search_TooShortQuery_ReturnsEmpty()
    {
        Assert.Empty(await _repository.SearchAsync("k"));
    }

    [Fact]
    public async Task GetVerseByGlobalIndex_MapsAcrossSurahs()
    {
        var verse = await _repository.GetVerseByGlobalIndexAsync(2);

        Assert.NotNull(verse);
        Assert.Equal(2, verse.SurahNumber);
        Assert.Equal(1, verse.VerseNumber);
    }

    [Fact]
    public async Task ToggleFavorite_AddsAndRemoves()
    {
        await _repository.ToggleFavoriteAsync(1, 1);
        Assert.True(await _repository.IsFavoriteAsync(1, 1));

        var favorites = await _repository.GetFavoritesAsync();
        var favorite = Assert.Single(favorites);
        Assert.Equal("Fâtiha", favorite.SurahName);

        await _repository.ToggleFavoriteAsync(1, 1);
        Assert.False(await _repository.IsFavoriteAsync(1, 1));
        Assert.Empty(await _repository.GetFavoritesAsync());
    }

    [Fact]
    public async Task Import_StoresArabicTextAlongsideTranslation()
    {
        var verses = await _repository.GetVersesAsync(1);

        Assert.Equal("a", verses[0].ArabicText);
        Assert.Equal("Rahman ve Rahim olan Allah'ın adıyla", verses[0].Text);
    }

    [Fact]
    public async Task Import_AssignsMushafPageAndJuz()
    {
        // Page 2 and juz 1 metadata: page 2 starts at 2:1 (Tanzil table).
        var fatiha = await _repository.GetVersesAsync(1);
        var baqarah = await _repository.GetVersesAsync(2);

        Assert.All(fatiha, v => Assert.Equal(1, v.Page));
        Assert.All(fatiha, v => Assert.Equal(1, v.Juz));
        Assert.All(baqarah, v => Assert.Equal(2, v.Page));
        Assert.All(baqarah, v => Assert.Equal(1, v.Juz));
    }

    [Fact]
    public async Task GetPageStart_ReturnsFirstVerseOfPage()
    {
        var start = await _repository.GetPageStartAsync(2);

        Assert.NotNull(start);
        Assert.Equal(2, start.SurahNumber);
        Assert.Equal(1, start.VerseNumber);
    }

    [Fact]
    public async Task GetJuzList_ReturnsRangesFromImportedData()
    {
        var juzList = await _repository.GetJuzListAsync();

        var juz = Assert.Single(juzList); // sample data only covers juz 1
        Assert.Equal(1, juz.Number);
        Assert.Equal(1, juz.StartSurah);
        Assert.Equal(1, juz.StartVerse);
        Assert.Equal(1, juz.StartPage);
        Assert.Equal(2, juz.EndPage);
    }

    [Fact]
    public async Task Reimport_ForNewImportVersion_PreservesFavorites()
    {
        // Arrange: v1-era database — verses imported without the version marker.
        await _repository.ToggleFavoriteAsync(2, 2);
        var db = await _database.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM cache_meta WHERE Key LIKE 'quran_import_version%'");
        await db.ExecuteAsync("UPDATE verses SET ArabicText = ''");

        // Act: a fresh repository instance detects the missing marker and re-imports.
        var freshRepository = new QuranRepository(
            _database,
            _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson))),
            new FakeClock(new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.FromHours(3))),
            NullLogger<QuranRepository>.Instance);

        var verses = await freshRepository.GetVersesAsync(1);

        Assert.Equal("a", verses[0].ArabicText);
        Assert.True(await freshRepository.IsFavoriteAsync(2, 2));
    }
}
