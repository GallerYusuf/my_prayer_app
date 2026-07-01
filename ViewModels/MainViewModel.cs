using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Models;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPrayerTimeService _prayerTimeService;
    private readonly IVerseService _verseService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationSchedulerService _notificationService;
    private IDispatcherTimer? _countdownTimer;
    private IReadOnlyList<PrayerTime> _prayerTimes = [];

    [ObservableProperty]
    private string _cityDisplay = "Van, Turkey";

    [ObservableProperty]
    private string _greeting = "Selâmün Aleyküm";

    [ObservableProperty]
    private string _todayDate = string.Empty;

    [ObservableProperty]
    private string _nextPrayerName = "-";

    [ObservableProperty]
    private string _nextPrayerTime = "--:--";

    [ObservableProperty]
    private string _remainingTime = "--:--:--";

    [ObservableProperty]
    private double _dayProgress;

    [ObservableProperty]
    private string _verseOfDay = "Yükleniyor...";

    [ObservableProperty]
    private string _verseReference = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<PrayerTime> PrayerTimes { get; } = [];

    public MainViewModel(
        IPrayerTimeService prayerTimeService,
        IVerseService verseService,
        ISettingsService settingsService,
        INotificationSchedulerService notificationService)
    {
        _prayerTimeService = prayerTimeService;
        _verseService = verseService;
        _settingsService = settingsService;
        _notificationService = notificationService;
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

            await _verseService.InitializeAsync();
            LoadVerseOfDay();

            UpdateGreeting();
            TodayDate = DateTime.Now.ToString("d MMMM yyyy, dddd", new CultureInfo("tr-TR"));

            var settings = _settingsService.GetSettings();
            CityDisplay = $"{settings.City}, {settings.Country}";
            if (!settings.HasCompletedOnboarding)
            {
                ErrorMessage = "Namaz vakitlerini göstermek için önce şehir ve ülke seçin.";
                await Shell.Current.GoToAsync("//SettingsPage");
                return;
            }

            await LoadPrayerTimesAsync(settings);
            await _notificationService.RequestPermissionAsync();
            await _notificationService.RescheduleIfStaleAsync(settings, _prayerTimes);

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
    private void RefreshVerse()
    {
        LoadVerseOfDay();
    }

    [RelayCommand]
    private static async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("//SettingsPage");
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

    private void LoadVerseOfDay()
    {
        var verse = _verseService.GetRandomVerse();
        VerseOfDay = verse.Text;
        VerseReference = $"{verse.SurahName} Suresi, {verse.AyahNumber}. Ayet";
    }

    private async Task LoadPrayerTimesAsync(AppSettings settings)
    {
        _prayerTimes = await _prayerTimeService.GetTodayPrayerTimesAsync(settings.City, settings.Country);
        UpdatePrayerList();
        UpdateCountdown();
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
        DayProgress = CalculateProgress(next, remaining);

        var now = DateTime.Now.TimeOfDay;
        foreach (var prayer in PrayerTimes)
        {
            prayer.IsNext = next?.Name == prayer.Name;
            prayer.IsPassed = prayer.Time <= now && !prayer.IsNext;
        }
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
