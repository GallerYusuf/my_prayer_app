using System.Text.Json.Serialization;

namespace PrayerApp.Models;

public record QuranVerse(
    [property: JsonPropertyName("surahName")] string SurahName,
    [property: JsonPropertyName("ayahNumber")] int AyahNumber,
    [property: JsonPropertyName("text")] string Text)
{
    public string DisplayText => $"{SurahName} Suresi, {AyahNumber}. Ayet\n\n{Text}";
}
