namespace PrayerApp.Core.Domain;

/// <summary>Per-prayer manual minute corrections chosen by the user (range clamped to ±60).</summary>
public sealed record PrayerAdjustments
{
    public const int MinMinutes = -60;
    public const int MaxMinutes = 60;

    public int Fajr { get; init; }
    public int Sunrise { get; init; }
    public int Dhuhr { get; init; }
    public int Asr { get; init; }
    public int Maghrib { get; init; }
    public int Isha { get; init; }

    public static PrayerAdjustments None { get; } = new();

    public bool IsZero => Fajr == 0 && Sunrise == 0 && Dhuhr == 0 && Asr == 0 && Maghrib == 0 && Isha == 0;

    public int GetMinutes(PrayerKind kind) => kind switch
    {
        PrayerKind.Fajr => Fajr,
        PrayerKind.Sunrise => Sunrise,
        PrayerKind.Dhuhr => Dhuhr,
        PrayerKind.Asr => Asr,
        PrayerKind.Maghrib => Maghrib,
        PrayerKind.Isha => Isha,
        _ => 0
    };

    public PrayerAdjustments WithMinutes(PrayerKind kind, int minutes)
    {
        var clamped = Math.Clamp(minutes, MinMinutes, MaxMinutes);
        return kind switch
        {
            PrayerKind.Fajr => this with { Fajr = clamped },
            PrayerKind.Sunrise => this with { Sunrise = clamped },
            PrayerKind.Dhuhr => this with { Dhuhr = clamped },
            PrayerKind.Asr => this with { Asr = clamped },
            PrayerKind.Maghrib => this with { Maghrib = clamped },
            PrayerKind.Isha => this with { Isha = clamped },
            _ => this
        };
    }

    /// <summary>Normalizes possibly-corrupt persisted values back into the valid range.</summary>
    public PrayerAdjustments Sanitized() => new()
    {
        Fajr = Math.Clamp(Fajr, MinMinutes, MaxMinutes),
        Sunrise = Math.Clamp(Sunrise, MinMinutes, MaxMinutes),
        Dhuhr = Math.Clamp(Dhuhr, MinMinutes, MaxMinutes),
        Asr = Math.Clamp(Asr, MinMinutes, MaxMinutes),
        Maghrib = Math.Clamp(Maghrib, MinMinutes, MaxMinutes),
        Isha = Math.Clamp(Isha, MinMinutes, MaxMinutes)
    };
}
