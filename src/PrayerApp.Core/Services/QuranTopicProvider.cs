using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Services;

public static class QuranTopicProvider
{
    public static IReadOnlyList<QuranTopic> GetTopics() => new List<QuranTopic>
    {
        new()
        {
            Name = "İman & Tevhid",
            Icon = "🌱",
            Verses = new[]
            {
                (112, 1), (112, 2), (112, 3), (112, 4), // İhlas
                (2, 255), // Ayetel Kürsi
                (2, 285), (2, 286) // Amenerrasulü
            }
        },
        new()
        {
            Name = "Dua ve Zikir",
            Icon = "🤲",
            Verses = new[]
            {
                (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), // Fatiha
                (2, 186), // Duaların kabulü
                (3, 190), (3, 191), (3, 192), (3, 193), (3, 194) // Al-i İmran son ayetler
            }
        },
        new()
        {
            Name = "Güzel Ahlak",
            Icon = "💎",
            Verses = new[]
            {
                (31, 17), (31, 18), // Lokman öğütleri
                (49, 10), (49, 11), (49, 12), // Kardeşlik, hüsnüzan, gıybet yasağı
                (25, 63) // Rahman'ın kullarının yürüyüşü
            }
        },
        new()
        {
            Name = "Sabır ve Tevekkül",
            Icon = "🛡️",
            Verses = new[]
            {
                (2, 153), (2, 154), (2, 155), (2, 156), (2, 157), // Sabredenlerin müjdelenmesi
                (94, 5), (94, 6), // İnşirah - zorlukla beraber kolaylık
                (65, 3) // Allah'a tevekkül
            }
        },
        new()
        {
            Name = "Tövbe ve Ümit",
            Icon = "❤️",
            Verses = new[]
            {
                (39, 53), // Allah'ın rahmetinden ümit kesmeyin
                (42, 25), // Tövbeyi kabul eden O'dur
                (66, 8) // Samimi tövbe (Tevbe-i Nasuh)
            }
        }
    };
}
