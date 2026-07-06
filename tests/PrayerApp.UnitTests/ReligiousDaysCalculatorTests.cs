using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class ReligiousDaysCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 6);

    [Fact]
    public void GetUpcoming_CoversTwelveMonthsInOrder()
    {
        var days = ReligiousDaysCalculator.GetUpcoming(Today, 12);

        Assert.NotEmpty(days);
        Assert.All(days, d => Assert.InRange(d.Date, Today, Today.AddMonths(12)));
        Assert.Equal(days.OrderBy(d => d.Date), days);
    }

    [Fact]
    public void GetUpcoming_TwelveMonths_IncludesCoreEvents()
    {
        var days = ReligiousDaysCalculator.GetUpcoming(Today, 12);
        var names = days.Select(d => d.Name).ToHashSet();

        Assert.Contains("Ramazan Başlangıcı", names);
        Assert.Contains("Kadir Gecesi", names);
        Assert.Contains("Mevlid Kandili", names);
        Assert.Contains("Regaib Kandili", names);
        Assert.Contains("Miraç Kandili", names);
        Assert.Contains("Berat Kandili", names);
        Assert.Contains(days, d => d.IsBayram);
    }

    [Fact]
    public void RegaibKandili_FallsOnThursday()
    {
        var days = ReligiousDaysCalculator.GetUpcoming(Today, 13);
        var regaib = days.First(d => d.Name == "Regaib Kandili");

        Assert.Equal(DayOfWeek.Thursday, regaib.Date.DayOfWeek);
    }

    [Fact]
    public void HijriConversion_RoundTripsForKnownDate()
    {
        var (year, month, day) = ReligiousDaysCalculator.ToHijri(Today);
        var back = ReligiousDaysCalculator.ToGregorian(year, month, day);

        Assert.True(year > 1400);
        Assert.InRange(month, 1, 12);
        Assert.InRange(day, 1, 30);
        Assert.Equal(Today, back);
    }

    [Fact]
    public void HijriMonthNames_AllTwelveDefined()
    {
        for (var month = 1; month <= 12; month++)
            Assert.False(string.IsNullOrEmpty(ReligiousDaysCalculator.HijriMonthName(month)));
    }

    [Fact]
    public void ToGregorian_OutOfRange_ReturnsNull()
    {
        Assert.Null(ReligiousDaysCalculator.ToGregorian(9999, 1, 1));
    }
}
