using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Data;
using PrayerApp.Models;
using PrayerApp.Services;
using PrayerApp.Views;

namespace PrayerApp.ViewModels;

public sealed record CalculationMethodOption(CalculationMethod Method, string DisplayName);

public partial class AdjustmentItem : ObservableObject
{
    public required PrayerKind Kind { get; init; }
    public string Name => Kind.ToTurkishName();

    [ObservableProperty] private int _minutes;

    [RelayCommand]
    private void Increment() => Minutes = Math.Min(Minutes + 1, PrayerAdjustments.MaxMinutes);

    [RelayCommand]
    private void Decrement() => Minutes = Math.Max(Minutes - 1, PrayerAdjustments.MinMinutes);
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ILocationService _locationService;
    private readonly INotificationScheduler _notificationScheduler;
    private readonly Core.Services.INotificationCoordinator _notificationCoordinator;
    private readonly ILogger<SettingsViewModel> _logger;

    private GeoLocation? _pendingLocation;
    private int _pendingPlateCode;
    private string _pendingDistrictApiName = string.Empty;

    [ObservableProperty] private string _locationSummary = "—";
    [ObservableProperty] private bool _isLocating;
    [ObservableProperty] private int _selectedMethodIndex;
    [ObservableProperty] private int _themeIndex;
    [ObservableProperty] private bool _prayerNotificationsEnabled = true;
    [ObservableProperty] private bool _sunriseNotificationEnabled;
    [ObservableProperty] private bool _persistentNotificationEnabled = true;
    [ObservableProperty] private int _minutesBeforePrayer;
    [ObservableProperty] private bool _soundEnabled = true;
    [ObservableProperty] private bool _vibrationEnabled = true;
    [ObservableProperty] private bool _verseNotificationsEnabled = true;
    [ObservableProperty] private TimeSpan _verseTime = new(9, 0, 0);
    [ObservableProperty] private bool _fridayNotificationsEnabled = true;
    [ObservableProperty] private TimeSpan _fridayTime = new(11, 30, 0);
    [ObservableProperty] private bool _ramadanModeEnabled = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _diagnosticsText = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public ObservableCollection<AdjustmentItem> Adjustments { get; } = [];
    public IReadOnlyList<CalculationMethodOption> MethodOptions { get; }
    public IReadOnlyList<string> ThemeOptions { get; } = ["Sistem", "Açık", "Koyu"];
    public int[] MinutesBeforeOptions { get; } = [0, 5, 10, 15, 30];

    public string AppVersion => $"Ezan Vakti {AppInfo.Current.VersionString}";

    public SettingsViewModel(
        ISettingsStore settingsStore,
        ILocationService locationService,
        INotificationScheduler notificationScheduler,
        Core.Services.INotificationCoordinator notificationCoordinator,
        ILogger<SettingsViewModel> logger)
    {
        _settingsStore = settingsStore;
        _locationService = locationService;
        _notificationScheduler = notificationScheduler;
        _notificationCoordinator = notificationCoordinator;
        _logger = logger;

        MethodOptions = CalculationMethodExtensions.All
            .Select(m => new CalculationMethodOption(m, m.ToDisplayName()))
            .ToList();
    }

