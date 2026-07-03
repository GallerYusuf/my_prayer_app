namespace PrayerApp.Models;

public record RamadanInfo
{
    public int RamadanDay { get; init; }
    public string IftarTime { get; init; } = string.Empty;
    public string SahurTime { get; init; } = string.Empty;
    public string NextEventLabel { get; init; } = string.Empty;
    public string CountdownText { get; init; } = string.Empty;
    public bool IsIftarNext { get; init; }
}
