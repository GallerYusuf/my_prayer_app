namespace PrayerApp.Core.Domain;

public sealed class QuranTopic
{
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required (int Surah, int Verse)[] Verses { get; init; }
}
