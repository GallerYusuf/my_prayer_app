using System.Text.Json.Serialization;

namespace PrayerApp.Models;

public record AladhanResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("data")] AladhanData Data);

public record AladhanData(
    [property: JsonPropertyName("timings")] PrayerTimings Timings,
    [property: JsonPropertyName("date")] AladhanDate Date,
    [property: JsonPropertyName("meta")] AladhanMeta? Meta);

public record AladhanMeta(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude);

public record PrayerTimings(
    [property: JsonPropertyName("Fajr")] string Fajr,
    [property: JsonPropertyName("Sunrise")] string Sunrise,
    [property: JsonPropertyName("Dhuhr")] string Dhuhr,
    [property: JsonPropertyName("Asr")] string Asr,
    [property: JsonPropertyName("Maghrib")] string Maghrib,
    [property: JsonPropertyName("Isha")] string Isha);

public record AladhanDate(
    [property: JsonPropertyName("readable")] string Readable,
    [property: JsonPropertyName("hijri")] AladhanHijri? Hijri);

public record AladhanHijri(
    [property: JsonPropertyName("day")] string Day,
    [property: JsonPropertyName("month")] AladhanHijriMonth Month,
    [property: JsonPropertyName("year")] string Year);

public record AladhanHijriMonth(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("en")] string En);
