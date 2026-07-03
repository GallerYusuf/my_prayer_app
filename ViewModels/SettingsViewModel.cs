using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Data;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;
using PrayerApp.Views;

namespace PrayerApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IPrayerTimeService _prayerTimeService;
    private readonly INotificationSchedulerService _notificationService;

    [ObservableProperty]
    private string _city = "Van";

    [ObservableProperty]
    private string _country = "Turkey";

    [ObservableProperty]
    private string _cityDisplay = "Van";

    [ObservableProperty]
    private string _cityRegion = "Doğu Anadolu";

    [ObservableProperty]
    private int _selectedPlateCode = 65;

    [ObservableProperty]
    private string _districtDisplay = string.Empty;

    [ObservableProperty]
    private string _districtApiName = string.Empty;

    [ObservableProperty]
    private string _locationSummary = "Van";

    [ObservableProperty]
    private bool _hasDistrict;

    [ObservableProperty]
    private bool _prayerNotificationsEnabled = true;

    [ObservableProperty]
    private bool _notifyAtPrayerTime = true;

    [ObservableProperty]
    private int _minutesBeforePrayer = 10;

    [ObservableProperty]
    private bool _fridayNotificationsEnabled = true;

    [ObservableProperty]
    private TimeSpan _fridayNotificationTime = new(11, 30, 0);

    [ObservableProperty]
    private bool _verseNotificationsEnabled = true;

    [ObservableProperty]
    private int _verseNotificationCount = 3;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _notificationDiagnostics = "Kontrol ediliyor...";

    [ObservableProperty]
    private bool _isFirstLaunch;

    [ObservableProperty]
    private bool _isAdvancedExpanded;

    [ObservableProperty]
    private bool _isAboutExpanded;

    [ObservableProperty]
    private bool _ramadanModeEnabled = true;

    public int[] MinutesBeforeOptions { get; } = [5, 10, 15, 30];

    public ObservableCollection<VerseTimeItem> VerseTimes { get; } = [];

    public SettingsViewModel(
        ISettingsService settingsService,
        IPrayerTimeService prayerTimeService,
        INotificationSchedulerService notificationService)
    {
        _settingsService = settingsService;
        _prayerTimeService = prayerTimeService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    public void LoadSettings()
    {
        var settings = _settingsService.GetSettings();
        Country = settings.Country;
        IsFirstLaunch = !settings.HasCompletedOnboarding;

        var city = TurkishCities.FindByApiName(settings.City) ?? TurkishCities.GetDefault();
        var district = TurkishCities.FindDistrict(city, settings.DistrictApiName);
        ApplySelection(new CitySelection(city, district));

        PrayerNotificationsEnabled = settings.PrayerNotificationsEnabled;
        NotifyAtPrayerTime = settings.NotifyAtPrayerTime;
        MinutesBeforePrayer = SnapMinutesBefore(settings.MinutesBeforePrayer);
        FridayNotificationsEnabled = settings.FridayNotificationsEnabled;
        FridayNotificationTime = settings.FridayNotificationTime;
        VerseNotificationsEnabled = settings.VerseNotificationsEnabled;
        VerseNotificationCount = settings.VerseNotificationCount;
        RamadanModeEnabled = settings.RamadanModeEnabled;

        VerseTimes.Clear();
        for (var i = 0; i < 5; i++)
        {
            var time = i < settings.VerseNotificationTimes.Count
                ? settings.VerseNotificationTimes[i]
                : new TimeSpan(8 + i * 2, 0, 0);

            VerseTimes.Add(new VerseTimeItem { Index = i + 1, Time = time, IsActive = i < settings.VerseNotificationCount });
        }

        _ = RefreshDiagnosticsAsync();
    }

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        try
        {
            NotificationDiagnostics = await _notificationService.GetDiagnosticStatusAsync();
        }
        catch (Exception ex)
        {
            NotificationDiagnostics = $"Durum okunamadı: {ex.Message}";
        }
    }

    partial void OnVerseNotificationCountChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 5);
        if (VerseNotificationCount != clamped)
        {
            VerseNotificationCount = clamped;
            return;
        }

        foreach (var item in VerseTimes)
            item.IsActive = item.Index <= clamped;
    }

    [RelayCommand]
    private void DecrementVerseCount()
    {
        if (VerseNotificationCount > 1)
            VerseNotificationCount--;
    }

    [RelayCommand]
    private void IncrementVerseCount()
    {
        if (VerseNotificationCount < 5)
            VerseNotificationCount++;
    }

    [RelayCommand]
    private void ToggleAdvanced() => IsAdvancedExpanded = !IsAdvancedExpanded;

    [RelayCommand]
    private void ToggleAbout() => IsAboutExpanded = !IsAboutExpanded;

    [RelayCommand]
    private void SelectMinutesBefore(int minutes) => MinutesBeforePrayer = minutes;

    [RelayCommand]
    private async Task OpenCityPickerAsync()
    {
        try
        {
            var navigation = Shell.Current?.Navigation;
            if (navigation is null)
                return;

            var pickerVm = new CityPickerViewModel(SelectedPlateCode, DistrictApiName);
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
            if (selected is not null)
                ApplySelection(selected);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Şehir seçici açılamadı: {ex.Message}";
        }
    }

    private void ApplySelection(CitySelection selection)
    {
        City = selection.City.ApiName;
        Country = "Turkey";
        CityDisplay = selection.City.DisplayName;
        CityRegion = selection.City.Region;
        SelectedPlateCode = selection.City.PlateCode;

        DistrictDisplay = selection.District?.DisplayName ?? string.Empty;
        DistrictApiName = selection.District?.ApiName ?? string.Empty;
        HasDistrict = selection.District is not null;
        LocationSummary = selection.Display;
    }

    private static int SnapMinutesBefore(int minutes) =>
        minutes switch
        {
            <= 7 => 5,
            <= 12 => 10,
            <= 22 => 15,
            _ => 30
        };

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Application.Current?.MainPage is not null)
        {
            Application.Current.MainPage.Unfocus();
            await Task.Delay(100);
        }

        var trimmedCity = City.Trim();
        var trimmedCountry = Country.Trim();

        if (string.IsNullOrWhiteSpace(trimmedCity) || string.IsNullOrWhiteSpace(trimmedCountry))
        {
            StatusMessage = "Devam etmek için şehir ve ülke bilgilerini girin.";
            return;
        }

        var wasFirstLaunch = IsFirstLaunch;
        var settings = _settingsService.GetSettings();
        var settingsToSave = new AppSettings
        {
            City = trimmedCity,
            CityDisplay = CityDisplay.Trim(),
            Region = CityRegion.Trim(),
            PlateCode = SelectedPlateCode,
            District = DistrictDisplay.Trim(),
            DistrictApiName = DistrictApiName.Trim(),
            Country = trimmedCountry,
            HasCompletedOnboarding = true,
            PrayerNotificationsEnabled = PrayerNotificationsEnabled,
            NotifyAtPrayerTime = NotifyAtPrayerTime,
            MinutesBeforePrayer = Math.Max(0, MinutesBeforePrayer),
            FridayNotificationsEnabled = FridayNotificationsEnabled,
            FridayNotificationTime = FridayNotificationTime,
            VerseNotificationsEnabled = VerseNotificationsEnabled,
            VerseNotificationCount = Math.Clamp(VerseNotificationCount, 1, 5),
            VerseNotificationTimes = VerseTimes.Select(v => v.Time).ToList(),
            RamadanModeEnabled = RamadanModeEnabled,
            Latitude = settings.Latitude,
            Longitude = settings.Longitude
        };

        IReadOnlyList<PrayerTime> prayers;
        try
        {
            var result = await _prayerTimeService.GetTodayPrayerTimesAsync(settingsToSave.LocationQuery, settingsToSave.Country);
            prayers = result.Times;
            if (result.Latitude != 0 && result.Longitude != 0)
            {
                settingsToSave.Latitude = result.Latitude;
                settingsToSave.Longitude = result.Longitude;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Şehir doğrulanamadı: {ex.Message}";
            return;
        }

        _settingsService.SaveSettings(settingsToSave);
        IsFirstLaunch = false;

        try
        {
            await _notificationService.RequestPermissionAsync();
            await _notificationService.RescheduleAllAsync(settingsToSave, prayers);
            var summary = await _notificationService.GetScheduleSummaryAsync();
            StatusMessage = $"Kaydedildi.\n{summary}";
            await RefreshDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ayarlar kaydedildi ancak bildirimler güncellenemedi: {ex.Message}";
        }

        if (wasFirstLaunch)
            await Shell.Current.GoToAsync("//MainPage");
    }

    [RelayCommand]
    private async Task TestNotificationAsync()
    {
        try
        {
            StatusMessage = await _notificationService.SendTestNotificationAsync();
            await RefreshDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Test bildirimi gönderilemedi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenNotificationSettingsAsync()
    {
        await _notificationService.OpenNotificationSettingsAsync();
        StatusMessage = "Telefon bildirim ayarları açıldı. Ezan Vakti için bildirimleri açın.";
    }

    [RelayCommand]
    private async Task OpenExactAlarmSettingsAsync()
    {
        await _notificationService.OpenExactAlarmSettingsAsync();
        StatusMessage = "Alarm izni ayarları açıldı. 'İzin ver' veya 'Açık' yapın.";
    }

    [RelayCommand]
    private async Task OpenBatterySettingsAsync()
    {
        await _notificationService.OpenBatterySettingsAsync();
        StatusMessage = "Pil optimizasyonu ayarı açıldı. Uygulamayı 'Kısıtlama yok' yapın.";
        await Task.Delay(1500);
        await RefreshDiagnosticsAsync();
    }
}

public partial class VerseTimeItem : ObservableObject
{
    public int Index { get; init; }

    [ObservableProperty]
    private TimeSpan _time;

    [ObservableProperty]
    private bool _isActive = true;
}
