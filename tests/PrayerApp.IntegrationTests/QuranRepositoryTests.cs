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
}
