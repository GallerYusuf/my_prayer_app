using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Data;
using PrayerApp.Views;

namespace PrayerApp.ViewModels;

public partial class QiblaViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsStore _settingsStore;
    private bool _compassActive;
    private bool _wasAligned;

    [ObservableProperty] private double _qiblaBearing;
    [ObservableProperty] private double _compassHeading;
    [ObservableProperty] private double _arrowRotation;
    [ObservableProperty] private string _cityDisplay = "—";
    [ObservableProperty] private string _distanceText = "—";
    [ObservableProperty] private string _bearingText = "—";
    [ObservableProperty] private string _headingText = "—";
    [ObservableProperty] private string _alignmentText = "Telefonu yatay tutun";
    [ObservableProperty] private string _sensorStatusText = "Sensör kontrol ediliyor...";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasCompass = true;
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private bool _isAligned;
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
        CityDisplay = location?.DisplayName ?? "Konum seçilmedi";

        if (location is null || !location.IsValid)
        {
            HasLocation = false;
            SensorStatusText = "Konum bekleniyor";
            AlignmentText = "Önce şehir seçin";
            StatusMessage = "Kıble açısını hesaplamak için konum bilgisi gerekiyor. Aşağıdan şehir seçebilirsiniz.";
            return;
        }

        HasLocation = true;
        StatusMessage = string.Empty;
        QiblaBearing = QiblaCalculator.CalculateBearing(location.Latitude, location.Longitude);
        DistanceText = $"{QiblaCalculator.CalculateDistanceKm(location.Latitude, location.Longitude):N0} km";
        BearingText = $"{QiblaBearing:F0}°";
        UpdateArrow();

        if (_compassActive)
            return;

        if (!Compass.Default.IsSupported)
        {
            HasCompass = false;
            SensorStatusText = "Manyetik sensör yok · Sabit açı modu";
            HeadingText = "Sensör yok";
            AlignmentText = $"Kuzeyden {QiblaBearing:F0}° saat yönünde";
            StatusMessage = $"Bu cihazda pusula sensörü bulunmuyor. {CityDisplay} için kıble açısı kuzeyden {QiblaBearing:F0}° yönündedir.";
            return;
        }

        HasCompass = true;
        SensorStatusText = "Canlı pusula sensörü aktif";
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

    [RelayCommand]
    private async Task ChangeCityAsync()
    {
        var navigation = Shell.Current?.Navigation;
        if (navigation is null)
            return;

        var current = _settingsStore.Get();
        var pickerVm = new CityPickerViewModel(current.CityPlateCode, current.DistrictApiName);
        pickerVm.SetNavigation(navigation);
        var page = new CityPickerPage(pickerVm);
        var modal = new NavigationPage(page)
        {
            BarBackgroundColor = Colors.Transparent,
            BackgroundColor = Colors.Transparent
        };
        NavigationPage.SetHasNavigationBar(page, false);

        await navigation.PushModalAsync(modal);

        var selected = await pickerVm.WaitForSelectionAsync();
        if (selected is null || CityCoordinates.FindByPlate(selected.City.PlateCode) is not { } coords)
            return;

        var updated = current with
        {
            Location = new GeoLocation
            {
                Latitude = coords.Lat,
                Longitude = coords.Lon,
                DisplayName = selected.Display,
                IsFromDevice = false
            },
            LocationMode = LocationMode.Manual,
            CityPlateCode = selected.City.PlateCode,
            DistrictApiName = selected.District?.ApiName ?? string.Empty
        };

        _settingsStore.Save(updated);
        Start();
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CompassHeading = e.Reading.HeadingMagneticNorth;
            HeadingText = $"{CompassHeading:F0}°";
            UpdateArrow();
        });
    }

    private void UpdateArrow()
    {
        ArrowRotation = (QiblaBearing - CompassHeading + 360) % 360;

        if (!HasCompass || !HasLocation)
            return;

        // Signed shortest angular difference (-180..+180)
        var diff = (QiblaBearing - CompassHeading + 540) % 360 - 180;
        var absDiff = Math.Abs(diff);

        if (absDiff <= 5)
        {
            IsAligned = true;
            AlignmentText = "✓ Kıble yönüyle hizalandınız";
            if (!_wasAligned)
            {
                _wasAligned = true;
                try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(45)); } catch { }
            }
        }
        else
        {
            IsAligned = false;
            _wasAligned = false;
            AlignmentText = diff > 0
                ? $"Sağa doğru {absDiff:F0}° dönün"
                : $"Sola doğru {absDiff:F0}° dönün";
        }
    }

    public void Dispose() => Stop();
}