    [RelayCommand]
    public void LoadSettings()
    {
        var settings = _settingsStore.Get();

        _pendingLocation = settings.Location;
        _pendingPlateCode = settings.CityPlateCode;
        _pendingDistrictApiName = settings.DistrictApiName;
        LocationSummary = settings.Location?.DisplayName ?? "Konum seçilmedi";

        SelectedMethodIndex = Math.Max(0, MethodOptions.ToList().FindIndex(o => o.Method == settings.CalculationMethod));
        ThemeIndex = (int)settings.Theme;

        PrayerNotificationsEnabled = settings.PrayerNotificationsEnabled;
        SunriseNotificationEnabled = !settings.PrayerNotificationToggles.TryGetValue(PrayerKind.Sunrise, out var sunrise) || sunrise;
        PersistentNotificationEnabled = settings.PersistentNotificationEnabled;
        MinutesBeforePrayer = settings.MinutesBeforePrayer;
        SoundEnabled = settings.NotificationSoundEnabled;
        VibrationEnabled = settings.NotificationVibrationEnabled;
        VerseNotificationsEnabled = settings.VerseNotificationsEnabled;
        VerseTime = (settings.VerseNotificationTimes.FirstOrDefault(new TimeOnly(9, 0))).ToTimeSpan();
        FridayNotificationsEnabled = settings.FridayNotificationsEnabled;
        FridayTime = settings.FridayNotificationTime.ToTimeSpan();
        RamadanModeEnabled = settings.RamadanModeEnabled;

        Adjustments.Clear();
        foreach (var kind in PrayerKindExtensions.All)
            Adjustments.Add(new AdjustmentItem { Kind = kind, Minutes = settings.Adjustments.GetMinutes(kind) });

        _ = RefreshDiagnosticsAsync();
    }

    [RelayCommand]
    private async Task UseDeviceLocationAsync()
    {
        if (IsLocating)
            return;

        try
        {
            IsLocating = true;
            StatusMessage = string.Empty;

            var result = await _locationService.GetCurrentLocationAsync();
            if (!result.IsSuccess)
            {
                StatusMessage = result.Error.Message;
                return;
            }

            _pendingLocation = result.Value;
            _pendingPlateCode = 0;
            _pendingDistrictApiName = string.Empty;
            LocationSummary = result.Value.DisplayName;
        }
        finally
        {
            IsLocating = false;
        }
    }

    [RelayCommand]
    private async Task OpenCityPickerAsync()
    {
        var navigation = Shell.Current?.Navigation;
        if (navigation is null)
            return;

        var pickerVm = new CityPickerViewModel(_pendingPlateCode, _pendingDistrictApiName);
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
        if (selected is null)
            return;

        if (CityCoordinates.FindByPlate(selected.City.PlateCode) is not { } coords)
            return;

        _pendingLocation = new GeoLocation
        {
            Latitude = coords.Lat,
            Longitude = coords.Lon,
            DisplayName = selected.Display,
            IsFromDevice = false
        };
        _pendingPlateCode = selected.City.PlateCode;
        _pendingDistrictApiName = selected.District?.ApiName ?? string.Empty;
        LocationSummary = selected.Display;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving)
            return;

        if (_pendingLocation is null || !_pendingLocation.IsValid)
        {
            StatusMessage = "Kaydetmeden önce bir konum seçin.";
            return;
        }

