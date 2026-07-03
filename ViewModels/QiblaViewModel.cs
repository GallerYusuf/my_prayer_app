using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Data;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.ViewModels;

public partial class QiblaViewModel : ObservableObject, IDisposable
{
    private readonly IQiblaService _qiblaService;
    private readonly ISettingsService _settingsService;
    private bool _compassActive;

    [ObservableProperty]
    private double _qiblaBearing;

    [ObservableProperty]
    private double _compassHeading;

    [ObservableProperty]
    private double _arrowRotation;

    [ObservableProperty]
    private string _cityDisplay = "—";

    [ObservableProperty]
    private string _distanceText = "—";

    [ObservableProperty]
    private string _bearingText = "—";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isCalibrated = true;

    [ObservableProperty]
    private bool _hasLocation;

    public QiblaViewModel(IQiblaService qiblaService, ISettingsService settingsService)
    {
        _qiblaService = qiblaService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public void Start()
    {
        var settings = _settingsService.GetSettings();
        var city = TurkishCities.FindByApiName(settings.City);
        CityDisplay = settings.HasDistrict
            ? $"{settings.District}, {settings.CityDisplay}"
            : settings.CityDisplay;

        if (settings.Latitude == 0 && settings.Longitude == 0)
        {
            HasLocation = false;
            StatusMessage = "Konum bilgisi yok. Ana sayfadan vakitleri yenileyin.";
            return;
        }

        HasLocation = true;
        StatusMessage = string.Empty;
        QiblaBearing = _qiblaService.CalculateBearing(settings.Latitude, settings.Longitude);
        var distance = _qiblaService.CalculateDistanceKm(settings.Latitude, settings.Longitude);
        DistanceText = $"{distance:F0} km";
        BearingText = $"{QiblaBearing:F0}°";
        UpdateArrow();

        if (_compassActive)
            return;

        if (!Compass.IsSupported)
        {
            StatusMessage = "Bu cihazda pusula sensörü desteklenmiyor.";
            return;
        }

        Compass.ReadingChanged += OnCompassReadingChanged;
        Compass.Start(SensorSpeed.UI);
        _compassActive = true;
    }

    [RelayCommand]
    public void Stop()
    {
        if (!_compassActive)
            return;

        Compass.ReadingChanged -= OnCompassReadingChanged;
        Compass.Stop();
        _compassActive = false;
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CompassHeading = e.Reading.HeadingMagneticNorth;
            IsCalibrated = e.Reading.HeadingMagneticNorth > 0 || Compass.IsSupported;
            UpdateArrow();
        });
    }

    private void UpdateArrow()
    {
        ArrowRotation = (QiblaBearing - CompassHeading + 360) % 360;
    }

    public void Dispose()
    {
        Stop();
    }
}
