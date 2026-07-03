using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Contracts;

/// <summary>Read access to the local Quran translation plus user favorites.</summary>
public interface IQuranRepository
{
    Task<IReadOnlyList<SurahInfo>> GetSurahsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Verse>> GetVersesAsync(int surahNumber, CancellationToken cancellationToken = default);

    Task<Verse?> GetVerseAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Verse>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);

    Task<int> GetTotalVerseCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Verse at a global 0-based index (used for deterministic verse-of-day).</summary>
    Task<Verse?> GetVerseByGlobalIndexAsync(int globalIndex, CancellationToken cancellationToken = default);

    // Favorites
    Task<bool> IsFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default);
    Task ToggleFavoriteAsync(int surahNumber, int verseNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Verse>> GetFavoritesAsync(CancellationToken cancellationToken = default);
}

public sealed record SurahInfo(int Number, string Name, string TranslatedName, int VerseCount);

public sealed record Verse(int SurahNumber, string SurahName, int VerseNumber, string Text)
{
    public string Reference => $"{SurahName} Suresi, {VerseNumber}. Ayet";
}
