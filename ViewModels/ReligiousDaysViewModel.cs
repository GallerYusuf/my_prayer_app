using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using PrayerApp.Core.Common;
using PrayerApp.Core.Services;

namespace PrayerApp.ViewModels;

public sealed record ReligiousDayItem(
    string DateText,
    string DayOfWeekText,
    string Name,
    string Description,
    string RemainingText,
    bool IsBayram,
    bool IsNext);

/// <summary>Upcoming Islamic special days plus a Gregorian→Hijri converter.</summary>
public partial class ReligiousDaysViewModel : ObservableObject
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly IClock _clock;

    [ObservableProperty] private DateTime _convertDate;
    [ObservableProperty] private string _convertedHijriText = string.Empty;

    public ObservableCollection<ReligiousDayItem> Days { get; } = [];

    public ReligiousDaysViewModel(IClock clock)
    {
        _clock = clock;
        _convertDate = clock.Today.ToDateTime(TimeOnly.MinValue);
        UpdateConversion();
    }

    public void OnAppearing()
    {
        if (Days.Count > 0)
            return;

        var today = _clock.Today;
        var upcoming = ReligiousDaysCalculator.GetUpcoming(today, 13);
        var isFirst = true;

        Days.Clear();
        foreach (var day in upcoming)
        {
            var remainingDays = day.Date.DayNumber - today.DayNumber;
            var remainingText = remainingDays switch
            {
                0 => "Bugün",
                1 => "Yarın",
                _ => $"{remainingDays} gün sonra"
            };

            Days.Add(new ReligiousDayItem(
                day.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", TurkishCulture),
                day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", TurkishCulture),
                day.Name,
                day.Description,
                remainingText,
                day.IsBayram,
                isFirst));

            isFirst = false;
        }
    }

    partial void OnConvertDateChanged(DateTime value) => UpdateConversion();

    private void UpdateConversion()
    {
        var (year, month, day) = ReligiousDaysCalculator.ToHijri(DateOnly.FromDateTime(ConvertDate));
        ConvertedHijriText = $"{day} {ReligiousDaysCalculator.HijriMonthName(month)} {year}";
    }
}
