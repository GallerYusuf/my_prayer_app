namespace PrayerApp.Core.Domain;

/// <summary>The next prayer occurrence resolved against absolute wall-clock time.</summary>
public sealed record UpcomingPrayer(
    PrayerKind Kind,
    DateOnly Date,
    DateTimeOffset OccursAt,
    TimeSpan Remaining)
{
    public bool IsTomorrow(DateOnly today) => Date > today;
}
