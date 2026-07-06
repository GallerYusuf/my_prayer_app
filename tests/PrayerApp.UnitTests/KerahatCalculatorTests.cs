using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class KerahatCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 6);

    [Fact]
    public void GetWindows_ReturnsThreeWindows()
    {
        var windows = KerahatCalculator.GetWindows(TestData.Day(Today), TimeSpan.FromHours(3));

        Assert.Equal(3, windows.Count);
        // Sunrise 06:00 → window starts at sunrise.
        Assert.Equal(TestData.At(Today, "06:00"), windows[0].Start);
        Assert.Equal(TestData.At(Today, "06:45"), windows[0].End);
        // Dhuhr 12:45 → window ends at Dhuhr.
        Assert.Equal(TestData.At(Today, "12:00"), windows[1].Start);
        Assert.Equal(TestData.At(Today, "12:45"), windows[1].End);
        // Maghrib 19:45 → window ends at Maghrib.
        Assert.Equal(TestData.At(Today, "19:00"), windows[2].Start);
        Assert.Equal(TestData.At(Today, "19:45"), windows[2].End);
    }

    [Theory]
    [InlineData("06:10", "Güneş doğarken")]
    [InlineData("12:30", "Güneş tepe noktasında")]
    [InlineData("19:20", "Güneş batarken")]
    public void GetCurrent_InsideWindow_ReturnsWindow(string time, string expectedName)
    {
        var current = KerahatCalculator.GetCurrent(TestData.Day(Today), TestData.At(Today, time));

        Assert.NotNull(current);
        Assert.Equal(expectedName, current.Name);
    }

    [Theory]
    [InlineData("05:00")]
    [InlineData("06:45")] // window end is exclusive
    [InlineData("10:00")]
    [InlineData("15:00")]
    [InlineData("21:00")]
    public void GetCurrent_OutsideWindows_ReturnsNull(string time)
    {
        Assert.Null(KerahatCalculator.GetCurrent(TestData.Day(Today), TestData.At(Today, time)));
    }
}
