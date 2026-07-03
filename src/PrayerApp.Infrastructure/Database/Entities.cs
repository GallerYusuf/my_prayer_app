using SQLite;

namespace PrayerApp.Infrastructure.Database;

internal static class Entities
{
    [Table("prayer_days")]
    public sealed class PrayerDayRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>Location cache key ("lat:lon" rounded).</summary>
        [Indexed(Name = "ix_prayer_lookup", Order = 1)]
        public string LocationKey { get; set; } = string.Empty;

        [Indexed(Name = "ix_prayer_lookup", Order = 2)]
        public int Method { get; set; }

        /// <summary>DateOnly.DayNumber for fast range queries.</summary>
        [Indexed(Name = "ix_prayer_lookup", Order = 3)]
        public int DayNumber { get; set; }

        // Times stored as minutes since midnight.
        public int FajrMinutes { get; set; }
        public int SunriseMinutes { get; set; }
        public int DhuhrMinutes { get; set; }
        public int AsrMinutes { get; set; }
        public int MaghribMinutes { get; set; }
        public int IshaMinutes { get; set; }

        public string HijriDateText { get; set; } = string.Empty;
        public int HijriMonth { get; set; }
        public int HijriDay { get; set; }
        public int HijriYear { get; set; }
    }

    [Table("cache_meta")]
    public sealed class CacheMetaRow
    {
        [PrimaryKey]
        public string Key { get; set; } = string.Empty;

        public long LastFetchUnixSeconds { get; set; }
    }

    [Table("surahs")]
    public sealed class SurahRow
    {
        [PrimaryKey]
        public int Number { get; set; }

        public string Name { get; set; } = string.Empty;
        public string TranslatedName { get; set; } = string.Empty;
        public int VerseCount { get; set; }
    }

    [Table("verses")]
    public sealed class VerseRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "ix_verse_lookup", Order = 1)]
        public int SurahNumber { get; set; }

        [Indexed(Name = "ix_verse_lookup", Order = 2)]
        public int VerseNumber { get; set; }

        /// <summary>0-based global order across the whole book.</summary>
        [Indexed]
        public int GlobalIndex { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    [Table("favorite_verses")]
    public sealed class FavoriteVerseRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "ix_fav_lookup", Order = 1)]
        public int SurahNumber { get; set; }

        [Indexed(Name = "ix_fav_lookup", Order = 2)]
        public int VerseNumber { get; set; }

        public long AddedUnixSeconds { get; set; }
    }

    [Table("dhikr_sessions")]
    public sealed class DhikrSessionRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int DayNumber { get; set; }

        public string DhikrName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TargetCount { get; set; }
    }
}
