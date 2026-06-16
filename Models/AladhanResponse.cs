using System.Text.Json.Serialization;

namespace PrayerApp.Models;

public record AladhanResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("data")] AladhanData Data);

public record AladhanData(
    [property: JsonPropertyName("timings")] PrayerTimings Timings,
    [property: JsonPropertyName("date")] AladhanDate Date);

public record PrayerTimings(
    [property: JsonPropertyName("Fajr")] string Fajr,
    [property: JsonPropertyName("Sunrise")] string Sunrise,
    [property: JsonPropertyName("Dhuhr")] string Dhuhr,
    [property: JsonPropertyName("Asr")] string Asr,
    [property: JsonPropertyName("Maghrib")] string Maghrib,
    [property: JsonPropertyName("Isha")] string Isha);

public record AladhanDate(
    [property: JsonPropertyName("readable")] string Readable);
