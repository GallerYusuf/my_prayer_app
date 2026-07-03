using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PrayerApp.Data;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPrayerTimeService _prayerTimeService;
    private readonly IVerseService _verseService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationSchedulerService _notificationService;
    private readonly IWidgetDataService _widgetDataService;
    private readonly IPrayerTrackingService _trackingService;
    private readonly IRamadanService _ramadanService;
    private IDispatcherTimer? _countdownTimer;
    private IReadOnlyList<PrayerTime> _prayerTimes = [];
    private PrayerDayResult? _dayResult;
    private RamadanInfo? _ramadanInfo;
    private QuranVerse? _currentVerse;
    private DateTime _lastWidgetUpdate = DateTime.MinValue;

    [ObservableProperty]
    private string _cityDisplay = "Van, Türkiye";

    [ObservableProperty]
    private string _greeting = "Selâmün Aleyküm";

    [ObservableProperty]
    private string _todayDate = string.Empty;

    [ObservableProperty]
    private string _hijriDate = string.Empty;

    [ObservableProperty]
    private string _dayPeriod = "day";

    [ObservableProperty]
    private string _nextPrayerName = "-";

    [ObservableProperty]
    private string _nextPrayerTime = "--:--";

    [ObservableProperty]
    private string _remainingTime = "--:--:--";

    [ObservableProperty]
    private string _remainingHours = "--";

    [ObservableProperty]
    private string _remainingMinutes = "--";

    [ObservableProperty]
    private string _remainingSeconds = "--";

    [ObservableProperty]
    private double _dayProgress;

    [ObservableProperty]
    private int _passedPrayersCount;

    [ObservableProperty]
    private string _prayerProgressText = "0/6 vakit";

    [ObservableProperty]
    private string _verseOfDay = "Yükleniyor...";

    [ObservableProperty]
    private string _verseReference = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _verseActionMessage = string.Empty;

    [ObservableProperty]
    private bool _isRamadanVisible;

    [ObservableProperty]
    private string _ramadanDayText = string.Empty;

    [ObservableProperty]
    private string _ramadanCountdownLabel = string.Empty;

    [ObservableProperty]
    private string _ramadanCountdown = string.Empty;

    [ObservableProperty]
    private string _ramadanIftarTime = string.Empty;

    [ObservableProperty]
    private string _ramadanSahurTime = string.Empty;

    [ObservableProperty]
    private string _trackProgressText = "0/5 kılındı";

    [ObservableProperty]
    private int _trackStreak;

    public ObservableCollection<PrayerTime> PrayerTimes { get; } = [];
    public ObservableCollection<PrayerTrackItem> TrackItems { get; } = [];

    public MainViewModel(
        IPrayerTimeService prayerTimeService,
        IVerseService verseService,
        ISettingsService settingsService,
        INotificationSchedulerService notificationService,
        IWidgetDataService widgetDataService,
        IPrayerTrackingService trackingService,
        IRamadanService ramadanService)
    {
        _prayerTimeService = prayerTimeService;
        _verseService = verseService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _widgetDataService = widgetDataService;
        _trackingService = trackingService;
        _ramadanService = ramadanService;

        WeakReferenceMessenger.Default.Register<VerseNotificationTappedMessage>(this, (_, message) =>
        {
            MainThread.BeginInvokeOnMainThread(() => ShowVerse(message.Value));
        });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            LoadTracking();

            await _verseService.InitializeAsync();

            var tappedVerse = App.ConsumePendingNotificationVerse();
            if (tappedVerse is not null)
                ShowVerse(tappedVerse);

            if (await LoadHomeDataAsync(refreshVerse: tappedVerse is null, requestPermission: true))
                StartCountdown();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPrayerTimesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsRefreshing = true;
            ErrorMessage = string.Empty;

            await _verseService.InitializeAsync();
            if (await LoadHomeDataAsync(refreshVerse: false, requestPermission: false))
                StartCountdown();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void RefreshVerse() => LoadVerseOfDay();

    [RelayCommand]
    private static async Task OpenSettingsAsync() =>
        await Shell.Current.GoToAsync("//SettingsPage");

    [RelayCommand]
    private void TogglePrayer(PrayerTrackItem item)
    {
        _trackingService.Toggle(item.Name);
        item.IsCompleted = !item.IsCompleted;
        RefreshTrackingSummary();
    }

    [RelayCommand]
    private async Task ShareVerseAsync()
    {
        if (_currentVerse is null)
            return;

        var text = $"\"{_currentVerse.Text}\"\n\n— {_currentVerse.SurahName} Suresi, {_currentVerse.AyahNumber}. Ayet\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Günün Ayeti",
            Text = text
        });
    }

    [RelayCommand]
    private async Task CopyVerseAsync()
    {
        if (_currentVerse is null)
            return;

        var text = $"\"{_currentVerse.Text}\" — {_currentVerse.SurahName} {_currentVerse.AyahNumber}";
        await Clipboard.Default.SetTextAsync(text);
        VerseActionMessage = "Ayet panoya kopyalandı";
        _ = ClearVerseActionMessageAsync();
    }

    private async Task ClearVerseActionMessageAsync()
    {
        await Task.Delay(2200);
        VerseActionMessage = string.Empty;
    }

    private void LoadTracking()
    {
        TrackItems.Clear();
        foreach (var item in _trackingService.GetTodayTrackItems())
            TrackItems.Add(item);
        RefreshTrackingSummary();
    }

    private void RefreshTrackingSummary()
    {
        TrackProgressText = _trackingService.GetProgressText();
        TrackStreak = _trackingService.GetStreak();
    }

    private async Task<bool> LoadHomeDataAsync(bool refreshVerse, bool requestPermission)
    {
        if (refreshVerse)
            LoadVerseOfDay();

        UpdateGreeting();
        UpdateDayPeriod();
        TodayDate = DateTime.Now.ToString("d MMMM yyyy, dddd", new CultureInfo("tr-TR"));

        var settings = _settingsService.GetSettings();
        CityDisplay = BuildCityDisplay(settings);
        if (!settings.HasCompletedOnboarding)
        {
            ErrorMessage = "Namaz vakitlerini göstermek için önce şehir seçin.";
            await Shell.Current.GoToAsync("//SettingsPage");
            return false;
        }

        await LoadPrayerTimesAsync(settings);

        if (requestPermission)
            await _notificationService.RequestPermissionAsync();

        await _notificationService.RescheduleIfStaleAsync(settings, _prayerTimes);
        UpdateWidget(force: true);
        return true;
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        Greeting = hour switch
        {
            >= 5 and < 12 => "Hayırlı Sabahlar",
            >= 12 and < 17 => "Hayırlı Günler",
            >= 17 and < 21 => "Hayırlı Akşamlar",
            _ => "Hayırlı Geceler"
        };
    }

    private void UpdateDayPeriod()
    {
        var hour = DateTime.Now.Hour;
        DayPeriod = hour switch
        {
            >= 5 and < 7 => "dawn",
            >= 7 and < 12 => "morning",
            >= 12 and < 17 => "day",
            >= 17 and < 20 => "sunset",
            _ => "night"
        };
    }

    private void LoadVerseOfDay() => ShowVerse(_verseService.GetRandomVerse());

    private void ShowVerse(QuranVerse verse)
    {
        _currentVerse = verse;
        VerseOfDay = verse.Text;
        VerseReference = $"{verse.SurahName} Suresi, {verse.AyahNumber}. Ayet";
        VerseActionMessage = string.Empty;
    }

    private async Task LoadPrayerTimesAsync(AppSettings settings)
    {
        var result = await _prayerTimeService.GetTodayPrayerTimesAsync(settings.LocationQuery, settings.Country);
        _dayResult = result;
        _prayerTimes = result.Times;
        HijriDate = string.IsNullOrWhiteSpace(result.HijriDate) ? "—" : result.HijriDate;

        if (result.Latitude != 0 && result.Longitude != 0 &&
            (settings.Latitude != result.Latitude || settings.Longitude != result.Longitude))
        {
            settings.Latitude = result.Latitude;
            settings.Longitude = result.Longitude;
            _settingsService.SaveSettings(settings);
        }

        UpdateRamadan(settings);
        UpdatePrayerList();
        UpdateCountdown();
    }

    private void UpdateRamadan(AppSettings settings)
    {
        if (_dayResult is null)
        {
            IsRamadanVisible = false;
            return;
        }

        _ramadanInfo = _ramadanService.BuildInfo(_dayResult, _prayerTimes, settings.RamadanModeEnabled);
        IsRamadanVisible = _ramadanInfo is not null;
        if (_ramadanInfo is null)
            return;

        RamadanDayText = $"{_ramadanInfo.RamadanDay}. gün";
        RamadanCountdownLabel = _ramadanInfo.NextEventLabel;
        RamadanCountdown = _ramadanInfo.CountdownText;
        RamadanIftarTime = _ramadanInfo.IftarTime;
        RamadanSahurTime = _ramadanInfo.SahurTime;
    }

    private static string BuildCityDisplay(AppSettings settings)
    {
        var city = TurkishCities.FindByApiName(settings.City);
        var cityName = city?.DisplayName ?? (string.IsNullOrWhiteSpace(settings.CityDisplay) ? settings.City : settings.CityDisplay);
        return settings.HasDistrict ? $"{settings.District}, {cityName}" : cityName;
    }

    private void UpdatePrayerList()
    {
        var (next, _) = _prayerTimeService.GetNextPrayer(_prayerTimes);
        PrayerTimes.Clear();
        foreach (var prayer in _prayerTimes)
        {
            prayer.IsNext = next?.Name == prayer.Name;
            PrayerTimes.Add(prayer);
        }
    }

    private void StartCountdown()
    {
        _countdownTimer?.Stop();
        _countdownTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_countdownTimer is null)
            return;

        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += (_, _) => UpdateCountdown();
        _countdownTimer.Start();
    }

    private void UpdateCountdown()
    {
        if (_prayerTimes.Count == 0)
            return;

        var (next, remaining) = _prayerTimeService.GetNextPrayer(_prayerTimes);
        NextPrayerName = next?.TurkishName ?? "-";
        NextPrayerTime = next is not null ? next.Time.ToString(@"hh\:mm") : "--:--";
        RemainingTime = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        RemainingHours = $"{(int)remaining.TotalHours:00}";
        RemainingMinutes = $"{remaining.Minutes:00}";
        RemainingSeconds = $"{remaining.Seconds:00}";
        DayProgress = CalculateProgress(next, remaining);

        var now = DateTime.Now.TimeOfDay;
        var passed = 0;
        foreach (var prayer in PrayerTimes)
        {
            prayer.IsNext = next?.Name == prayer.Name;
            prayer.IsPassed = prayer.Time <= now && !prayer.IsNext;
            if (prayer.IsPassed)
                passed++;
        }

        PassedPrayersCount = passed;
        PrayerProgressText = $"{passed}/{PrayerTimes.Count} vakit geçti";
        UpdateDayPeriod();

        if (_ramadanInfo is not null)
        {
            _ramadanInfo = _ramadanService.UpdateCountdown(_ramadanInfo, _prayerTimes);
            RamadanCountdownLabel = _ramadanInfo.NextEventLabel;
            RamadanCountdown = _ramadanInfo.CountdownText;
        }

        UpdateWidget(force: false);
    }

    private void UpdateWidget(bool force)
    {
        if (!force && (DateTime.UtcNow - _lastWidgetUpdate).TotalSeconds < 55)
            return;

        _lastWidgetUpdate = DateTime.UtcNow;
        _widgetDataService.Update(CityDisplay, NextPrayerName, NextPrayerTime, RemainingTime);
    }

    private double CalculateProgress(PrayerTime? next, TimeSpan remaining)
    {
        if (next is null)
            return 0;

        var ordered = _prayerTimes.OrderBy(p => p.Time).ToList();
        var nextIndex = ordered.FindIndex(p => p.Name == next.Name);

        TimeSpan interval;
        if (nextIndex <= 0)
        {
            var lastPrayer = ordered[^1].Time;
            interval = TimeSpan.FromHours(24) - lastPrayer + next.Time;
        }
        else
        {
            interval = next.Time - ordered[nextIndex - 1].Time;
        }

        if (interval <= TimeSpan.Zero)
            return 0;

        var elapsed = interval - remaining;
        return Math.Clamp(elapsed.TotalSeconds / interval.TotalSeconds, 0, 1);
    }
}
