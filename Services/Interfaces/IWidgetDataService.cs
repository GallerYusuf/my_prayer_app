namespace PrayerApp.Services.Interfaces;

public interface IWidgetDataService
{
    void Update(string cityDisplay, string nextPrayerName, string nextPrayerTime, string remainingTime);
}
