namespace PrayerApp.Data;

public sealed record QuranicDua(string Title, int Surah, int Verse, int VerseEnd = 0)
{
    public int EffectiveEnd => VerseEnd > 0 ? VerseEnd : Verse;
}

/// <summary>
/// Curated well-known duas from the Quran itself, referenced by surah:verse.
/// Text comes from the app's own Quran database (Arabic + meal), so there is
/// no external content or licensing involved.
/// </summary>
public static class QuranicDuas
{
    public static readonly IReadOnlyList<QuranicDua> All =
    [
        new("Fâtiha — En kapsamlı dua", 1, 1, 7),
        new("Rabbena âtinâ (dünya ve ahiret iyiliği)", 2, 201),
        new("Sabır ve zafer duası", 2, 250),
        new("Âyetü'l-Kürsî", 2, 255),
        new("Unutma ve hata için (Âmenerrasûlü)", 2, 286),
        new("Kalplerin kaymaması için", 3, 8),
        new("Günahların bağışlanması için", 3, 16),
        new("Hayırlı evlat için (Zekeriya'nın duası)", 3, 38),
        new("Bize Allah yeter (Hasbünallah)", 3, 173),
        new("Yakarış — ayakta, oturarak, yatarken", 3, 191, 194),
        new("Zulümden ve zalimlerden korunma", 7, 47),
        new("Rabbimiz, bizi bağışla (Hz. Âdem'in duası)", 7, 23),
        new("Tevekkül duası (Hz. İbrahim)", 9, 129),
        new("Sıkıntıdan kurtuluş (Hz. Yunus'un duası)", 21, 87),
        new("Rahmet kapılarının açılması", 18, 10),
        new("Göğsümü aç, işimi kolaylaştır (Hz. Musa)", 20, 25, 28),
        new("İlmimi artır duası", 20, 114),
        new("Şeytanın vesvesesinden sığınma", 23, 97, 98),
        new("Göz aydınlığı eş ve nesil için", 25, 74),
        new("Şükür ve salih amel duası", 27, 19),
        new("Anne-baba için dua", 17, 24),
        new("Hesap gününde bağışlanma", 14, 41),
        new("Namazı dosdoğru kılanlardan eyle", 14, 40),
        new("Nur ve bağışlanma duası", 66, 8),
        new("Felak — sabah aydınlığının Rabbine sığınırım", 113, 1, 5),
        new("Nâs — insanların Rabbine sığınırım", 114, 1, 6)
    ];
}
