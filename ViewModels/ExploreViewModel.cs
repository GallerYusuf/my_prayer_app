using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Core.Services;
using PrayerApp.Data;

namespace PrayerApp.ViewModels;

public partial class ExploreViewModel : ObservableObject
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly ISettingsStore _settingsStore;
    private readonly IClock _clock;

    [ObservableProperty] private string _qiblaSummary = "Kıble açısı ve Kâbe mesafesi";
    [ObservableProperty] private string _nextReligiousDaySummary = "Kandiller, bayramlar ve hicri çevirici";
    [ObservableProperty] private string _esmaOfTheDayArabic = string.Empty;
    [ObservableProperty] private string _esmaOfTheDayName = string.Empty;
    [ObservableProperty] private string _esmaOfTheDayMeaning = string.Empty;
    [ObservableProperty] private string _duasSummary = $"{QuranicDuas.All.Count} ayet duası · Arapça ve Türkçe meal";
    [ObservableProperty] private string _settingsSummary = "Konum, bildirimler, görünüm ve yedekleme";

    public ExploreViewModel(ISettingsStore settingsStore, IClock clock)
    {
        _settingsStore = settingsStore;
        _clock = clock;
    }

    public void OnAppearing()
    {
        var settings = _settingsStore.Get();
        if (settings.Location is { IsValid: true } loc)
        {
            var bearing = QiblaCalculator.CalculateBearing(loc.Latitude, loc.Longitude);
            var dist = QiblaCalculator.CalculateDistanceKm(loc.Latitude, loc.Longitude);
            QiblaSummary = $"{loc.DisplayName} · Kıble {bearing:F0}° · {dist:N0} km";
        }
        else
        {
            QiblaSummary = "Canlı pusula ve Kâbe mesafesi";
        }

        var today = _clock.Today;
        var nextDay = ReligiousDaysCalculator.GetUpcoming(today, 12).FirstOrDefault();
        if (nextDay is not null)
        {
            var diff = nextDay.Date.DayNumber - today.DayNumber;
            var when = diff switch
            {
                0 => "Bugün",
                1 => "Yarın",
                _ => $"{diff} gün sonra ({nextDay.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMM", TurkishCulture)})"
            };
            NextReligiousDaySummary = $"Sıradaki: {nextDay.Name} · {when}";
        }

        var esmaList = EsmaulHusnaData.All;
        if (esmaList.Count > 0)
        {
            var index = Math.Abs(today.DayNumber) % esmaList.Count;
            var esma = esmaList[index];
            EsmaOfTheDayArabic = esma.Arabic;
            EsmaOfTheDayName = $"{esma.Number}. {esma.Transliteration}";
            EsmaOfTheDayMeaning = esma.Meaning;
        }

        var themeName = settings.Theme switch
        {
            ThemePreference.Light => "Açık tema",
            ThemePreference.Dark => "Koyu tema",
            _ => "Sistem teması"
        };
        SettingsSummary = $"{settings.Location?.DisplayName ?? "Konum yok"} · {themeName}";
    }

    [RelayCommand]
    private static async Task OpenQiblaAsync() => await Shell.Current.GoToAsync("QiblaPage");

    [RelayCommand]
    private static async Task OpenDuasAsync() => await Shell.Current.GoToAsync("DuasPage");

    [RelayCommand]
    private static async Task OpenEsmaulHusnaAsync() => await Shell.Current.GoToAsync("EsmaulHusnaPage");

    [RelayCommand]
    private static async Task OpenReligiousDaysAsync() => await Shell.Current.GoToAsync("ReligiousDaysPage");

    [RelayCommand]
    private static async Task OpenZakatAsync() => await Shell.Current.GoToAsync("ZakatPage");

    [RelayCommand]
    private static async Task OpenKazaAsync() => await Shell.Current.GoToAsync("KazaPage");

    [RelayCommand]
    private static async Task OpenDhikrAsync() => await Shell.Current.GoToAsync("DhikrPage");

    [RelayCommand]
    private static async Task OpenSettingsAsync() => await Shell.Current.GoToAsync("SettingsPage");
}
