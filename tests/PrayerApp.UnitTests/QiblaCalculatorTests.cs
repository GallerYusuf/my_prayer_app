using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class QiblaCalculatorTests
{
    [Theory]
    // Reference bearings computed from the standard great-circle formula.
    [InlineData(41.0082, 28.9784, 151.6, 1.5)]  // İstanbul ≈ 151-152°
    [InlineData(39.9334, 32.8597, 160.2, 1.5)]  // Ankara ≈ 160°
    [InlineData(38.5012, 43.3730, 191.1, 1.5)]  // Van ≈ 191°
    [InlineData(21.4225, 39.8262, 0.0, 360.0)]  // Kaaba itself — any bearing acceptable
    public void CalculateBearing_MatchesReferenceValues(double lat, double lon, double expected, double tolerance)
    {
        var bearing = QiblaCalculator.CalculateBearing(lat, lon);

        Assert.InRange(bearing, 0, 360);
        if (tolerance < 360)
        {
            var diff = Math.Abs(bearing - expected);
            if (diff > 180)
                diff = 360 - diff;
            Assert.True(diff <= tolerance, $"Bearing {bearing:F1} differs from expected {expected:F1} by {diff:F1}");
        }
    }

    [Fact]
    public void CalculateDistanceKm_IstanbulToMecca_IsAbout2400Km()
    {
        var distance = QiblaCalculator.CalculateDistanceKm(41.0082, 28.9784);
        Assert.InRange(distance, 2350, 2500);
    }

    [Fact]
    public void CalculateDistanceKm_AtKaaba_IsZero()
    {
        var distance = QiblaCalculator.CalculateDistanceKm(
            QiblaCalculator.KaabaLatitude, QiblaCalculator.KaabaLongitude);
        Assert.True(distance < 0.001);
    }

    [Fact]
    public void CalculateBearing_NorthernHemisphereWestOfMecca_PointsEastSouth()
    {
        // Anywhere in Turkey the qibla is roughly south-southeast (140°..200°).
        var bearing = QiblaCalculator.CalculateBearing(37.0, 35.3); // Adana
        Assert.InRange(bearing, 140, 200);
    }
}
