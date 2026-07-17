using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Contracts;

/// <summary>Read access to the local Quran text (Arabic + translation) plus user favorites.</summary>
public interface IQuranRepository
{
    Task<IReadOnlyList<SurahInfo>> GetSurahsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Verse>> GetVersesAsync(int surahNumber, CancellationToken cancellationToken = default);

    Task<Verse?> GetVerseAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Verse>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);

    Task<int> GetTotalVerseCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Verse at a global 0-based index (used for deterministic verse-of-day).</summary>
    Task<Verse?> GetVerseByGlobalIndexAsync(int globalIndex, CancellationToken cancellationToken = default);

    /// <summary>The 30 juz sections with their mushaf page ranges.</summary>
    Task<IReadOnlyList<JuzInfo>> GetJuzListAsync(CancellationToken cancellationToken = default);

    /// <summary>First verse of a standard Medina mushaf page (1-604).</summary>
    Task<Verse?> GetPageStartAsync(int page, CancellationToken cancellationToken = default);

    // Favorites
    Task<bool> IsFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default);
    Task ToggleFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Verse>> GetFavoritesAsync(CancellationToken cancellationToken = default);
}

public sealed record SurahInfo(int Number, string Name, string TranslatedName, int VerseCount, int NuzulNumber);

/// <summary>
/// A single verse. <paramref name="ArabicText"/>, <paramref name="Page"/> and
/// <paramref name="Juz"/> default to empty/0 for callers that only need the translation.
/// </summary>
public sealed record Verse(
    int SurahNumber,
    string SurahName,
    int VerseNumber,
    string Text,
    string ArabicText = "",
    int Page = 0,
    int Juz = 0)
{
    public string Reference => $"{SurahName} Suresi, {VerseNumber}. Ayet";
}

/// <summary>One of the 30 juz sections of the mushaf.</summary>
public sealed record JuzInfo(
    int Number,
    string StartSurahName,
    int StartSurah,
    int StartVerse,
    int StartPage,
    int EndPage);
