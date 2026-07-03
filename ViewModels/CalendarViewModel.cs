using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;

namespace PrayerApp.ViewModels;

public sealed record CalendarRow(string Name, string Time);

public partial class CalendarViewModel : ObservableObject
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly IPrayerScheduleService _scheduleService;
    private readonly ISettingsStore _settingsStore;
    private readonly IClock _clock;

    private IReadOnlyList<PrayerDay> _days = [];

    [ObservableProperty] private DateOnly _selectedDate;
    [ObservableProperty] private string _selectedDateText = string.Empty;
    [ObservableProperty] private string _hijriDateText = string.Empty;
    [ObservableProperty] private string _locationText = string.Empty;
    [ObservableProperty] private string _methodText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _canGoForward = true;
    [ObservableProperty] private bool _canGoBack = true;
    [ObservableProperty] private bool _isToday;

    public ObservableCollection<CalendarRow> Rows { get; } = [];

    public CalendarViewModel(IPrayerScheduleService scheduleService, ISettingsStore settingsStore, IClock clock)
    {
        _scheduleService = scheduleService;
        _settingsStore = settingsStore;
        _clock = clock;
        _selectedDate = _clock.Today;
    }

    public async Task OnAppearingAsync()
    {
        var settings = _settingsStore.Get();
        LocationText = settings.Location?.DisplayName ?? "—";
        MethodText = settings.CalculationMethod.ToDisplayName();

        if (_days.Count == 0)
            await LoadWindowAsync();
        else
            ShowSelectedDay();
    }

    [RelayCommand]
    private async Task GoPreviousDayAsync() => await MoveAsync(-1);

    [RelayCommand]
    private async Task GoNextDayAsync() => await MoveAsync(1);

    [RelayCommand]
    private async Task GoTodayAsync()
    {
        SelectedDate = _clock.Today;
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

        var text = $"{SelectedDateText} — {LocationText}\n{string.Join("\n", lines)}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Namaz Vakitleri", Text = text });
    }

    [RelayCommand]
    private static async Task OpenAdjustmentsAsync() => await Shell.Current.GoToAsync("//SettingsPage");

    private async Task MoveAsync(int deltaDays)
    {
        SelectedDate = SelectedDate.AddDays(deltaDays);
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

            // Cache holds ~35 days from today; clamp the visible range accordingly.
            var from = SelectedDate < _clock.Today ? SelectedDate : _clock.Today;
            var result = await _scheduleService.GetScheduleAsync(from, 35);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error.Message;
                Rows.Clear();
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

    private void ShowSelectedDay()
    {
        SelectedDateText = SelectedDate.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy, dddd", TurkishCulture);
        IsToday = SelectedDate == _clock.Today;

        var day = _days.FirstOrDefault(d => d.Date == SelectedDate);
        Rows.Clear();

        if (day is null)
        {
            ErrorMessage = "Bu tarih için kayıtlı vakit verisi yok. İnternet bağlantısıyla yenileyin.";
            HijriDateText = string.Empty;
            CanGoForward = false;
            return;
        }

        ErrorMessage = string.Empty;
        HijriDateText = day.HijriDateText;
        CanGoForward = _days.Any(d => d.Date > SelectedDate);
        CanGoBack = true;

        foreach (var kind in PrayerKindExtensions.All)
        {
            if (day.Times.TryGetValue(kind, out var time))
                Rows.Add(new CalendarRow(kind.ToTurkishName(), time.ToString("HH\\:mm")));
        }
    }
}
