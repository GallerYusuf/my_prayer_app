using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Models;
using PrayerApp.Services;
using PrayerApp.Services.Interfaces;

namespace PrayerApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly IPrayerScheduleService _scheduleService;
    private readonly IQuranRepository _quranRepository;
    private readonly ISettingsStore _settingsStore;
    private readonly INotificationCoordinator _notificationCoordinator;
    private readonly IWidgetDataService _widgetDataService;
    private readonly IPrayerTrackingService _trackingService;
    private readonly IClock _clock;
    private readonly ILogger<MainViewModel> _logger;

    private IDispatcherTimer? _countdownTimer;
    private IReadOnlyList<PrayerDay> _days = [];
    private DateOnly _loadedDate;
    private Verse? _currentVerse;
    private DateTime _lastWidgetUpdate = DateTime.MinValue;
    private bool _initialized;

    [ObservableProperty] private string _cityDisplay = "—";
    [ObservableProperty] private string _greeting = "Selâmün Aleyküm";
    [ObservableProperty] private string _todayDate = string.Empty;
    [ObservableProperty] private string _hijriDate = "—";
    [ObservableProperty] private string _dayPeriod = "day";
    [ObservableProperty] private string _nextPrayerName = "-";
    [ObservableProperty] private string _nextPrayerTime = "--:--";
    [ObservableProperty] private string _remainingTime = "--:--:--";
    [ObservableProperty] private string _remainingHours = "--";
    [ObservableProperty] private string _remainingMinutes = "--";
    [ObservableProperty] private string _remainingSeconds = "--";
    [ObservableProperty] private double _dayProgress;
    [ObservableProperty] private string _prayerProgressText = "0/6 vakit";
    [ObservableProperty] private string _verseOfDay = "Yükleniyor...";
    [ObservableProperty] private string _verseReference = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _verseActionMessage = string.Empty;
    [ObservableProperty] private bool _isRamadanVisible;
    [ObservableProperty] private string _ramadanDayText = string.Empty;
    [ObservableProperty] private string _ramadanCountdownLabel = string.Empty;
    [ObservableProperty] private string _ramadanCountdown = string.Empty;
    [ObservableProperty] private string _ramadanIftarTime = string.Empty;
    [ObservableProperty] private string _ramadanSahurTime = string.Empty;
    [ObservableProperty] private string _trackProgressText = "0/5 kılındı";
    [ObservableProperty] private int _trackStreak;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _dataStatusText = string.Empty;

    public ObservableCollection<PrayerTime> PrayerTimes { get; } = [];
    public ObservableCollection<PrayerTrackItem> TrackItems { get; } = [];

    public MainViewModel(
        IPrayerScheduleService scheduleService,
        IQuranRepository quranRepository,
        ISettingsStore settingsStore,
        INotificationCoordinator notificationCoordinator,
        IWidgetDataService widgetDataService,
        IPrayerTrackingService trackingService,
        IClock clock,
        ILogger<MainViewModel> logger)
    {
        _scheduleService = scheduleService;
        _quranRepository = quranRepository;
        _settingsStore = settingsStore;
        _notificationCoordinator = notificationCoordinator;
        _widgetDataService = widgetDataService;
        _trackingService = trackingService;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Called from the page's OnAppearing; loads data once and resumes the countdown.</summary>
    public async Task OnAppearingAsync()
    {
        var settings = _settingsStore.Get();
        if (!settings.HasCompletedOnboarding)
        {
            await Shell.Current.GoToAsync("//OnboardingPage");
            return;
        }

        if (!_initialized)
        {
            _initialized = true;
            await InitializeAsync();
        }
        else
        {
            if (_loadedDate != _clock.Today)
                await LoadAsync(forceRefresh: false, showBusy: false);

            StartCountdown();
        }
    }

    /// <summary>Called from the page's OnDisappearing; stops per-second UI updates.</summary>
    public void OnDisappearing() => _countdownTimer?.Stop();

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            LoadTracking();
            await LoadVerseOfDayAsync();
            await LoadAsync(forceRefresh: false, showBusy: true);
            StartCountdown();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPrayerTimesAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadAsync(forceRefresh: true, showBusy: false);
            StartCountdown();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshVerseAsync()
    {
        try
        {
            var total = await _quranRepository.GetTotalVerseCountAsync();
            if (total <= 0)
                return;

            var verse = await _quranRepository.GetVerseByGlobalIndexAsync(Random.Shared.Next(total));
            if (verse is not null)
                ShowVerse(verse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Random verse load failed");
        }
    }

    [RelayCommand]
    private static async Task OpenSettingsAsync() => await Shell.Current.GoToAsync("//SettingsPage");

    [RelayCommand]
    private static async Task OpenQiblaAsync() => await Shell.Current.GoToAsync("//QiblaPage");

    [RelayCommand]
    private static async Task OpenCalendarAsync() => await Shell.Current.GoToAsync("//CalendarPage");

    [RelayCommand]
    private static async Task OpenDhikrAsync() => await Shell.Current.GoToAsync("DhikrPage");

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

        var text = $"\"{_currentVerse.Text}\"\n\n— {_currentVerse.Reference}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Günün Ayeti", Text = text });
    }

    [RelayCommand]
    private async Task CopyVerseAsync()
    {
        if (_currentVerse is null)
            return;

        await Clipboard.Default.SetTextAsync($"\"{_currentVerse.Text}\" — {_currentVerse.Reference}");
        VerseActionMessage = "Ayet panoya kopyalandı";
        _ = ClearVerseActionMessageAsync();
    }

    private async Task LoadAsync(bool forceRefresh, bool showBusy)
    {
        ErrorMessage = string.Empty;
        UpdateHeader();

        var result = await _scheduleService.GetScheduleAsync(_clock.Today, 2, forceRefresh);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error.Message;
            IsOffline = result.Error.Kind is ErrorKind.Network or ErrorKind.Timeout;
            return;
        }

        _days = result.Value.Days;
        _loadedDate = _clock.Today;
        IsOffline = result.Value.IsFromCache && forceRefresh;
        DataStatusText = BuildDataStatusText(result.Value);

        var today = _days.FirstOrDefault(d => d.Date == _clock.Today);
        HijriDate = string.IsNullOrWhiteSpace(today?.HijriDateText) ? "—" : today!.HijriDateText;

        UpdatePrayerList(today);
        UpdateRamadan(today);
        UpdateCountdown();

        // Notifications are replanned off the UI path; failures are logged, not shown.
        _ = Task.Run(async () =>
        {
            try
            {
                await _notificationCoordinator.RescheduleIfStaleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification reschedule failed");
            }
        });
    }

    private string BuildDataStatusText(PrayerScheduleResult result)
    {
        if (result.LastFetchedAtUtc is not { } fetchedAt)
            return string.Empty;

        var local = fetchedAt.ToLocalTime();
        var prefix = result.IsFromCache ? "Kayıtlı veri" : "Güncellendi";
        return $"{prefix}: {local.ToString("d MMM HH:mm", TurkishCulture)}";
    }

    private void UpdateHeader()
    {
        var now = _clock.LocalNow;
        TodayDate = now.ToString("d MMMM yyyy, dddd", TurkishCulture);
        Greeting = now.Hour switch
        {
            >= 5 and < 12 => "Hayırlı Sabahlar",
            >= 12 and < 17 => "Hayırlı Günler",
            >= 17 and < 21 => "Hayırlı Akşamlar",
            _ => "Hayırlı Geceler"
        };
        UpdateDayPeriod();

        var settings = _settingsStore.Get();
        CityDisplay = settings.Location?.DisplayName is { Length: > 0 } name ? name : "Konum seçilmedi";
    }

    private void UpdateDayPeriod()
    {
        DayPeriod = _clock.LocalNow.Hour switch
        {
            >= 5 and < 7 => "dawn",
            >= 7 and < 12 => "morning",
            >= 12 and < 17 => "day",
            >= 17 and < 20 => "sunset",
            _ => "night"
        };
    }

    private async Task LoadVerseOfDayAsync()
    {
        try
        {
            var total = await _quranRepository.GetTotalVerseCountAsync();
            if (total <= 0)
                return;

            var index = VerseOfDaySelector.GetGlobalIndex(_clock.Today, total);
            var verse = await _quranRepository.GetVerseByGlobalIndexAsync(index);
            if (verse is not null)
                ShowVerse(verse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verse of day load failed");
            VerseOfDay = "Günün ayeti yüklenemedi.";
        }
    }

    private void ShowVerse(Verse verse)
    {
        _currentVerse = verse;
        VerseOfDay = verse.Text;
        VerseReference = verse.Reference;
        VerseActionMessage = string.Empty;
    }

    private void UpdatePrayerList(PrayerDay? today)
    {
        PrayerTimes.Clear();
        if (today is null)
            return;

        foreach (var kind in PrayerKindExtensions.All)
        {
            if (!today.Times.TryGetValue(kind, out var time))
                continue;

            PrayerTimes.Add(new PrayerTime
            {
                Name = kind.ToString(),
                TurkishName = kind.ToTurkishName(),
                Glyph = GetGlyph(kind),
                Time = time.ToTimeSpan()
            });
        }
    }

    private static string GetGlyph(PrayerKind kind) => kind switch
    {
        PrayerKind.Fajr => "🌙",
        PrayerKind.Sunrise => "🌅",
        PrayerKind.Dhuhr => "☀️",
        PrayerKind.Asr => "🌤️",
        PrayerKind.Maghrib => "🌇",
        PrayerKind.Isha => "🌃",
        _ => string.Empty
    };

    private void UpdateRamadan(PrayerDay? today)
    {
        var settings = _settingsStore.Get();
        IsRamadanVisible = settings.RamadanModeEnabled && today?.HijriMonth == 9;
        if (!IsRamadanVisible || today is null)
            return;

        RamadanDayText = $"{today.HijriDay}. gün";
        RamadanIftarTime = today[PrayerKind.Maghrib].ToString("HH\\:mm");
        RamadanSahurTime = today[PrayerKind.Fajr].ToString("HH\\:mm");
    }

    private void StartCountdown()
    {
        if (_countdownTimer is null)
        {
            _countdownTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_countdownTimer is null)
                return;

            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += (_, _) => UpdateCountdown();
        }

        if (!_countdownTimer.IsRunning)
            _countdownTimer.Start();
    }

    private void UpdateCountdown()
    {
        if (_days.Count == 0)
            return;

        // Day rollover while the page is open: reload today's data.
        if (_loadedDate != _clock.Today)
        {
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await LoadAsync(forceRefresh: false, showBusy: false);
                await LoadVerseOfDayAsync();
                LoadTracking();
            });
            _loadedDate = _clock.Today;
            return;
        }

        var now = _clock.LocalNow;
        var next = NextPrayerCalculator.FindNext(_days, now);
        if (next is null)
        {
            NextPrayerName = "-";
            RemainingTime = "--:--:--";
            return;
        }

        NextPrayerName = next.Kind.ToTurkishName();
        NextPrayerTime = TimeOnly.FromDateTime(next.OccursAt.DateTime).ToString("HH\\:mm");
        var remaining = next.Remaining;
        RemainingTime = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        RemainingHours = $"{(int)remaining.TotalHours:00}";
        RemainingMinutes = $"{remaining.Minutes:00}";
        RemainingSeconds = $"{remaining.Seconds:00}";

        UpdateDayStates(now, next);
        UpdateDayPeriod();
        UpdateRamadanCountdown(now);
        UpdateWidget(force: false);
    }

    private void UpdateDayStates(DateTimeOffset now, UpcomingPrayer next)
    {
        var passed = 0;
        foreach (var prayer in PrayerTimes)
        {
            var isNext = next.Date == _clock.Today && next.Kind.ToTurkishName() == prayer.TurkishName;
            prayer.IsNext = isNext;
            prayer.IsPassed = !isNext && prayer.Time <= now.TimeOfDay;
            if (prayer.IsPassed)
                passed++;
        }

        PrayerProgressText = $"{passed}/{PrayerTimes.Count} vakit geçti";
        DayProgress = CalculateProgress(next, now);
    }

    private double CalculateProgress(UpcomingPrayer next, DateTimeOffset now)
    {
        var ordered = _days
            .SelectMany(d => d.Times.Select(t => new DateTimeOffset(d.Date.ToDateTime(t.Value), now.Offset)))
            .OrderBy(t => t)
            .ToList();

        var previous = ordered.LastOrDefault(t => t <= now);
        if (previous == default)
            return 0;

        var interval = next.OccursAt - previous;
        if (interval <= TimeSpan.Zero)
            return 0;

        return Math.Clamp((now - previous).TotalSeconds / interval.TotalSeconds, 0, 1);
    }

    private void UpdateRamadanCountdown(DateTimeOffset now)
    {
        if (!IsRamadanVisible)
            return;

        var today = _days.FirstOrDefault(d => d.Date == _clock.Today);
        if (today is null)
            return;

        var maghribAt = new DateTimeOffset(_clock.Today.ToDateTime(today[PrayerKind.Maghrib]), now.Offset);
        if (now < maghribAt)
        {
            RamadanCountdownLabel = "İftara kalan";
            var remaining = maghribAt - now;
            RamadanCountdown = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
        else
        {
            var tomorrow = _days.FirstOrDefault(d => d.Date == _clock.Today.AddDays(1)) ?? today;
            var fajrAt = new DateTimeOffset(_clock.Today.AddDays(1).ToDateTime(tomorrow[PrayerKind.Fajr]), now.Offset);
            RamadanCountdownLabel = "Sahura kalan";
            var remaining = fajrAt - now;
            RamadanCountdown = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
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

    private void UpdateWidget(bool force)
    {
        if (!force && (DateTime.UtcNow - _lastWidgetUpdate).TotalSeconds < 55)
            return;

        _lastWidgetUpdate = DateTime.UtcNow;
        _widgetDataService.Update(CityDisplay, NextPrayerName, NextPrayerTime, RemainingTime);
    }

    private async Task ClearVerseActionMessageAsync()
    {
        await Task.Delay(2200);
        VerseActionMessage = string.Empty;
    }
}