        try
        {
            IsSaving = true;
            StatusMessage = string.Empty;

            var current = _settingsStore.Get();
            var updated = current with
            {
                HasCompletedOnboarding = true,
                Location = _pendingLocation,
                LocationMode = _pendingLocation.IsFromDevice ? LocationMode.Automatic : LocationMode.Manual,
                CityPlateCode = _pendingPlateCode,
                DistrictApiName = _pendingDistrictApiName,
                CalculationMethod = MethodOptions[Math.Clamp(SelectedMethodIndex, 0, MethodOptions.Count - 1)].Method,
                Theme = (ThemePreference)Math.Clamp(ThemeIndex, 0, 2),
                Adjustments = BuildAdjustments(),
                PrayerNotificationsEnabled = PrayerNotificationsEnabled,
                PrayerNotificationToggles = new Dictionary<PrayerKind, bool> { [PrayerKind.Sunrise] = SunriseNotificationEnabled },
                PersistentNotificationEnabled = PersistentNotificationEnabled,
                MinutesBeforePrayer = MinutesBeforePrayer,
                NotificationSoundEnabled = SoundEnabled,
                NotificationVibrationEnabled = VibrationEnabled,
                VerseNotificationsEnabled = VerseNotificationsEnabled,
                VerseNotificationTimes = [TimeOnly.FromTimeSpan(VerseTime)],
                FridayNotificationsEnabled = FridayNotificationsEnabled,
                FridayNotificationTime = TimeOnly.FromTimeSpan(FridayTime),
                RamadanModeEnabled = RamadanModeEnabled
            };

            _settingsStore.Save(updated);
            ThemeManager.Apply(updated.Theme);

            if (PrayerNotificationsEnabled || VerseNotificationsEnabled || FridayNotificationsEnabled || PersistentNotificationEnabled)
                await _notificationScheduler.RequestPermissionAsync();

            // Atomic replan: the coordinator fetches/validates data first and only
            // then swaps the old notification plan for the new one.
            var replan = await _notificationCoordinator.RescheduleAsync(force: true);
            StatusMessage = replan.IsSuccess
                ? $"Ayarlar kaydedildi. {replan.Value} bildirim planlandı."
                : $"Ayarlar kaydedildi. {replan.Error.Message}";

            await RefreshDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings save failed");
            StatusMessage = "Ayarlar kaydedilirken bir sorun oluştu. Lütfen tekrar deneyin.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private PrayerAdjustments BuildAdjustments()
    {
        var adjustments = PrayerAdjustments.None;
        foreach (var item in Adjustments)
            adjustments = adjustments.WithMinutes(item.Kind, item.Minutes);
        return adjustments;
    }

    [RelayCommand]
    private void SelectMinutesBefore(string minutes)
    {
        if (int.TryParse(minutes, out var value))
            MinutesBeforePrayer = Math.Clamp(value, 0, 120);
    }

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        try
        {
            var permitted = await _notificationScheduler.AreNotificationsPermittedAsync();
            var pending = await _notificationScheduler.GetPendingCountAsync();
            var exact = await CheckExactAlarmAsync();

            var lines = new List<string>
            {
                permitted ? "Bildirim izni: Açık" : "Bildirim izni: Kapalı",
                exact ? "Tam zamanlı alarm: Açık" : "Tam zamanlı alarm: Kapalı (bildirimler gecikebilir)",
                $"Planlanmış bildirim: {pending}"
            };

            DiagnosticsText = string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics read failed");
            DiagnosticsText = "Bildirim durumu okunamadı.";
        }
    }

    private static async Task<bool> CheckExactAlarmAsync()
    {
#if ANDROID
        var android = Plugin.LocalNotification.LocalNotificationCenter.AndroidService;
        if (android is not null)
            return await android.CanScheduleExactNotifications();
#endif
        await Task.CompletedTask;
        return true;
    }

    [RelayCommand]
    private async Task TestNotificationAsync()
    {
        try
        {
            await _notificationScheduler.RequestPermissionAsync();
            if (!await _notificationScheduler.AreNotificationsPermittedAsync())
            {
                StatusMessage = "Bildirim izni kapalı. Telefon ayarlarından bildirimlere izin verin.";
                return;
            }

            await Plugin.LocalNotification.LocalNotificationCenter.Current.Show(
                new Plugin.LocalNotification.Core.Models.NotificationRequest
                {
                    NotificationId = 999_999_001,
                    Title = "Test Bildirimi",
                    Description = "Bildirimler çalışıyor.",
                    Android = new Plugin.LocalNotification.Core.Models.AndroidOption.AndroidOptions
                    {
                        ChannelId = LocalNotificationScheduler.ChannelOther,
                        IconSmallName = new Plugin.LocalNotification.Core.Models.AndroidOption.AndroidIcon("notification_icon")
                    }
                });

            StatusMessage = "Test bildirimi gönderildi.";
            await RefreshDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test notification failed");
            StatusMessage = "Test bildirimi gönderilemedi.";
        }
    }

    [RelayCommand]
    private Task OpenNotificationSettingsAsync()
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionAppNotificationSettings);
            intent.PutExtra(global::Android.Provider.Settings.ExtraAppPackage, context.PackageName);
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
#endif
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenExactAlarmSettingsAsync()
    {
#if ANDROID
        Plugin.LocalNotification.LocalNotificationCenter.AndroidService?.RequestExactAlarmsPermission();
#endif
        return Task.CompletedTask;
    }
}
