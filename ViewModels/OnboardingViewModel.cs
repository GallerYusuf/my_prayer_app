using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Data;
using PrayerApp.Models;
using PrayerApp.Services;

namespace PrayerApp.ViewModels;

/// <summary>
/// Streamlined, fast onboarding:
/// Step 0: Prominent manual city selection (with popular cities + search) or optional GPS.
/// Step 1: Theme and Contextual Notification permission (user can continue even if permission is declined).
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private static readonly int[] PopularPlateCodes = [34, 6, 35, 16, 42, 7, 1, 27];

    private readonly ISettingsStore _settingsStore;
    private readonly ILocationService _locationService;
    private readonly INotificationScheduler _notificationScheduler;
    private readonly INotificationCoordinator _notificationCoordinator;

    private GeoLocation? _selectedLocation;
    private int _selectedPlateCode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep0), nameof(IsStep1), nameof(IsStep2), nameof(IsStep3),
        nameof(IsLastStep), nameof(IsNotFirstStep), nameof(NextButtonText),
        nameof(Indicator2Opacity), nameof(Indicator3Opacity), nameof(Indicator4Opacity))]
    private int _step;

    public bool IsStep0 => Step == 0;
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool IsLastStep => Step == LastStep;
    public bool IsNotFirstStep => Step > 0;
    public string NextButtonText => IsLastStep ? "Uygulamaya Başla" : "Devam Et";
    public double Indicator2Opacity => Step >= 1 ? 1 : 0.25;
    public double Indicator3Opacity => Step >= 1 ? 1 : 0.25;
    public double Indicator4Opacity => Step >= 1 ? 1 : 0.25;

    [ObservableProperty] private string _locationSummary = "Henüz şehir seçilmedi";
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private bool _isLocating;
    [ObservableProperty] private string _locationError = string.Empty;
    [ObservableProperty] private int _themeIndex; // 0 System, 1 Light, 2 Dark
    [ObservableProperty] private bool _notificationsEnabled = true;
    [ObservableProperty] private bool _isFinishing;
    [ObservableProperty] private string _citySearch = string.Empty;

    public ObservableCollection<TurkishCity> PopularCities { get; } = [];
    public ObservableCollection<TurkishCity> FilteredCities { get; } = [];

    public const int LastStep = 1;

    public OnboardingViewModel(
        ISettingsStore settingsStore,
        ILocationService locationService,
        INotificationScheduler notificationScheduler,
        INotificationCoordinator notificationCoordinator)
    {
        _settingsStore = settingsStore;
        _locationService = locationService;
        _notificationScheduler = notificationScheduler;
        _notificationCoordinator = notificationCoordinator;

        var all = TurkishCities.GetAll();
        foreach (var plate in PopularPlateCodes)
        {
            if (all.FirstOrDefault(c => c.PlateCode == plate) is { } city)
                PopularCities.Add(city);
        }

        RefreshCityList();
    }

    partial void OnCitySearchChanged(string value) => RefreshCityList();

    partial void OnThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            1 => ThemePreference.Light,
            2 => ThemePreference.Dark,
            _ => ThemePreference.System
        };
        ThemeManager.Apply(theme);
    }

    private void RefreshCityList()
    {
        FilteredCities.Clear();
        foreach (var city in TurkishCities.GetAll().Where(c => TurkishCities.MatchesSearch(c, CitySearch)).Take(81))
            FilteredCities.Add(city);
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (Step == 0)
        {
            if (_selectedLocation is null)
            {
                LocationError = "Lütfen listeden şehrinizi seçin veya GPS konumunu kullanın.";
                return;
            }
            Step = 1;
            return;
        }

        await FinishAsync();
    }

    [RelayCommand]
    private void Back()
    {
        if (Step > 0)
            Step--;
    }

    [RelayCommand]
    private async Task UseDeviceLocationAsync()
    {
        if (IsLocating)
            return;

        try
        {
            IsLocating = true;
            LocationError = string.Empty;

            var result = await _locationService.GetCurrentLocationAsync();
            if (!result.IsSuccess)
            {
                LocationError = "Cihaz konumu alınamadı. İzin vermeden de aşağıdaki listeden şehrinizi seçerek devam edebilirsiniz.";
                return;
            }

            _selectedLocation = result.Value;
            _selectedPlateCode = 0;
            HasLocation = true;
            LocationSummary = result.Value.DisplayName;
        }
        finally
        {
            IsLocating = false;
        }
    }

    [RelayCommand]
    private void SelectCity(TurkishCity city)
    {
        if (CityCoordinates.FindByPlate(city.PlateCode) is not { } coords)
            return;

        _selectedLocation = new GeoLocation
        {
            Latitude = coords.Lat,
            Longitude = coords.Lon,
            DisplayName = city.DisplayName,
            IsFromDevice = false
        };
        _selectedPlateCode = city.PlateCode;
        HasLocation = true;
        LocationError = string.Empty;
        LocationSummary = city.DisplayName;
    }

    [RelayCommand]
    private async Task FinishWithoutNotificationsAsync()
    {
        NotificationsEnabled = false;
        await FinishAsync();
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (_selectedLocation is null)
        {
            Step = 0;
            LocationError = "Devam etmek için bir şehir seçin.";
            return;
        }

        if (IsFinishing)
            return;

        try
        {
            IsFinishing = true;

            if (NotificationsEnabled)
            {
                try
                {
                    await _notificationScheduler.RequestPermissionAsync();
                }
                catch
                {
                    // Never block the user if notification permission fails or is denied.
                }
            }

            var theme = ThemeIndex switch
            {
                1 => ThemePreference.Light,
                2 => ThemePreference.Dark,
                _ => ThemePreference.System
            };

            var settings = _settingsStore.Get() with
            {
                HasCompletedOnboarding = true,
                Location = _selectedLocation,
                LocationMode = _selectedLocation.IsFromDevice ? LocationMode.Automatic : LocationMode.Manual,
                CityPlateCode = _selectedPlateCode,
                Theme = theme,
                PrayerNotificationsEnabled = NotificationsEnabled,
                VerseNotificationsEnabled = NotificationsEnabled,
                FridayNotificationsEnabled = NotificationsEnabled
            };

            _settingsStore.Save(settings);
            ThemeManager.Apply(theme);

            _ = Task.Run(() => _notificationCoordinator.RescheduleAsync());

            await Shell.Current.GoToAsync("//MainPage");
        }
        finally
        {
            IsFinishing = false;
        }
    }
}
