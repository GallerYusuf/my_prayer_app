namespace PrayerApp.Core.Domain;

/// <summary>Prayer times for a single calendar day at a specific location.</summary>
public sealed record PrayerDay
{
    public required DateOnly Date { get; init; }

    /// <summary>Raw (unadjusted) times as reported by the provider, keyed by prayer.</summary>
    public required IReadOnlyDictionary<PrayerKind, TimeOnly> Times { get; init; }

    public string HijriDateText { get; init; } = string.Empty;
    public int HijriMonth { get; init; }
    public int HijriDay { get; init; }
    public int HijriYear { get; init; }

    public TimeOnly this[PrayerKind kind] => Times[kind];

    public bool IsComplete => PrayerKindExtensions.All.All(Times.ContainsKey);

    /// <summary>Returns a copy with per-prayer minute adjustments applied.</summary>
    public PrayerDay WithAdjustments(PrayerAdjustments adjustments)
    {
        if (adjustments.IsZero)
            return this;

        var adjusted = new Dictionary<PrayerKind, TimeOnly>(Times.Count);
        foreach (var (kind, time) in Times)
        {
            var minutes = adjustments.GetMinutes(kind);
            adjusted[kind] = minutes == 0 ? time : time.AddMinutes(minutes);
        }

        return this with { Times = adjusted };
    }
}
