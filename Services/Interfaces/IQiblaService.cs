namespace PrayerApp.Services.Interfaces;

public interface IQiblaService
{
    double CalculateBearing(double latitude, double longitude);
    double CalculateDistanceKm(double latitude, double longitude);
}
