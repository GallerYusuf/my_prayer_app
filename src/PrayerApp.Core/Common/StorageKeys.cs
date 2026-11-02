using System.Globalization;

namespace PrayerApp.Core.Common;

/// <summary>
/// Key-value (Preferences) keys that hold <b>user data</b>. Single source of truth for the
/// stores/view models that write them and for the backup service, so a new piece of user
/// data cannot silently be left out of backups. Device-specific keys (notification ids,
/// plan dates, Quran import markers) are intentionally not listed here.
/// </summary>
public static class StorageKeys
{
    public const string UserSettings = "user_settings_v2";

    // Prayer tracking (one JSON entry per day + streak bookkeeping)
    public const string PrayerTrackDayPrefix = "prayer_track_";
    public const string PrayerTrackStreak = "prayer_track_streak";
    public const string PrayerTrackLastFull = "prayer_track_last_full";
    public const string PrayerTrackFirstDate = "prayer_track_first";

    public const string KazaCounters = "kaza_counters";
    public const string EsmaFavorites = "esma_favorites";

    // Quran reading position and reading preferences
    public const string QuranLastRead = "quran_last_read";
    public const string QuranFontScale = "quran_font_scale";
    public const string QuranArabicFontSize = "quran_arabic_font_size";
    public const string QuranTranslationFontSize = "quran_translation_font_size";
    public const string QuranShowArabic = "quran_show_arabic";
    public const string QuranShowTranslation = "quran_show_translation";
    public const string QuranSurahPositionPrefix = "quran_surah_pos_";
    public const int SurahCount = 114;

    public const string ZakatGoldPrice = "zakat_gold_price";
    public const string ZakatSilverPrice = "zakat_silver_price";

    /// <summary>Fixed (non per-day) user-data keys included in backups.</summary>
    public static readonly IReadOnlyList<string> BackedUpKeys =
    [
        UserSettings,
        PrayerTrackStreak,
        PrayerTrackLastFull,
        PrayerTrackFirstDate,
        KazaCounters,
        EsmaFavorites,
        QuranLastRead,
        QuranFontScale,
        QuranArabicFontSize,
        QuranTranslationFontSize,
        QuranShowArabic,
        QuranShowTranslation,
        ZakatGoldPrice,
        ZakatSilverPrice
    ];

    /// <summary>Daily prayer-tracking key, e.g. <c>prayer_track_2026-11-02</c>.</summary>
    public static string PrayerTrackDay(DateOnly date) =>
        PrayerTrackDayPrefix + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Last visible verse inside a surah, e.g. <c>quran_surah_pos_36</c>.</summary>
    public static string QuranSurahPosition(int surahNumber) =>
        QuranSurahPositionPrefix + surahNumber.ToString(CultureInfo.InvariantCulture);

    /// <summary>True for keys a backup is allowed to restore (fixed, per-surah or daily tracking keys).</summary>
    public static bool IsRestorable(string key)
    {
        if (BackedUpKeys.Contains(key))
            return true;

        if (key.StartsWith(QuranSurahPositionPrefix, StringComparison.Ordinal))
        {
            return int.TryParse(key[QuranSurahPositionPrefix.Length..], NumberStyles.None,
                       CultureInfo.InvariantCulture, out var surah)
                   && surah is >= 1 and <= SurahCount;
        }

        return key.StartsWith(PrayerTrackDayPrefix, StringComparison.Ordinal) &&
               DateOnly.TryParseExact(key[PrayerTrackDayPrefix.Length..], "yyyy-MM-dd",
                   CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
