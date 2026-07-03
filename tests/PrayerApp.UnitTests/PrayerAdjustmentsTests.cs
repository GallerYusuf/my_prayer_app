using PrayerApp.Core.Domain;
using Xunit;

namespace PrayerApp.UnitTests;

public class PrayerAdjustmentsTests
{
    private static readonly DateOnly Date = new(2026, 7, 3);

    [Fact]
    public void WithAdjustments_ShiftsTimes()
    {
        var day = TestData.Day(Date, fajr: "04:30", isha: "21:15");
        var adjustments = new PrayerAdjustments { Fajr = -5, Isha = 10 };

        var adjusted = day.WithAdjustments(adjustments);

        Assert.Equal(new TimeOnly(4, 25), adjusted[PrayerKind.Fajr]);
        Assert.Equal(new TimeOnly(21, 25), adjusted[PrayerKind.Isha]);
        Assert.Equal(new TimeOnly(12, 45), adjusted[PrayerKind.Dhuhr]);
    }

    [Fact]
    public void WithAdjustments_Zero_ReturnsSameInstance()
    {
        var day = TestData.Day(Date);
        Assert.Same(day, day.WithAdjustments(PrayerAdjustments.None));
    }

    [Fact]
    public void WithMinutes_ClampsToValidRange()
    {
        var adjustments = PrayerAdjustments.None
            .WithMinutes(PrayerKind.Fajr, 500)
            .WithMinutes(PrayerKind.Isha, -500);

        Assert.Equal(PrayerAdjustments.MaxMinutes, adjustments.Fajr);
        Assert.Equal(PrayerAdjustments.MinMinutes, adjustments.Isha);
    }

    [Fact]
    public void Sanitized_RepairsCorruptValues()
    {
        var corrupt = new PrayerAdjustments { Dhuhr = 9999, Asr = -9999 };
        var sanitized = corrupt.Sanitized();

        Assert.Equal(PrayerAdjustments.MaxMinutes, sanitized.Dhuhr);
        Assert.Equal(PrayerAdjustments.MinMinutes, sanitized.Asr);
    }

    [Fact]
    public void UserSettings_Sanitized_RepairsOutOfRangeValues()
    {
        var settings = new UserSettings
        {
            MinutesBeforePrayer = 999,
            FontScale = 5.0,
            Adjustments = new PrayerAdjustments { Fajr = 1000 }
        };

        var sanitized = settings.Sanitized();

        Assert.Equal(120, sanitized.MinutesBeforePrayer);
        Assert.Equal(1.4, sanitized.FontScale);
        Assert.Equal(PrayerAdjustments.MaxMinutes, sanitized.Adjustments.Fajr);
    }
}
