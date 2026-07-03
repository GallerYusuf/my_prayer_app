using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Services;

namespace PrayerApp.ViewModels;

public partial class QiblaViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsStore _settingsStore;
    private bool _compassActive;

    [ObservableProperty] private double _qiblaBearing;
    [ObservableProperty] private double _compassHeading;
    [ObservableProperty] private double _arrowRotation;
    [ObservableProperty] private string _cityDisplay = "—";
    [ObservableProperty] private string _distanceText = "—";
    [ObservableProperty] private string _bearingText = "—";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasCompass = true;
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private bool _isLowAccuracy;

    public QiblaViewModel(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    [RelayCommand]
    public void Start()
    {
        var settings = _settingsStore.Get();
        var location = settings.Location;
        CityDisplay = location?.DisplayName ?? "—";

        if (location is null || !location.IsValid)
        {
            HasLocation = false;
            StatusMessage = "Konum bilgisi yok. Ayarlardan şehir seçin veya konumu açın.";
            return;
        }

        HasLocation = true;
        StatusMessage = string.Empty;
        QiblaBearing = QiblaCalculator.CalculateBearing(location.Latitude, location.Longitude);
        DistanceText = $"{QiblaCalculator.CalculateDistanceKm(location.Latitude, location.Longitude):F0} km";
        BearingText = $"{QiblaBearing:F0}°";
        UpdateArrow();

        if (_compassActive)
            return;

        if (!Compass.Default.IsSupported)
        {
            HasCompass = false;
            StatusMessage = "Bu cihazda pusula sensörü yok. Kıble yönü kuzeye göre "
                + $"{QiblaBearing:F0}° olarak hesaplandı; yönünüzü harita uygulamasıyla doğrulayabilirsiniz.";
            return;
        }

        Compass.Default.ReadingChanged += OnCompassReadingChanged;
        Compass.Default.Start(SensorSpeed.UI, applyLowPassFilter: true);
        _compassActive = true;
    }

    [RelayCommand]
    public void Stop()
    {
        if (!_compassActive)
            return;

        Compass.Default.ReadingChanged -= OnCompassReadingChanged;
        Compass.Default.Stop();
        _compassActive = false;
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CompassHeading = e.Reading.HeadingMagneticNorth;
            UpdateArrow();
        });
    }

    private void UpdateArrow() => ArrowRotation = (QiblaBearing - CompassHeading + 360) % 360;

    public void Dispose() => Stop();
}
