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

        /// <summary>Original Arabic text (Uthmani script from the bundled source).</summary>
        public string ArabicText { get; set; } = string.Empty;

        /// <summary>Standard Medina mushaf page (1-604).</summary>
        [Indexed]
        public int Page { get; set; }

        /// <summary>Juz number (1-30).</summary>
        [Indexed]
        public int Juz { get; set; }
    }

    [Table("khatm_plans")]
    public sealed class KhatmPlanRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>DateOnly.DayNumber of the plan start.</summary>
        public int StartDayNumber { get; set; }

        /// <summary>Planned duration in days (0 = no deadline).</summary>
        public int TargetDays { get; set; }

        /// <summary>Last mushaf page marked as read (0-604).</summary>
        public int LastReadPage { get; set; }

        /// <summary>DayNumber of completion; 0 while in progress.</summary>
        public int CompletedDayNumber { get; set; }
    }

    [Table("khatm_log")]
    public sealed class KhatmLogRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "ix_khatm_log", Order = 1)]
        public int PlanId { get; set; }

        [Indexed(Name = "ix_khatm_log", Order = 2)]
        public int DayNumber { get; set; }

        /// <summary>Highest page reached at the end of this day.</summary>
        public int EndPage { get; set; }
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
