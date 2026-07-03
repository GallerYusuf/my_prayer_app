using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class VerseOfDaySelectorTests
{
    [Fact]
    public void SameDay_AlwaysReturnsSameIndex()
    {
        var date = new DateOnly(2026, 7, 3);
        var first = VerseOfDaySelector.GetGlobalIndex(date, 6236);

        for (var i = 0; i < 100; i++)
            Assert.Equal(first, VerseOfDaySelector.GetGlobalIndex(date, 6236));
    }

    [Fact]
    public void DifferentDays_ProduceDifferentIndexes()
    {
        var date = new DateOnly(2026, 7, 3);
        var indexes = Enumerable.Range(0, 30)
            .Select(i => VerseOfDaySelector.GetGlobalIndex(date.AddDays(i), 6236))
            .Distinct()
            .Count();

        // Statistically almost impossible to collide on all; require variety.
        Assert.True(indexes > 25);
    }

    [Fact]
    public void DifferentSlots_ProduceDifferentIndexes()
    {
        var date = new DateOnly(2026, 7, 3);
        var slot0 = VerseOfDaySelector.GetGlobalIndex(date, 6236, 0);
        var slot1 = VerseOfDaySelector.GetGlobalIndex(date, 6236, 1);

        Assert.NotEqual(slot0, slot1);
    }

    [Fact]
    public void Index_AlwaysWithinBounds()
    {
        var date = new DateOnly(2000, 1, 1);
        for (var i = 0; i < 10_000; i += 37)
        {
            var index = VerseOfDaySelector.GetGlobalIndex(date.AddDays(i), 6236);
            Assert.InRange(index, 0, 6235);
        }
    }

    [Fact]
    public void ZeroVerseCount_ReturnsZero()
    {
        Assert.Equal(0, VerseOfDaySelector.GetGlobalIndex(new DateOnly(2026, 7, 3), 0));
    }
}
