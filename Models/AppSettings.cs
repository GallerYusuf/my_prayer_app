using System.Text.Json.Serialization;

namespace PrayerApp.Models;

public class AppSettings
{
    public string City { get; set; } = "Van";
    public string CityDisplay { get; set; } = "Van";
    public string Region { get; set; } = "Doğu Anadolu";
    public int PlateCode { get; set; } = 65;

    public string District { get; set; } = string.Empty;
    public string DistrictApiName { get; set; } = string.Empty;

    public string Country { get; set; } = "Turkey";
    public bool HasCompletedOnboarding { get; set; }

    [JsonIgnore]
    public bool HasDistrict => !string.IsNullOrWhiteSpace(DistrictApiName);

    [JsonIgnore]
    public string LocationQuery => HasDistrict ? $"{DistrictApiName}, {City}" : City;

    [JsonIgnore]
    public string LocationDisplay => HasDistrict ? $"{District}, {CityDisplay}" : CityDisplay;

    public bool PrayerNotificationsEnabled { get; set; } = true;
    public bool NotifyAtPrayerTime { get; set; } = true;
    public int MinutesBeforePrayer { get; set; } = 10;

    public bool FridayNotificationsEnabled { get; set; } = true;
    public TimeSpan FridayNotificationTime { get; set; } = new(11, 30, 0);

    public bool VerseNotificationsEnabled { get; set; } = true;
    public int VerseNotificationCount { get; set; } = 3;
    public List<TimeSpan> VerseNotificationTimes { get; set; } =
    [
        new(10, 0, 0),
        new(14, 30, 0),
        new(20, 0, 0)
    ];

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool RamadanModeEnabled { get; set; } = true;
}
