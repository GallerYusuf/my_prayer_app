namespace PrayerApp.Core.Services;

/// <summary>
/// Deterministic verse-of-day selection: the same date always maps to the same verse,
/// so the verse stays fixed throughout the day and works offline.
/// </summary>
public static class VerseOfDaySelector
{
    /// <summary>Maps a date (plus optional slot for multiple daily notifications) to a global verse index.</summary>
    public static int GetGlobalIndex(DateOnly date, int totalVerseCount, int slot = 0)
    {
        if (totalVerseCount <= 0)
            return 0;

        // SplitMix64-style scramble of the day number for a uniform, deterministic pick.
        var z = (ulong)(date.DayNumber * 6364136223846793005L + slot * 1442695040888963407L);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;

        return (int)(z % (ulong)totalVerseCount);
    }
}
