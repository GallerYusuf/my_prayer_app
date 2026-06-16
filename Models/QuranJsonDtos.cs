using System.Text.Json.Serialization;

namespace PrayerApp.Models;

/// <summary>
/// quran-json (Diyanet meal) kaynak formatı — yükleme sırasında düzleştirilir.
/// </summary>
internal record QuranSurahDto(
    [property: JsonPropertyName("translation")] string Translation,
    [property: JsonPropertyName("verses")] List<QuranVerseDto> Verses);

internal record QuranVerseDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("translation")] string Translation);
