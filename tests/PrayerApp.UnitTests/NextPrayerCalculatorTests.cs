using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class NextPrayerCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 3);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    [Fact]
    public void FindNext_MorningTime_ReturnsDhuhr()
    {
        var days = new[] { TestData.Day(Today), TestData.Day(Tomorrow) };
        var now = TestData.At(Today, "10:00");

        var next = NextPrayerCalculator.FindNext(days, now);

        Assert.NotNull(next);
        Assert.Equal(PrayerKind.Dhuhr, next.Kind);
        Assert.Equal(Today, next.Date);
        Assert.Equal(TimeSpan.FromMinutes(165), next.Remaining);
    }

    [Fact]
    public void FindNext_AfterIsha_ReturnsTomorrowFajr()
    {
        var days = new[] { TestData.Day(Today), TestData.Day(Tomorrow, fajr: "04:31") };
        var now = TestData.At(Today, "22:00");

        var next = NextPrayerCalculator.FindNext(days, now);

        Assert.NotNull(next);
        Assert.Equal(PrayerKind.Fajr, next.Kind);
        Assert.Equal(Tomorrow, next.Date);
        Assert.True(next.IsTomorrow(Today));
        // 22:00 -> 04:31 next day = 6h31m
        Assert.Equal(new TimeSpan(6, 31, 0), next.Remaining);
    }

    [Fact]
    public void FindNext_AfterMidnightBeforeFajr_ReturnsTodayFajr()
    {
        var days = new[] { TestData.Day(Today), TestData.Day(Tomorrow) };
        var now = TestData.At(Today, "01:30");

        var next = NextPrayerCalculator.FindNext(days, now);

        Assert.NotNull(next);
        Assert.Equal(PrayerKind.Fajr, next.Kind);
        Assert.Equal(Today, next.Date);
        Assert.Equal(TimeSpan.FromHours(3), next.Remaining);
    }

    [Fact]
    public void FindNext_ExactlyAtPrayerTime_MovesToNext()
    {
        var days = new[] { TestData.Day(Today), TestData.Day(Tomorrow) };
        var now = TestData.At(Today, "12:45");

        var next = NextPrayerCalculator.FindNext(days, now);

        Assert.NotNull(next);
        Assert.Equal(PrayerKind.Asr, next.Kind);
    }

    [Fact]
    public void FindNext_NoData_ReturnsNull()
    {
        var next = NextPrayerCalculator.FindNext([], TestData.At(Today, "10:00"));
        Assert.Null(next);
    }

    [Fact]
    public void FindNext_OnlyPastDays_ReturnsNull()
    {
        var days = new[] { TestData.Day(Today.AddDays(-2)), TestData.Day(Today.AddDays(-1)) };
        var next = NextPrayerCalculator.FindNext(days, TestData.At(Today, "10:00"));
        Assert.Null(next);
    }

    [Fact]
    public void FindNext_DifferentUtcOffset_UsesWallClock()
    {
        // Same wall-clock time in a different timezone must give the same prayer.
        var days = new[] { TestData.Day(Today), TestData.Day(Tomorrow) };
        var nowPlus1 = TestData.At(Today, "10:00", offsetHours: 1);

        var next = NextPrayerCalculator.FindNext(days, nowPlus1);

        Assert.NotNull(next);
        Assert.Equal(PrayerKind.Dhuhr, next.Kind);
        Assert.Equal(TimeSpan.FromMinutes(165), next.Remaining);
    }

    [Fact]
    public void GetDayState_MidAfternoon_MarksPassedPrayers()
    {
        var day = TestData.Day(Today);
        var now = TestData.At(Today, "17:00");
        var next = NextPrayerCalculator.FindNext([day], now);

        var (passed, current) = NextPrayerCalculator.GetDayState(day, now, next?.Kind, next?.Date);

        Assert.Equal([PrayerKind.Fajr, PrayerKind.Sunrise, PrayerKind.Dhuhr, PrayerKind.Asr], passed);
        Assert.Equal(PrayerKind.Asr, current);
    }
}
