using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Models;

namespace PrayerApp.ViewModels;

public sealed record CalendarRow(string Name, string Time, string Icon, bool IsHighlighted = false);

public sealed record CalendarReligiousDayItem(
    DateOnly Date,
    string DateText,
    string DayOfWeekText,
    string Name,
    string Description,
    string RemainingText,
    bool IsBayram,
    bool IsNext);

public partial class CalendarViewModel : ObservableObject
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly IPrayerScheduleService _scheduleService;
    private readonly ISettingsStore _settingsStore;
    private readonly IClock _clock;

    private IReadOnlyList<PrayerDay> _days = [];
    private IReadOnlyList<ReligiousDay> _upcomingReligiousDays = [];

    // 0 = Günlük Vakitler, 1 = Dini Günler & Hicri Çevirici
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTimesTab))]
    [NotifyPropertyChangedFor(nameof(IsReligiousDaysTab))]
    private int _activeTab;

    public bool IsTimesTab => ActiveTab == 0;
    public bool IsReligiousDaysTab => ActiveTab == 1;

    [ObservableProperty] private DateOnly _selectedDate;
    [ObservableProperty] private DateTime _pickerDate;
    [ObservableProperty] private string _selectedDateText = string.Empty;
    [ObservableProperty] private string _hijriDateText = string.Empty;
    [ObservableProperty] private string _selectedReligiousDayName = string.Empty;
    [ObservableProperty] private string _selectedReligiousDayDesc = string.Empty;
    [ObservableProperty] private string _locationText = string.Empty;
    [ObservableProperty] private string _methodText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _canGoForward = true;
    [ObservableProperty] private bool _canGoBack = true;
    [ObservableProperty] private bool _isToday;
    [ObservableProperty] private bool _hasRows;

    // Hijri converter
    [ObservableProperty] private DateTime _convertDate;
    [ObservableProperty] private string _convertedHijriText = string.Empty;

    public ObservableCollection<CalendarRow> Rows { get; } = [];
    public ObservableCollection<CalendarReligiousDayItem> ReligiousDays { get; } = [];
    public ObservableCollection<CalendarReligiousDayItem> NextThreeReligiousDays { get; } = [];

    public CalendarViewModel(IPrayerScheduleService scheduleService, ISettingsStore settingsStore, IClock clock)
    {
        _scheduleService = scheduleService;
        _settingsStore = settingsStore;
        _clock = clock;
        _selectedDate = _clock.Today;
        _pickerDate = _selectedDate.ToDateTime(TimeOnly.MinValue);
        _convertDate = _pickerDate;
        UpdateConversion();
    }

    public async Task OnAppearingAsync()
    {
        var settings = _settingsStore.Get();
        LocationText = settings.Location?.DisplayName ?? "Konum seçilmedi";
        MethodText = settings.CalculationMethod.ToDisplayName();

        LoadReligiousDays();

        if (_days.Count == 0)
            await LoadWindowAsync();
        else
            ShowSelectedDay();
    }

    [RelayCommand]
    private void SelectTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var idx))
            ActiveTab = Math.Clamp(idx, 0, 1);
    }

    partial void OnPickerDateChanged(DateTime value)
    {
        var newDate = DateOnly.FromDateTime(value);
        if (newDate != SelectedDate)
        {
            SelectedDate = newDate;
            _ = RefreshForSelectedDateAsync();
        }
    }

    partial void OnConvertDateChanged(DateTime value) => UpdateConversion();

    private void UpdateConversion()
    {
        var (year, month, day) = ReligiousDaysCalculator.ToHijri(DateOnly.FromDateTime(ConvertDate));
        ConvertedHijriText = $"{day} {ReligiousDaysCalculator.HijriMonthName(month)} {year}";
    }

    private void LoadReligiousDays()
    {
        var today = _clock.Today;
        _upcomingReligiousDays = ReligiousDaysCalculator.GetUpcoming(today.AddMonths(-1), 15);
        var upcomingFromToday = _upcomingReligiousDays.Where(d => d.Date >= today).ToList();

        ReligiousDays.Clear();
        NextThreeReligiousDays.Clear();

        var isFirst = true;
        foreach (var day in upcomingFromToday)
        {
            var remainingDays = day.Date.DayNumber - today.DayNumber;
            var remainingText = remainingDays switch
            {
                0 => "Bugün",
                1 => "Yarın",
                _ => $"{remainingDays} gün sonra"
            };

            var item = new CalendarReligiousDayItem(
                day.Date,
                day.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", TurkishCulture),
                day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", TurkishCulture),
                day.Name,
                day.Description,
                remainingText,
                day.IsBayram,
                isFirst);

            ReligiousDays.Add(item);
            if (NextThreeReligiousDays.Count < 3)
                NextThreeReligiousDays.Add(item);

            isFirst = false;
        }
    }

    [RelayCommand]
    private async Task SelectReligiousDayAsync(CalendarReligiousDayItem item)
    {
        SelectedDate = item.Date;
        PickerDate = item.Date.ToDateTime(TimeOnly.MinValue);
        ActiveTab = 0;
        await RefreshForSelectedDateAsync();
    }

    [RelayCommand]
    private async Task GoPreviousDayAsync() => await MoveAsync(-1);

    [RelayCommand]
    private async Task GoNextDayAsync() => await MoveAsync(1);

    [RelayCommand]
    private async Task GoTodayAsync()
    {
        SelectedDate = _clock.Today;
        PickerDate = SelectedDate.ToDateTime(TimeOnly.MinValue);
        await LoadWindowAsync();
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        var day = _days.FirstOrDefault(d => d.Date == SelectedDate);
        if (day is null)
            return;

        var lines = PrayerKindExtensions.All
            .Where(k => day.Times.ContainsKey(k))
            .Select(k => $"{k.ToTurkishName()}: {day[k]:HH\\:mm}");

        var text = $"{SelectedDateText} ({HijriDateText}) — {LocationText}\n{string.Join("\n", lines)}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Namaz Vakitleri", Text = text });
    }

    [RelayCommand]
    private static async Task OpenAdjustmentsAsync() => await Shell.Current.GoToAsync("SettingsPage");

    private async Task MoveAsync(int deltaDays)
    {
        SelectedDate = SelectedDate.AddDays(deltaDays);
        PickerDate = SelectedDate.ToDateTime(TimeOnly.MinValue);
        await RefreshForSelectedDateAsync();
    }

    private async Task RefreshForSelectedDateAsync()
    {
        if (_days.All(d => d.Date != SelectedDate))
            await LoadWindowAsync();
        else
            ShowSelectedDay();
    }

    private async Task LoadWindowAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var from = SelectedDate < _clock.Today ? SelectedDate : _clock.Today;
            var result = await _scheduleService.GetScheduleAsync(from, 35);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error.Message;
                Rows.Clear();
                HasRows = false;
                UpdateDayMetadata(null);
                return;
            }

            _days = result.Value.Days;
            ShowSelectedDay();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateDayMetadata(PrayerDay? day)
    {
        SelectedDateText = SelectedDate.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy, dddd", TurkishCulture);
        IsToday = SelectedDate == _clock.Today;

        if (!string.IsNullOrWhiteSpace(day?.HijriDateText))
        {
            HijriDateText = day.HijriDateText;
        }
        else
        {
            var (hYear, hMonth, hDay) = ReligiousDaysCalculator.ToHijri(SelectedDate);
            HijriDateText = $"{hDay} {ReligiousDaysCalculator.HijriMonthName(hMonth)} {hYear}";
        }

        var special = _upcomingReligiousDays.FirstOrDefault(r => r.Date == SelectedDate)
            ?? ReligiousDaysCalculator.GetUpcoming(SelectedDate, 1)
                .FirstOrDefault(r => r.Date == SelectedDate);
        SelectedReligiousDayName = special?.Name ?? string.Empty;
        SelectedReligiousDayDesc = special?.Description ?? string.Empty;
    }

    private void ShowSelectedDay()
    {
        var day = _days.FirstOrDefault(d => d.Date == SelectedDate);
        UpdateDayMetadata(day);
        Rows.Clear();

        if (day is null)
        {
            HasRows = false;
            ErrorMessage = "Seçilen tarih 35 günlük çevrimdışı vakit önbelleğinin dışında veya henüz indirilmedi. Hicri tarih ve dini gün bilgisini yukarıda görebilirsiniz.";
            CanGoForward = true;
            return;
        }

        ErrorMessage = string.Empty;
        CanGoForward = true;
        CanGoBack = true;

        var now = _clock.LocalNow;
        var nextToday = IsToday ? NextPrayerCalculator.FindNext([day], now) : null;

        foreach (var kind in PrayerKindExtensions.All)
        {
            if (day.Times.TryGetValue(kind, out var time))
            {
                var isHighlight = nextToday is not null && nextToday.Kind == kind;
                Rows.Add(new CalendarRow(
                    kind.ToTurkishName(),
                    time.ToString("HH\\:mm"),
                    PrayerIcons.For(kind),
                    isHighlight));
            }
        }
        HasRows = Rows.Count > 0;
    }
}
