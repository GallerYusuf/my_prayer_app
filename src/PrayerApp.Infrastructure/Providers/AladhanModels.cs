using System.Text.Json.Serialization;

namespace PrayerApp.Infrastructure.Providers;

// DTOs for the Aladhan /v1/calendar endpoint.

internal sealed class AladhanCalendarResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<AladhanCalendarDay> Data { get; set; } = [];
}

internal sealed class AladhanCalendarDay
{
    [JsonPropertyName("timings")]
    public AladhanTimings? Timings { get; set; }

    [JsonPropertyName("date")]
    public AladhanDate? Date { get; set; }
}

internal sealed class AladhanTimings
{
    [JsonPropertyName("Fajr")]
    public string Fajr { get; set; } = string.Empty;

    [JsonPropertyName("Sunrise")]
    public string Sunrise { get; set; } = string.Empty;

    [JsonPropertyName("Dhuhr")]
    public string Dhuhr { get; set; } = string.Empty;

    [JsonPropertyName("Asr")]
    public string Asr { get; set; } = string.Empty;

    [JsonPropertyName("Maghrib")]
    public string Maghrib { get; set; } = string.Empty;

    [JsonPropertyName("Isha")]
    public string Isha { get; set; } = string.Empty;
}

internal sealed class AladhanDate
{
    [JsonPropertyName("gregorian")]
    public AladhanGregorian? Gregorian { get; set; }

    [JsonPropertyName("hijri")]
    public AladhanHijriDate? Hijri { get; set; }
}

internal sealed class AladhanGregorian
{
    /// <summary>Format: dd-MM-yyyy</summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
}

internal sealed class AladhanHijriDate
{
    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public AladhanHijriMonth? Month { get; set; }
}

internal sealed class AladhanHijriMonth
{
    [JsonPropertyName("number")]
    public int Number { get; set; }
}
