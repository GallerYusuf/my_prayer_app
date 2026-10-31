using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Data;
using PrayerApp.Models;
using PrayerApp.Services;
using PrayerApp.Services.Interfaces;
using PrayerApp.Views;

namespace PrayerApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly IPrayerScheduleService _scheduleService;
    private readonly IQuranRepository _quranRepository;
    private readonly IKhatmRepository _khatmRepository;
    private readonly IDhikrRepository _dhikrRepository;
    private readonly ISettingsStore _settingsStore;
    private readonly IKeyValueStore _keyValueStore;
    private readonly INotificationCoordinator _notificationCoordinator;
    private readonly IPrayerTrackingService _trackingService;
    private readonly IClock _clock;
    private readonly ILogger<MainViewModel> _logger;

    private IDispatcherTimer? _countdownTimer;
    private IReadOnlyList<PrayerDay> _days = [];
    private DateOnly _loadedDate;
    private Verse? _currentVerse;
    private bool _initialized;

    [ObservableProperty] private string _cityDisplay = "Konum seçin";
    [ObservableProperty] private string _greeting = "Selâmün Aleyküm";
    [ObservableProperty] private string _todayDate = string.Empty;
    [ObservableProperty] private string _hijriDate = "—";
    [ObservableProperty] private string _dayPeriod = "day";
    [ObservableProperty] private string _nextPrayerName = "—";
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
    [ObservableProperty] private string _trackProgressText = "0/5 vakit kılındı";
    [ObservableProperty] private int _trackStreak;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _dataStatusText = string.Empty;
    [ObservableProperty] private string _kerahatText = string.Empty;
    [ObservableProperty] private bool _hasScheduleData;

    // Quick resume & tools summaries
    [ObservableProperty] private string _quranResumeTitle = "Kur'an Okumaya Başla";
    [ObservableProperty] private string _quranResumeSubtitle = "114 sure · Arapça metin ve Türkçe meal";
    [ObservableProperty] private string _qiblaSummaryText = "Kıble yönünü bul";
    [ObservableProperty] private string _dhikrSummaryText = "Sübhanallah · 33";

    public ObservableCollection<PrayerTime> PrayerTimes { get; } = [];
    public ObservableCollection<PrayerTrackItem> TrackItems { get; } = [];

    public MainViewModel(
        IPrayerScheduleService scheduleService,
        IQuranRepository quranRepository,
        IKhatmRepository khatmRepository,
        IDhikrRepository dhikrRepository,
        ISettingsStore settingsStore,
        IKeyValueStore keyValueStore,
        INotificationCoordinator notificationCoordinator,
        IPrayerTrackingService trackingService,
        IClock clock,
        ILogger<MainViewModel> logger)
    {
        _scheduleService = scheduleService;
        _quranRepository = quranRepository;
        _khatmRepository = khatmRepository;
        _dhikrRepository = dhikrRepository;
        _settingsStore = settingsStore;
        _keyValueStore = keyValueStore;
        _notificationCoordinator = notificationCoordinator;
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
            var locationChanged = (settings.Location?.DisplayName ?? "Konum seçin") != CityDisplay;
            if (_loadedDate != _clock.Today || locationChanged || !HasScheduleData)
                await LoadAsync(forceRefresh: false, showBusy: false);

            LoadTracking();
            SyncPrayerCompletionStates();
            await RefreshQuickCardsAsync();
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
            await RefreshQuickCardsAsync();
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
            await RefreshQuickCardsAsync();
            StartCountdown();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task OpenCityPickerAsync()
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
        if (selected is null)
            return;

        if (CityCoordinates.FindByPlate(selected.City.PlateCode) is not { } coords)
            return;

        var updatedLocation = new GeoLocation
        {
            Latitude = coords.Lat,
            Longitude = coords.Lon,
            DisplayName = selected.Display,
            IsFromDevice = false
        };

        var updatedSettings = current with
        {
            Location = updatedLocation,
            LocationMode = LocationMode.Manual,
            CityPlateCode = selected.City.PlateCode,
            DistrictApiName = selected.District?.ApiName ?? string.Empty
        };

        _settingsStore.Save(updatedSettings);
        await LoadAsync(forceRefresh: true, showBusy: true);
        await RefreshQuickCardsAsync();
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
    private async Task OpenVerseInQuranAsync()
    {
        if (_currentVerse is null)
            return;

        await Shell.Current.GoToAsync($"SurahPage?surah={_currentVerse.SurahNumber}&verse={_currentVerse.VerseNumber}");
    }

    [RelayCommand]
    private async Task ContinueQuranAsync()
    {
        if (SurahViewModel.GetLastRead(_keyValueStore) is { } pos)
        {
            await Shell.Current.GoToAsync($"SurahPage?surah={pos.Surah}&verse={pos.Verse}");
            return;
        }

        var plan = await _khatmRepository.GetActivePlanAsync();
        if (plan is not null)
        {
            var nextPage = Math.Clamp(plan.LastReadPage + 1, 1, KhatmPlan.TotalPages);
            var startVerse = await _quranRepository.GetPageStartAsync(nextPage);
            if (startVerse is not null)
            {
                await Shell.Current.GoToAsync($"SurahPage?surah={startVerse.SurahNumber}&verse={startVerse.VerseNumber}");
                return;
            }
        }

        await Shell.Current.GoToAsync("//QuranPage");
    }

    [RelayCommand]
    private static async Task OpenSettingsAsync() => await Shell.Current.GoToAsync("SettingsPage");

    [RelayCommand]
    private static async Task OpenQiblaAsync() => await Shell.Current.GoToAsync("QiblaPage");

    [RelayCommand]
    private static async Task OpenCalendarAsync() => await Shell.Current.GoToAsync("//CalendarPage");

    [RelayCommand]
    private static async Task OpenDhikrAsync() => await Shell.Current.GoToAsync("DhikrPage");

    [RelayCommand]
    private static async Task OpenWorshipAsync() => await Shell.Current.GoToAsync("//WorshipPage");

    [RelayCommand]
    private static async Task OpenEsmaulHusnaAsync() => await Shell.Current.GoToAsync("EsmaulHusnaPage");

    [RelayCommand]
    private static async Task OpenDuasAsync() => await Shell.Current.GoToAsync("DuasPage");

    [RelayCommand]
    private static async Task OpenReligiousDaysAsync() => await Shell.Current.GoToAsync("ReligiousDaysPage");

    [RelayCommand]
    private static async Task OpenKazaAsync() => await Shell.Current.GoToAsync("KazaPage");

    [RelayCommand]
    private static async Task OpenZakatAsync() => await Shell.Current.GoToAsync("ZakatPage");

    [RelayCommand]
    private void TogglePrayer(PrayerTrackItem item)
    {
        _trackingService.Toggle(item.Name);
        item.IsCompleted = !item.IsCompleted;
        SyncPrayerCompletionStates();
        RefreshTrackingSummary();
    }

    [RelayCommand]
    private void TogglePrayerTime(PrayerTime item)
    {
        if (!item.IsTrackable)
            return;

        _trackingService.Toggle(item.Name);
        item.IsCompleted = _trackingService.IsCompletedToday(item.Name);

        var trackItem = TrackItems.FirstOrDefault(t => t.Name == item.Name);
        if (trackItem is not null)
            trackItem.IsCompleted = item.IsCompleted;

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

    private async Task RefreshQuickCardsAsync()
    {
        try
        {
            var settings = _settingsStore.Get();
            if (settings.Location is { IsValid: true } loc)
            {
                var bearing = QiblaCalculator.CalculateBearing(loc.Latitude, loc.Longitude);
                QiblaSummaryText = $"Kıble · {bearing:F0}°";
            }
            else
            {
                QiblaSummaryText = "Kıble Pusulası";
            }

            // Quran resume card
            var lastRead = SurahViewModel.GetLastRead(_keyValueStore);
            var activeKhatm = await _khatmRepository.GetActivePlanAsync();
            if (lastRead is { } pos)
            {
                var surahs = await _quranRepository.GetSurahsAsync();
                var surah = surahs.FirstOrDefault(s => s.Number == pos.Surah);
                QuranResumeTitle = surah is not null
                    ? $"{surah.TranslatedName} Suresi, {pos.Verse}. ayet"
                    : $"{pos.Surah}. Sure, {pos.Verse}. ayet";
            }
            else
            {
                QuranResumeTitle = "Kur'an okumaya başla";
            }

            QuranResumeSubtitle = activeKhatm is not null
                ? $"Hatim: {activeKhatm.LastReadPage}/{KhatmPlan.TotalPages}. sayfa (%{activeKhatm.Progress * 100:0})"
                : "Kaldığın yerden devam et veya sure seç";

            // Dhikr summary
            var history = await _dhikrRepository.GetHistoryAsync(10);
            var todayTotal = history.Where(h => h.Date == _clock.Today).Sum(h => h.Count);
            DhikrSummaryText = todayTotal > 0 ? $"Bugün {todayTotal} zikir" : "Tesbihat & Sayaç";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick cards refresh failed");
        }
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
            if (_days.Count == 0)
            {
                HasScheduleData = false;
                NextPrayerName = "Veri yok";
                NextPrayerTime = "--:--";
                RemainingTime = "--:--:--";
            }
            return;
        }

        _days = result.Value.Days;
        _loadedDate = _clock.Today;
        var today = _days.FirstOrDefault(d => d.Date == _clock.Today);
        HasScheduleData = today is not null;
        IsOffline = result.Value.IsFromCache;
        DataStatusText = BuildDataStatusText(result.Value, today?.Date);

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

    private static string BuildDataStatusText(PrayerScheduleResult result, DateOnly? shownDate)
    {
        var dateLabel = shownDate is { } d
            ? d.ToDateTime(TimeOnly.MinValue).ToString("d MMMM", TurkishCulture)
            : "Bugün";

        if (result.LastFetchedAtUtc is not { } fetchedAt)
            return result.IsFromCache ? $"Çevrimdışı · {dateLabel} verisi" : string.Empty;

        var local = fetchedAt.ToLocalTime();
        return result.IsFromCache
            ? $"Kayıtlı veri ({dateLabel}) · Son güncelleme {local.ToString("d MMM HH:mm", TurkishCulture)}"
            : $"Güncel ({dateLabel})";
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
        CityDisplay = settings.Location?.DisplayName is { Length: > 0 } name ? name : "Şehir Seçin";
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

            var name = kind.ToString();
            var isTrackable = kind != PrayerKind.Sunrise;

            PrayerTimes.Add(new PrayerTime
            {
                Name = name,
                TurkishName = kind.ToTurkishName(),
                Icon = PrayerIcons.For(kind),
                Time = time.ToTimeSpan(),
                IsTrackable = isTrackable,
                IsCompleted = isTrackable && _trackingService.IsCompletedToday(name)
            });
        }
    }

    private void SyncPrayerCompletionStates()
    {
        foreach (var pt in PrayerTimes)
        {
            if (pt.IsTrackable)
                pt.IsCompleted = _trackingService.IsCompletedToday(pt.Name);
        }
    }

    private void UpdateRamadan(PrayerDay? today)
    {
        var settings = _settingsStore.Get();
        IsRamadanVisible = settings.RamadanModeEnabled && today?.HijriMonth == 9;
        if (!IsRamadanVisible || today is null)
            return;

        RamadanDayText = $"Ramazan · {today.HijriDay}. Gün";
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
        if (_days.Count == 0 || !HasScheduleData)
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
            NextPrayerName = "—";
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
        UpdateKerahat(now);
    }

    private void UpdateKerahat(DateTimeOffset now)
    {
        var today = _days.FirstOrDefault(d => d.Date == _clock.Today);
        if (today is null)
        {
            KerahatText = string.Empty;
            return;
        }

        var window = KerahatCalculator.GetCurrent(today, now);
        KerahatText = window is null ? string.Empty : $"Kerahat vakti · {window.Name}";
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
            RamadanCountdownLabel = "İftara kalan süre";
            var remaining = maghribAt - now;
            RamadanCountdown = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
        else
        {
            var tomorrow = _days.FirstOrDefault(d => d.Date == _clock.Today.AddDays(1)) ?? today;
            var fajrAt = new DateTimeOffset(_clock.Today.AddDays(1).ToDateTime(tomorrow[PrayerKind.Fajr]), now.Offset);
            RamadanCountdownLabel = "Sahura kalan süre";
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

    private async Task ClearVerseActionMessageAsync()
    {
        await Task.Delay(2200);
        VerseActionMessage = string.Empty;
    }
}
