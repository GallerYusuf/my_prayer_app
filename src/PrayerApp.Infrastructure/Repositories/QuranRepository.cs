using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Infrastructure.Database;

namespace PrayerApp.Infrastructure.Repositories;

/// <summary>
/// Quran data access backed by SQLite. On first run the bundled quran_tr.json is imported
/// once (off the UI thread); afterwards all reads are indexed SQLite queries instead of
/// keeping ~6000 verses in memory. Import version 2 adds Arabic text plus mushaf page and
/// juz numbers; existing installs re-import automatically (favorites are preserved).
/// </summary>
public sealed class QuranRepository(
    AppDatabase database,
    Func<CancellationToken, Task<Stream>> sourceJsonStreamFactory,
    IClock clock,
    ILogger<QuranRepository> logger) : IQuranRepository
{
    private const string ImportVersionKey = "quran_import_version";
    private const string CurrentImportVersion = "2";

    private readonly SemaphoreSlim _importLock = new(1, 1);
    private bool _importChecked;

    public async Task<IReadOnlyList<SurahInfo>> GetSurahsAsync(CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var rows = await db.Table<Entities.SurahRow>().OrderBy(s => s.Number).ToListAsync();
        return rows.Select(r => new SurahInfo(r.Number, r.Name, r.TranslatedName, r.VerseCount, Database.QuranMetadata.SurahNuzulOrder[r.Number])).ToList();
    }

    public async Task<IReadOnlyList<Verse>> GetVersesAsync(int surahNumber, CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var surah = await db.FindAsync<Entities.SurahRow>(surahNumber);
        if (surah is null)
            return [];

        var rows = await db.Table<Entities.VerseRow>()
            .Where(v => v.SurahNumber == surahNumber)
            .OrderBy(v => v.VerseNumber)
            .ToListAsync();

        return rows
            .Select(r => new Verse(r.SurahNumber, surah.TranslatedName, r.VerseNumber, r.Text, r.ArabicText, r.Page, r.Juz))
            .ToList();
    }

    public async Task<Verse?> GetVerseAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var row = await db.Table<Entities.VerseRow>()
            .Where(v => v.SurahNumber == surahNumber && v.VerseNumber == verseNumber)
            .FirstOrDefaultAsync();
        if (row is null)
            return null;

        var surah = await db.FindAsync<Entities.SurahRow>(surahNumber);
        return new Verse(
            row.SurahNumber, surah?.TranslatedName ?? string.Empty, row.VerseNumber,
            row.Text, row.ArabicText, row.Page, row.Juz);
    }

    public async Task<IReadOnlyList<JuzInfo>> GetJuzListAsync(CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var ranges = await db.QueryAsync<JuzRange>(
            "SELECT Juz, MIN(Page) AS StartPage, MAX(Page) AS EndPage FROM verses GROUP BY Juz ORDER BY Juz");
        var surahs = await db.Table<Entities.SurahRow>().ToListAsync();
        var surahNames = surahs.ToDictionary(s => s.Number, s => s.TranslatedName);

        var result = new List<JuzInfo>(QuranMetadata.JuzCount);
        foreach (var range in ranges)
        {
            if (range.Juz is < 1 or > QuranMetadata.JuzCount)
                continue;

            var (startSurah, startVerse) = QuranMetadata.JuzStarts[range.Juz - 1];
            result.Add(new JuzInfo(
                range.Juz,
                surahNames.GetValueOrDefault(startSurah, string.Empty),
                startSurah,
                startVerse,
                range.StartPage,
                range.EndPage));
        }

        return result;
    }

    public async Task<Verse?> GetPageStartAsync(int page, CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var rows = await db.QueryAsync<VerseWithSurah>(
            """
            SELECT v.SurahNumber, v.VerseNumber, v.Text, v.ArabicText, v.Page, v.Juz, s.TranslatedName AS SurahName
            FROM verses v
            JOIN surahs s ON s.Number = v.SurahNumber
            WHERE v.Page = ?
            ORDER BY v.GlobalIndex
            LIMIT 1
            """,
            page);

        return rows.FirstOrDefault()?.ToVerse();
    }

    public async Task<IReadOnlyList<Verse>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim();
        if (trimmed.Length < 2)
            return [];

        var db = await EnsureImportedAsync(cancellationToken);
        var rows = await db.QueryAsync<VerseWithSurah>(
            """
            SELECT v.SurahNumber, v.VerseNumber, v.Text, v.ArabicText, v.Page, v.Juz, s.TranslatedName AS SurahName
            FROM verses v
            JOIN surahs s ON s.Number = v.SurahNumber
            WHERE v.Text LIKE ? COLLATE NOCASE
            ORDER BY v.GlobalIndex
            LIMIT ?
            """,
            $"%{trimmed}%", limit);

        return rows.Select(r => r.ToVerse()).ToList();
    }

    public async Task<int> GetTotalVerseCountAsync(CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        return await db.Table<Entities.VerseRow>().CountAsync();
    }

    public async Task<Verse?> GetVerseByGlobalIndexAsync(int globalIndex, CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var rows = await db.QueryAsync<VerseWithSurah>(
            """
            SELECT v.SurahNumber, v.VerseNumber, v.Text, v.ArabicText, v.Page, v.Juz, s.TranslatedName AS SurahName
            FROM verses v
            JOIN surahs s ON s.Number = v.SurahNumber
            WHERE v.GlobalIndex = ?
            LIMIT 1
            """,
            globalIndex);

        return rows.FirstOrDefault()?.ToVerse();
    }

    public async Task<bool> IsFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var count = await db.Table<Entities.FavoriteVerseRow>()
            .Where(f => f.SurahNumber == surahNumber && f.VerseNumber == verseNumber)
            .CountAsync();
        return count > 0;
    }

    public async Task ToggleFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var existing = await db.Table<Entities.FavoriteVerseRow>()
            .Where(f => f.SurahNumber == surahNumber && f.VerseNumber == verseNumber)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            await db.DeleteAsync(existing);
            return;
        }

        await db.InsertAsync(new Entities.FavoriteVerseRow
        {
            SurahNumber = surahNumber,
            VerseNumber = verseNumber,
            AddedUnixSeconds = clock.UtcNow.ToUnixTimeSeconds()
        });
    }

    public async Task<IReadOnlyList<Verse>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        var db = await EnsureImportedAsync(cancellationToken);
        var rows = await db.QueryAsync<VerseWithSurah>(
            """
            SELECT v.SurahNumber, v.VerseNumber, v.Text, v.ArabicText, v.Page, v.Juz, s.TranslatedName AS SurahName
            FROM favorite_verses f
            JOIN verses v ON v.SurahNumber = f.SurahNumber AND v.VerseNumber = f.VerseNumber
            JOIN surahs s ON s.Number = v.SurahNumber
            ORDER BY f.AddedUnixSeconds DESC
            """);

        return rows.Select(r => r.ToVerse()).ToList();
    }

    private async Task<SQLite.SQLiteAsyncConnection> EnsureImportedAsync(CancellationToken cancellationToken)
    {
        var db = await database.GetConnectionAsync();
        if (_importChecked)
            return db;

        await _importLock.WaitAsync(cancellationToken);
        try
        {
            if (_importChecked)
                return db;

            var surahCount = await db.Table<Entities.SurahRow>().CountAsync();
            var versionMarker = ImportVersionKey + ":" + CurrentImportVersion;
            var hasCurrentImport = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM cache_meta WHERE Key = ?", versionMarker) > 0;

            if (surahCount < 114 || !hasCurrentImport)
            {
                logger.LogInformation("Importing Quran data into SQLite (import v{Version})", CurrentImportVersion);
                await ImportAsync(db, cancellationToken);
                await db.ExecuteAsync(
                    "INSERT OR REPLACE INTO cache_meta (Key, LastFetchUnixSeconds) VALUES (?, ?)",
                    versionMarker, clock.UtcNow.ToUnixTimeSeconds());
            }

            _importChecked = true;
            return db;
        }
        finally
        {
            _importLock.Release();
        }
    }

    private async Task ImportAsync(SQLite.SQLiteAsyncConnection db, CancellationToken cancellationToken)
    {
        await using var stream = await sourceJsonStreamFactory(cancellationToken);
        var surahs = await JsonSerializer.DeserializeAsync<List<SourceSurah>>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Quran source JSON could not be read.");

        var surahRows = new List<Entities.SurahRow>(surahs.Count);
        var verseRows = new List<Entities.VerseRow>(6300);
        var globalIndex = 0;
        var pageIndex = 0; // 0-based into QuranMetadata.PageStarts
        var juzIndex = 0;  // 0-based into QuranMetadata.JuzStarts

        foreach (var surah in surahs)
        {
            surahRows.Add(new Entities.SurahRow
            {
                Number = surah.Id,
                Name = surah.Transliteration,
                TranslatedName = surah.Translation,
                VerseCount = surah.Verses.Count
            });

            foreach (var verse in surah.Verses)
            {
                // Boundaries are ordered by position in the mushaf; advance the cursor
                // whenever this verse starts the next page/juz.
                while (pageIndex + 1 < QuranMetadata.PageStarts.Length &&
                       QuranMetadata.PageStarts[pageIndex + 1] == (surah.Id, verse.Id))
                    pageIndex++;
                while (juzIndex + 1 < QuranMetadata.JuzStarts.Length &&
                       QuranMetadata.JuzStarts[juzIndex + 1] == (surah.Id, verse.Id))
                    juzIndex++;

                verseRows.Add(new Entities.VerseRow
                {
                    SurahNumber = surah.Id,
                    VerseNumber = verse.Id,
                    GlobalIndex = globalIndex++,
                    Text = verse.Translation?.Trim() ?? string.Empty,
                    ArabicText = verse.Text?.Trim() ?? string.Empty,
                    Page = pageIndex + 1,
                    Juz = juzIndex + 1
                });
            }
        }

        await db.RunInTransactionAsync(sync =>
        {
            sync.Execute("DELETE FROM verses");
            sync.Execute("DELETE FROM surahs");
            sync.InsertAll(surahRows, runInTransaction: false);
            sync.InsertAll(verseRows, runInTransaction: false);
        });
    }

    private sealed class JuzRange
    {
        public int Juz { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
    }

    private sealed class VerseWithSurah
    {
        public int SurahNumber { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ArabicText { get; set; } = string.Empty;
        public int Page { get; set; }
        public int Juz { get; set; }
        public string SurahName { get; set; } = string.Empty;

        public Verse ToVerse() => new(SurahNumber, SurahName, VerseNumber, Text, ArabicText, Page, Juz);
    }

    private sealed record SourceSurah(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("transliteration")] string Transliteration,
        [property: JsonPropertyName("translation")] string Translation,
        [property: JsonPropertyName("verses")] List<SourceVerse> Verses);

    private sealed record SourceVerse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("translation")] string? Translation);
}
