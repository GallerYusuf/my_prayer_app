using System.Globalization;

namespace PrayerApp.Core.Services;

public sealed record ReligiousDay(
    DateOnly Date,
    string Name,
    string Description,
    bool IsBayram);

/// <summary>
/// Computes upcoming Islamic special days (kandils, Ramadan, bayrams) by converting
/// fixed hijri dates with <see cref="UmAlQuraCalendar"/>. Dates may differ from the
/// Diyanet calendar by one day around month boundaries; the UI carries that note.
/// </summary>
public static class ReligiousDaysCalculator
{
    private static readonly UmAlQuraCalendar Hijri = new();

    // Fixed hijri-date events: (month, day, name, description, isBayram).
    // Kandil entries use the DAY whose following night is the kandil night,
    // matching how Turkish calendars list them.
    private static readonly (int Month, int Day, string Name, string Description, bool IsBayram)[] FixedEvents =
    [
        (1, 1, "Hicri Yılbaşı", "Hicri takvimin ilk günü (1 Muharrem)", false),
        (1, 10, "Aşure Günü", "Muharrem ayının onuncu günü", false),
        (3, 11, "Mevlid Kandili", "Peygamber Efendimiz'in doğum gecesi", false),
        (7, 26, "Miraç Kandili", "İsra ve Miraç mucizesinin gecesi", false),
        (8, 14, "Berat Kandili", "Şaban ayının on beşinci gecesi", false),
        (9, 1, "Ramazan Başlangıcı", "On bir ayın sultanının ilk günü", false),
        (9, 26, "Kadir Gecesi", "Bin aydan hayırlı gece (Ramazan'ın 27. gecesi)", false),
        (10, 1, "Ramazan Bayramı (1. gün)", "Şevval ayının ilk günü", true),
        (10, 2, "Ramazan Bayramı (2. gün)", "", true),
        (10, 3, "Ramazan Bayramı (3. gün)", "", true),
        (12, 9, "Arefe", "Kurban Bayramı arefesi", false),
        (12, 10, "Kurban Bayramı (1. gün)", "Zilhicce ayının onuncu günü", true),
        (12, 11, "Kurban Bayramı (2. gün)", "", true),
        (12, 12, "Kurban Bayramı (3. gün)", "", true),
        (12, 13, "Kurban Bayramı (4. gün)", "", true)
    ];

    /// <summary>Special days from <paramref name="from"/> (inclusive) for the next <paramref name="months"/> months.</summary>
    public static IReadOnlyList<ReligiousDay> GetUpcoming(DateOnly from, int months = 12)
    {
        var until = from.AddMonths(months);
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var hijriYear = Hijri.GetYear(fromDate);

        var result = new List<ReligiousDay>();

        // Cover the hijri years overlapping the requested window.
        for (var year = hijriYear; year <= hijriYear + 2; year++)
        {
            foreach (var (month, day, name, description, isBayram) in FixedEvents)
            {
                var date = SafeToGregorian(year, month, day);
                if (date is { } d && d >= from && d <= until)
                    result.Add(new ReligiousDay(d, name, description, isBayram));
            }

            if (RegaibOf(year) is { } regaib && regaib >= from && regaib <= until)
                result.Add(new ReligiousDay(regaib, "Regaib Kandili", "Receb ayının ilk cuma gecesi", false));
        }

        return result.OrderBy(d => d.Date).ToList();
    }

    /// <summary>Hijri date of a Gregorian day, e.g. for the converter UI.</summary>
    public static (int Year, int Month, int Day) ToHijri(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return (Hijri.GetYear(dt), Hijri.GetMonth(dt), Hijri.GetDayOfMonth(dt));
    }

    /// <summary>Gregorian date of a hijri day, or null when out of the supported range.</summary>
    public static DateOnly? ToGregorian(int hijriYear, int hijriMonth, int hijriDay) =>
        SafeToGregorian(hijriYear, hijriMonth, hijriDay);

    public static string HijriMonthName(int month) => month switch
    {
        1 => "Muharrem", 2 => "Safer", 3 => "Rebiülevvel", 4 => "Rebiülahir",
        5 => "Cemaziyelevvel", 6 => "Cemaziyelahir", 7 => "Receb", 8 => "Şaban",
        9 => "Ramazan", 10 => "Şevval", 11 => "Zilkade", 12 => "Zilhicce",
        _ => string.Empty
    };

    /// <summary>Regaib: the Thursday whose night leads into the first Friday of Rajab.</summary>
    private static DateOnly? RegaibOf(int hijriYear)
    {
        var rajabFirst = SafeToGregorian(hijriYear, 7, 1);
        if (rajabFirst is not { } start)
            return null;

        for (var i = 0; i < 7; i++)
        {
            var candidate = start.AddDays(i);
            if (candidate.DayOfWeek == DayOfWeek.Friday)
                return candidate.AddDays(-1);
        }

        return null;
    }

    private static DateOnly? SafeToGregorian(int year, int month, int day)
    {
        try
        {
            // Some hijri months have 29 days; clamp day 30 requests gracefully.
            var daysInMonth = Hijri.GetDaysInMonth(year, month);
            var dt = Hijri.ToDateTime(year, month, Math.Min(day, daysInMonth), 0, 0, 0, 0);
            return DateOnly.FromDateTime(dt);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
