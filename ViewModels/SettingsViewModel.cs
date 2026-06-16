using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

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
    private bool _prayerNotificationsEnabled = true;

    [ObservableProperty]
    private bool _notifyAtPrayerTime = true;

    [ObservableProperty]
    private int _minutesBeforePrayer = 10;

    [ObservableProperty]
    private bool _verseNotificationsEnabled = true;

    [ObservableProperty]
    private int _verseNotificationCount = 3;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _notificationDiagnostics = "Kontrol ediliyor...";

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
        City = settings.City;
        Country = settings.Country;
        PrayerNotificationsEnabled = settings.PrayerNotificationsEnabled;
        NotifyAtPrayerTime = settings.NotifyAtPrayerTime;
        MinutesBeforePrayer = settings.MinutesBeforePrayer;
        VerseNotificationsEnabled = settings.VerseNotificationsEnabled;
        VerseNotificationCount = settings.VerseNotificationCount;

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
    private async Task SaveAsync()
    {
        if (Application.Current?.MainPage is not null)
        {
            Application.Current.MainPage.Unfocus();
            await Task.Delay(100);
        }

        var settings = new AppSettings
        {
            City = City.Trim(),
            Country = Country.Trim(),
            PrayerNotificationsEnabled = PrayerNotificationsEnabled,
            NotifyAtPrayerTime = NotifyAtPrayerTime,
            MinutesBeforePrayer = Math.Max(0, MinutesBeforePrayer),
            VerseNotificationsEnabled = VerseNotificationsEnabled,
            VerseNotificationCount = Math.Clamp(VerseNotificationCount, 1, 5),
            VerseNotificationTimes = VerseTimes.Select(v => v.Time).ToList()
        };

        _settingsService.SaveSettings(settings);

        try
        {
            await _notificationService.RequestPermissionAsync();
            var prayers = await _prayerTimeService.GetTodayPrayerTimesAsync(settings.City, settings.Country);
            await _notificationService.RescheduleAllAsync(settings, prayers);
            var summary = await _notificationService.GetScheduleSummaryAsync();
            StatusMessage = $"Kaydedildi.\n{summary}";
            await RefreshDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ayarlar kaydedildi ancak bildirimler güncellenemedi: {ex.Message}";
        }
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
