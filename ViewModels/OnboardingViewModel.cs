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
/// Four-step onboarding: welcome → location → calculation/theme → notifications.
/// Permissions are requested contextually on their own step, never up-front.
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ILocationService _locationService;
    private readonly INotificationScheduler _notificationScheduler;
    private readonly INotificationCoordinator _notificationCoordinator;

    private GeoLocation? _selectedLocation;

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
    public string NextButtonText => IsLastStep ? "Başla" : "Devam";
    public double Indicator2Opacity => Step >= 1 ? 1 : 0.25;
    public double Indicator3Opacity => Step >= 2 ? 1 : 0.25;
    public double Indicator4Opacity => Step >= 3 ? 1 : 0.25;
    [ObservableProperty] private string _locationSummary = "Henüz konum seçilmedi";
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private bool _isLocating;
    [ObservableProperty] private string _locationError = string.Empty;
    [ObservableProperty] private int _themeIndex; // 0 System, 1 Light, 2 Dark
    [ObservableProperty] private bool _notificationsEnabled = true;
    [ObservableProperty] private bool _isFinishing;
    [ObservableProperty] private string _citySearch = string.Empty;

    public ObservableCollection<TurkishCity> FilteredCities { get; } = [];

    public const int LastStep = 3;

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
        RefreshCityList();
    }

    partial void OnCitySearchChanged(string value) => RefreshCityList();

    private void RefreshCityList()
    {
        FilteredCities.Clear();
        foreach (var city in TurkishCities.GetAll().Where(c => TurkishCities.MatchesSearch(c, CitySearch)).Take(81))
            FilteredCities.Add(city);
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (Step < LastStep)
        {
            Step++;
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
                LocationError = result.Error.Message;
                return;
            }

            _selectedLocation = result.Value;
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
        HasLocation = true;
        LocationError = string.Empty;
        LocationSummary = city.DisplayName;
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (_selectedLocation is null)
        {
            Step = 1;
            LocationError = "Devam etmek için bir konum seçin.";
            return;
        }

        if (IsFinishing)
            return;

        try
        {
            IsFinishing = true;

            if (NotificationsEnabled)
                await _notificationScheduler.RequestPermissionAsync();

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
