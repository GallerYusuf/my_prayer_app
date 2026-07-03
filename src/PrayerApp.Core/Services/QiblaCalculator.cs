namespace PrayerApp.Core.Services;

/// <summary>Great-circle bearing and distance from a coordinate to the Kaaba.</summary>
public static class QiblaCalculator
{
    public const double KaabaLatitude = 21.4225;
    public const double KaabaLongitude = 39.8262;

    /// <summary>Initial great-circle bearing in degrees (0..360, clockwise from true north).</summary>
    public static double CalculateBearing(double latitude, double longitude)
    {
        var lat1 = ToRadians(latitude);
        var lat2 = ToRadians(KaabaLatitude);
        var deltaLon = ToRadians(KaabaLongitude - longitude);

        var y = Math.Sin(deltaLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);
        return (ToDegrees(Math.Atan2(y, x)) + 360) % 360;
    }

    /// <summary>Haversine distance to the Kaaba in kilometers.</summary>
    public static double CalculateDistanceKm(double latitude, double longitude)
    {
        const double earthRadiusKm = 6371;
        var dLat = ToRadians(KaabaLatitude - latitude);
        var dLon = ToRadians(KaabaLongitude - longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(latitude)) * Math.Cos(ToRadians(KaabaLatitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    private static double ToDegrees(double radians) => radians * 180 / Math.PI;
}
